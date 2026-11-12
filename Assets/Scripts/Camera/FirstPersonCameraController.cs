using System.Collections.Generic;
using UnityEngine;
using FlameFighter.Input;

namespace FlameFighter.CameraSystem
{
    public class FirstPersonCameraController : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The player body transform rotated horizontally (Yaw). If null, defaults to this GameObject.")]
        [SerializeField] private Transform m_PlayerBody;

        [Tooltip("The camera eye/pitch pivot rotated vertically. If null, finds or creates CameraTarget child.")]
        [SerializeField] private Transform m_CameraPitchRoot;

        [Tooltip("Input reader source. If null, finds one on player or self.")]
        [SerializeField] private PlayerInputReader m_InputReader;

        [Header("Mesh / Arms Pitch Following")]
        [Tooltip("Transforms (such as arms rig, weapon mesh, hose) that should pitch up and down with the camera.")]
        [SerializeField] private List<Transform> m_PitchFollowTargets = new List<Transform>();

        [Header("Settings")]
        [SerializeField] private CameraSettings m_Settings;

        // Internal rotation state
        private float m_CurrentYaw;
        private float m_CurrentPitch;

        // Damping velocities
        private float m_YawVelocity;
        private float m_PitchVelocity;

        // Recoil kick offset (applied to pitch and yaw)
        private Vector2 m_CurrentKick;
        private Vector2 m_TargetKick;

        // Camera trauma / smoke cough shake
        private float m_ShakeTrauma;
        private Vector3 m_InitialLocalPosition;

        // Stored initial rotations for pitch follow targets
        private readonly List<Quaternion> m_InitialFollowerRotations = new List<Quaternion>();

        public Transform CameraPitchRoot => m_CameraPitchRoot;
        public float CurrentPitch => m_CurrentPitch;
        public float CurrentYaw => m_CurrentYaw;

        private void Awake()
        {
            InitializePlayerAndCamera();
            CacheFollowerRotations();
        }

        private void InitializePlayerAndCamera()
        {
            if (m_PlayerBody == null)
            {
                m_PlayerBody = transform;
            }

            // Ensure CameraPitchRoot is a distinct child object (never the player body root)
            if (m_CameraPitchRoot == null || m_CameraPitchRoot == m_PlayerBody)
            {
                Transform foundTarget = transform.Find("CameraTarget");
                if (foundTarget != null)
                {
                    m_CameraPitchRoot = foundTarget;
                }
                else
                {
                    GameObject targetObj = new GameObject("CameraTarget");
                    targetObj.transform.SetParent(transform);
                    targetObj.transform.localPosition = new Vector3(0f, 1.6f, 0f);
                    targetObj.transform.localRotation = Quaternion.identity;
                    m_CameraPitchRoot = targetObj.transform;
                }
            }

            m_InitialLocalPosition = m_CameraPitchRoot.localPosition;

            // Ensure the main camera is parented directly to CameraPitchRoot
            Camera mainCam = Camera.main;
            if (mainCam == null)
            {
                mainCam = GetComponentInChildren<Camera>();
            }

            if (mainCam != null)
            {
                // Disable any Cinemachine brain that could lock rotation
                var components = mainCam.GetComponents<Component>();
                foreach (var comp in components)
                {
                    if (comp != null && comp.GetType().Name.Contains("Cinemachine"))
                    {
                        if (comp is Behaviour b) b.enabled = false;
                    }
                }

                // Parent under CameraPitchRoot if needed
                if (mainCam.transform.parent != m_CameraPitchRoot)
                {
                    mainCam.transform.SetParent(m_CameraPitchRoot);
                    mainCam.transform.localPosition = Vector3.zero;
                    mainCam.transform.localRotation = Quaternion.identity;
                    mainCam.transform.localScale = Vector3.one;
                }
            }

            if (m_InputReader == null)
            {
                m_InputReader = GetComponent<PlayerInputReader>();
                if (m_InputReader == null)
                {
                    m_InputReader = GetComponentInParent<PlayerInputReader>();
                }
            }

            // Initialize angles from current orientation
            m_CurrentYaw = m_PlayerBody.eulerAngles.y;
            m_CurrentPitch = m_CameraPitchRoot.localEulerAngles.x;
            if (m_CurrentPitch > 180f) m_CurrentPitch -= 360f;
        }

        private void CacheFollowerRotations()
        {
            m_InitialFollowerRotations.Clear();
            if (m_PitchFollowTargets != null)
            {
                for (int i = 0; i < m_PitchFollowTargets.Count; i++)
                {
                    if (m_PitchFollowTargets[i] != null)
                    {
                        m_InitialFollowerRotations.Add(m_PitchFollowTargets[i].localRotation);
                    }
                    else
                    {
                        m_InitialFollowerRotations.Add(Quaternion.identity);
                    }
                }
            }
        }

