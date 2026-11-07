using UnityEngine;

namespace FlameFighter.Fire
{
    [RequireComponent(typeof(Flammable))]
    public class FireVisual : MonoBehaviour
    {
        [Header("Particle Systems")]
        [Tooltip("Primary flame particle system. Scaled by fire intensity.")]
        [SerializeField] private ParticleSystem m_FlameParticles;

        [Tooltip("Smoke particle system emitted while burning or smoldering.")]
        [SerializeField] private ParticleSystem m_SmokeParticles;

        [Tooltip("Steam particle system triggered when water hits the fire.")]
        [SerializeField] private ParticleSystem m_SteamParticles;

        [Header("Lighting")]
        [Tooltip("Point light illuminating surrounding forest with flickering firelight.")]
        [SerializeField] private Light m_FireLight;

        [SerializeField] private float m_BaseLightIntensity = 3.5f;
        [SerializeField] private float m_LightFlickerSpeed = 15f;
        [SerializeField] private float m_LightFlickerAmount = 0.5f;

        [Header("Audio")]
        [Tooltip("Looping crackle audio source.")]
        [SerializeField] private AudioSource m_FireAudioSource;

        [Tooltip("Audio clip played when water hits fire.")]
        [SerializeField] private AudioClip m_SteamHissClip;

        [Header("Charred Visual (Burned Out)")]
        [Tooltip("Mesh renderer to tint black when burned out.")]
        [SerializeField] private Renderer m_TargetRenderer;

        [SerializeField] private Color m_CharredColor = new Color(0.12f, 0.12f, 0.12f, 1f);

        private Flammable m_Flammable;
        private float m_InitialFlameEmission;
        private float m_InitialSmokeEmission;
        private MaterialPropertyBlock m_PropBlock;

        private void Awake()
        {
            m_Flammable = GetComponent<Flammable>();
            m_PropBlock = new MaterialPropertyBlock();

            if (m_FlameParticles != null)
            {
                m_InitialFlameEmission = m_FlameParticles.emission.rateOverTimeMultiplier;
            }

            if (m_SmokeParticles != null)
            {
                m_InitialSmokeEmission = m_SmokeParticles.emission.rateOverTimeMultiplier;
            }

            if (m_TargetRenderer == null)
            {
                m_TargetRenderer = GetComponentInChildren<Renderer>();
            }
        }

        private void OnEnable()
        {
            if (m_Flammable != null)
            {
                m_Flammable.OnIgnited += HandleIgnited;
                m_Flammable.OnExtinguished += HandleExtinguished;
                m_Flammable.OnBurnedOut += HandleBurnedOut;
                m_Flammable.OnHitByWater += HandleWaterHit;
            }
        }

        private void OnDisable()
        {
            if (m_Flammable != null)
            {
                m_Flammable.OnIgnited -= HandleIgnited;
                m_Flammable.OnExtinguished -= HandleExtinguished;
                m_Flammable.OnBurnedOut -= HandleBurnedOut;
                m_Flammable.OnHitByWater -= HandleWaterHit;
            }
        }

        private void Update()
        {
            if (m_Flammable == null) return;

            float intensity = m_Flammable.FireIntensity;

            // Scale flame particles
            if (m_FlameParticles != null)
            {
                var emission = m_FlameParticles.emission;
                emission.rateOverTimeMultiplier = m_InitialFlameEmission * intensity;

                if (intensity <= 0.001f && m_FlameParticles.isPlaying)
                {
                    m_FlameParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                }
                else if (intensity > 0.001f && !m_FlameParticles.isPlaying)
                {
                    m_FlameParticles.Play();
                }
            }

            // Flicker and scale fire light
            if (m_FireLight != null)
            {
                if (intensity > 0.001f)
                {
                    m_FireLight.enabled = true;
                    float flicker = Mathf.PerlinNoise(Time.time * m_LightFlickerSpeed, 0f) * m_LightFlickerAmount;
                    m_FireLight.intensity = (m_BaseLightIntensity + flicker) * intensity;
                }
                else
                {
                    m_FireLight.enabled = false;
                }
            }

            // Scale fire audio
            if (m_FireAudioSource != null)
            {
                m_FireAudioSource.volume = intensity;
                if (intensity <= 0.001f && m_FireAudioSource.isPlaying)
                {
                    m_FireAudioSource.Stop();
                }
                else if (intensity > 0.001f && !m_FireAudioSource.isPlaying)
                {
                    m_FireAudioSource.Play();
                }
            }
        }

        private void HandleWaterHit(Vector3 hitPoint, bool isBaseHit)
        {
            // Spawn steam puff at water hit point
            if (m_SteamParticles != null)
            {
                m_SteamParticles.transform.position = hitPoint;
                m_SteamParticles.Emit(isBaseHit ? 15 : 8);
            }

            // Play steam hiss audio
            if (m_SteamHissClip != null)
            {
                AudioSource.PlayClipAtPoint(m_SteamHissClip, hitPoint, 0.6f);
            }
        }

        private void HandleIgnited(Flammable flammable)
        {
            if (m_FlameParticles != null) m_FlameParticles.Play();
            if (m_SmokeParticles != null) m_SmokeParticles.Play();
            if (m_FireLight != null) m_FireLight.enabled = true;
            if (m_FireAudioSource != null) m_FireAudioSource.Play();
        }

        private void HandleExtinguished(Flammable flammable)
        {
            if (m_FlameParticles != null) m_FlameParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            if (m_FireLight != null) m_FireLight.enabled = false;
            if (m_FireAudioSource != null) m_FireAudioSource.Stop();

            // Emit a final puff of steam and white smoke
            if (m_SteamParticles != null)
            {
                m_SteamParticles.Emit(30);
            }
        }

        private void HandleBurnedOut(Flammable flammable)
        {
            HandleExtinguished(flammable);

            // Darken material to charred black
            if (m_TargetRenderer != null)
            {
                m_TargetRenderer.GetPropertyBlock(m_PropBlock);
                m_PropBlock.SetColor("_BaseColor", m_CharredColor);
                m_PropBlock.SetColor("_Color", m_CharredColor);
                m_TargetRenderer.SetPropertyBlock(m_PropBlock);
            }
        }
    }
}

