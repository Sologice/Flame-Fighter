using System;
using UnityEngine;
using FlameFighter.CameraSystem;
using FlameFighter.Fire;
using FlameFighter.Input;

namespace FlameFighter.Hose
{
    public class HoseController : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private HoseSettings m_Settings;

        [Header("References")]
        [Tooltip("The camera eye from which the water stream is aimed. If null, uses Camera.main.")]
        [SerializeField] private Transform m_StreamOrigin;

        [Tooltip("Input reader source. If null, finds one on player or self.")]
        [SerializeField] private PlayerInputReader m_InputReader;

        [Tooltip("Camera controller to receive recoil kick when spraying.")]
        [SerializeField] private FirstPersonCameraController m_CameraController;

        [Header("Visual Effects")]
        [Tooltip("Particle system shooting the water stream from the hose nozzle.")]
        [SerializeField] private ParticleSystem m_WaterStreamParticles;

        [Tooltip("Particle system spawned or moved to the impact point where water hits an obstacle/fire.")]
        [SerializeField] private ParticleSystem m_WaterImpactParticles;

        [Header("Audio")]
        [Tooltip("Looping audio source playing the continuous pressurized water spray sound.")]
        [SerializeField] private AudioSource m_SprayAudioSource;

        // Runtime state
        private float m_CurrentWater;
        private bool m_IsSpraying;
        private readonly RaycastHit[] m_Hits = new RaycastHit[8];

        // Public Events & Properties
        public event Action<float> OnWaterChanged; // currentWater
        public float CurrentWater => m_CurrentWater;
        public float MaxWater => (m_Settings != null) ? m_Settings.MaxWaterCapacity : 100f;
        public float WaterRatio => MaxWater > 0f ? Mathf.Clamp01(m_CurrentWater / MaxWater) : 1f;
        public bool IsSpraying => m_IsSpraying;

        private void Awake()
        {
            if (m_InputReader == null)
            {
                m_InputReader = GetComponent<PlayerInputReader>();
                if (m_InputReader == null)
                {
                    m_InputReader = GetComponentInParent<PlayerInputReader>();
                }
            }

            if (m_CameraController == null)
            {
                m_CameraController = GetComponent<FirstPersonCameraController>();
                if (m_CameraController == null)
                {
                    m_CameraController = GetComponentInParent<FirstPersonCameraController>();
                }
            }

            if (m_StreamOrigin == null)
            {
                if (Camera.main != null)
                {
                    m_StreamOrigin = Camera.main.transform;
                }
                else if (m_CameraController != null && m_CameraController.CameraPitchRoot != null)
                {
                    m_StreamOrigin = m_CameraController.CameraPitchRoot;
                }
            }

            if (m_Settings != null)
            {
                m_CurrentWater = m_Settings.MaxWaterCapacity;
            }
        }

        private void Update()
        {
            if (m_Settings == null || m_InputReader == null) return;

            bool wantsToSpray = m_InputReader.IsSpraying;
            bool hasWater = m_Settings.UnlimitedWater || m_CurrentWater > 0f;

            if (wantsToSpray && hasWater)
            {
                StartOrContinueSpraying();
            }
            else
            {
                StopSpraying();
            }
        }

        private void StartOrContinueSpraying()
        {
            m_IsSpraying = true;

            // Drain water tank if not unlimited
            if (!m_Settings.UnlimitedWater)
            {
                m_CurrentWater = Mathf.Max(0f, m_CurrentWater - (m_Settings.DrainRatePerSecond * Time.deltaTime));
                OnWaterChanged?.Invoke(m_CurrentWater);

                if (m_CurrentWater <= 0f)
                {
                    StopSpraying();
                    return;
                }
            }

            // Play stream particles
            if (m_WaterStreamParticles != null && !m_WaterStreamParticles.isPlaying)
            {
                m_WaterStreamParticles.Play();
            }

            // Play audio loop
            if (m_SprayAudioSource != null && !m_SprayAudioSource.isPlaying)
            {
                m_SprayAudioSource.Play();
            }

            // Apply slight camera kick while spraying
            if (m_CameraController != null)
            {
                Vector2 kick = new Vector2(
                    (Mathf.PerlinNoise(Time.time * 12f, 0f) * 2f - 1f) * m_Settings.SprayCameraKick.x,
                    m_Settings.SprayCameraKick.y
                ) * Time.deltaTime * 60f;

                m_CameraController.AddKick(kick);
            }

            // Perform spherecast to find what the water stream hits
            Transform origin = m_StreamOrigin != null ? m_StreamOrigin : transform;
            Ray ray = new Ray(origin.position, origin.forward);

            int hitCount = Physics.SphereCastNonAlloc(
                ray,
                m_Settings.SpherecastRadius,
                m_Hits,
                m_Settings.SprayRange,
                m_Settings.HitLayers,
                QueryTriggerInteraction.Ignore
            );

            bool hitObstacle = false;
            RaycastHit closestHit = default;
            float closestDist = float.MaxValue;

            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = m_Hits[i];
                // Ignore self
                if (hit.collider.transform.IsChildOf(transform.root)) continue;

                if (hit.distance < closestDist)
                {
                    closestDist = hit.distance;
                    closestHit = hit;
                    hitObstacle = true;
                }
            }

            if (hitObstacle)
            {
                // Position water impact particles
                if (m_WaterImpactParticles != null)
                {
                    m_WaterImpactParticles.transform.position = closestHit.point;
                    m_WaterImpactParticles.transform.rotation = Quaternion.LookRotation(closestHit.normal);
                    if (!m_WaterImpactParticles.isPlaying) m_WaterImpactParticles.Play();
                }

                // Check for Flammable target
                if (closestHit.collider.TryGetComponent<Flammable>(out var flammable))
                {
                    float damageThisFrame = m_Settings.ExtinguishDPS * Time.deltaTime;
                    flammable.Extinguish(damageThisFrame, closestHit.point, m_Settings.BaseHitMultiplier);
                }
            }
            else
            {
                if (m_WaterImpactParticles != null && m_WaterImpactParticles.isPlaying)
                {
                    m_WaterImpactParticles.Stop();
                }
            }
        }

        private void StopSpraying()
        {
            if (!m_IsSpraying) return;

            m_IsSpraying = false;

            if (m_WaterStreamParticles != null && m_WaterStreamParticles.isPlaying)
            {
                m_WaterStreamParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }

            if (m_WaterImpactParticles != null && m_WaterImpactParticles.isPlaying)
            {
                m_WaterImpactParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }

            if (m_SprayAudioSource != null && m_SprayAudioSource.isPlaying)
            {
                m_SprayAudioSource.Stop();
            }
        }

        /// <summary>
        /// Refills water into the tank (called by WaterSource).
        /// </summary>
        public void Refill(float amount)
        {
            if (m_Settings == null) return;
            m_CurrentWater = Mathf.Min(m_Settings.MaxWaterCapacity, m_CurrentWater + amount);
            OnWaterChanged?.Invoke(m_CurrentWater);
        }

        public void SetUnlimitedWater(bool unlimited)
        {
            if (m_Settings != null)
            {
                // Toggle unlimited mode
            }
        }
    }
}