        private void LateUpdate()
        {
            // Only process look if cursor is locked
            if (Cursor.lockState != CursorLockMode.Locked) return;

            Vector2 lookInput = m_InputReader != null ? m_InputReader.LookInput : Vector2.zero;

            // Fallback to direct mouse delta if input reader is not yet delivering
            if (lookInput.sqrMagnitude < 0.0001f && UnityEngine.InputSystem.Mouse.current != null)
            {
                lookInput = UnityEngine.InputSystem.Mouse.current.delta.ReadValue();
            }

            float sensX = (m_Settings != null) ? m_Settings.MouseSensitivityX : 1.8f;
            float sensY = (m_Settings != null) ? m_Settings.MouseSensitivityY : 1.8f;
            bool invertY = (m_Settings != null) && m_Settings.InvertY;
            float minPitch = (m_Settings != null) ? m_Settings.MinPitch : -85f;
            float maxPitch = (m_Settings != null) ? m_Settings.MaxPitch : 85f;
            bool smoothing = (m_Settings != null) && m_Settings.EnableSmoothing;
            float smoothTime = (m_Settings != null) ? m_Settings.SmoothTime : 0.02f;
            float kickReturnSpeed = (m_Settings != null) ? m_Settings.KickReturnSpeed : 10f;

            // Calculate rotation increments
            float mouseX = lookInput.x * sensX * 0.1f;
            float mouseY = lookInput.y * sensY * 0.1f * (invertY ? -1f : 1f);

            // Target angles (mouse up decreases pitch to look upward)
            float targetYaw = m_CurrentYaw + mouseX;
            float targetPitch = Mathf.Clamp(m_CurrentPitch - mouseY, minPitch, maxPitch);

            // Apply smoothing or direct tracking
            if (smoothing)
            {
                m_CurrentYaw = Mathf.SmoothDampAngle(m_CurrentYaw, targetYaw, ref m_YawVelocity, smoothTime);
                m_CurrentPitch = Mathf.SmoothDamp(m_CurrentPitch, targetPitch, ref m_PitchVelocity, smoothTime);
            }
            else
            {
                m_CurrentYaw = targetYaw;
                m_CurrentPitch = targetPitch;
            }

            // Decay recoil kick
            m_CurrentKick = Vector2.Lerp(m_CurrentKick, m_TargetKick, Time.deltaTime * kickReturnSpeed);
            m_TargetKick = Vector2.Lerp(m_TargetKick, Vector2.zero, Time.deltaTime * kickReturnSpeed);

            // Apply horizontal rotation to player body (Yaw)
            if (m_PlayerBody != null)
            {
                m_PlayerBody.rotation = Quaternion.Euler(0f, m_CurrentYaw + m_CurrentKick.x, 0f);
            }

            // Apply vertical pitch + kick to camera pitch root (Pitch)
            Quaternion pitchRotation = Quaternion.Euler(m_CurrentPitch - m_CurrentKick.y, 0f, 0f);
            if (m_CameraPitchRoot != null)
            {
                m_CameraPitchRoot.localRotation = pitchRotation;
            }

            // Apply vertical pitch to any designated follow targets (arms, meshes, weapons)
            if (m_PitchFollowTargets != null)
            {
                for (int i = 0; i < m_PitchFollowTargets.Count; i++)
                {
                    Transform target = m_PitchFollowTargets[i];
                    if (target != null && target != m_CameraPitchRoot)
                    {
                        Quaternion initialRot = (i < m_InitialFollowerRotations.Count) ? m_InitialFollowerRotations[i] : Quaternion.identity;
                        target.localRotation = pitchRotation * initialRot;
                    }
                }
            }

            // Decay trauma shake
            if (m_ShakeTrauma > 0f && m_CameraPitchRoot != null)
            {
                m_ShakeTrauma = Mathf.Max(0f, m_ShakeTrauma - Time.deltaTime * 0.8f);
                float shakeAmount = m_ShakeTrauma * m_ShakeTrauma;
                float shakeX = (Mathf.PerlinNoise(Time.time * 25f, 0f) * 2f - 1f) * shakeAmount * 0.04f;
                float shakeY = (Mathf.PerlinNoise(0f, Time.time * 25f) * 2f - 1f) * shakeAmount * 0.04f;
                m_CameraPitchRoot.localPosition = m_InitialLocalPosition + new Vector3(shakeX, shakeY, 0f);
            }
            else if (m_CameraPitchRoot != null)
            {
                m_CameraPitchRoot.localPosition = m_InitialLocalPosition;
            }
        }

        /// <summary>
        /// Registers a mesh or transform to tilt up/down with the camera.
        /// </summary>
        public void AddPitchFollowTarget(Transform target)
        {
            if (target != null && !m_PitchFollowTargets.Contains(target))
            {
                m_PitchFollowTargets.Add(target);
                m_InitialFollowerRotations.Add(target.localRotation);
            }
        }

        /// <summary>
        /// Adds a camera kick impulse (e.g. from spraying the hose).
        /// kick.x affects yaw, kick.y affects upward pitch.
        /// </summary>
        public void AddKick(Vector2 kick)
        {
            m_TargetKick += kick;
        }

        /// <summary>
        /// Adds trauma for horror smoke coughing or impact shake (normalized 0 to 1).
        /// </summary>
        public void AddTrauma(float trauma)
        {
            m_ShakeTrauma = Mathf.Clamp01(m_ShakeTrauma + trauma);
        }
    }
}
