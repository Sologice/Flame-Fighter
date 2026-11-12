using UnityEngine;
using FlameFighter.Input;

namespace FlameFighter.CameraSystem
{
    public class FirstPersonCinemachineController : MonoBehaviour
    {
        [Header("Camera Target")]
        [Tooltip("The camera eye/pitch pivot rotated vertically.")]
        [SerializeField] private Transform m_CinemachineCameraTarget;

        [Header("References")]
        [Tooltip("The player root transform rotated horizontally for yaw.")]
        [SerializeField] private Transform m_PlayerBody;

        [Tooltip("Input reader source.")]
        [SerializeField] private PlayerInputReader m_InputReader;

        [Header("Settings")]
        [SerializeField] private CameraSettings m_Settings;

        // Current rotation state
        private float m_CurrentYaw;
        private float m_CurrentPitch;

        // Smoothing velocities
        private float m_YawVelocity;
        private float m_PitchVelocity;

        // Recoil kick offset
        private Vector2 m_CurrentKick;
        private Vector2 m_TargetKick;

        // Horror smoke trauma
        private float m_Trauma;
        private Vector3 m_InitialTargetLocalPosition;

        public Transform CameraTarget => m_CinemachineCameraTarget;

        private void Awake()
        {
            if (m_PlayerBody == null)
            {
                m_PlayerBody = transform;
            }

            if (m_CinemachineCameraTarget == null || m_CinemachineCameraTarget == m_PlayerBody)
            {
                Transform foundTarget = transform.Find("CameraTarget");
                if (foundTarget != null)
                {
                    m_CinemachineCameraTarget = foundTarget;
                }
                else
                {
                    GameObject targetObj = new GameObject("CameraTarget");
                    targetObj.transform.SetParent(transform);
                    targetObj.transform.localPosition = new Vector3(0f, 1.6f, 0f);
                    targetObj.transform.localRotation = Quaternion.identity;
                    m_CinemachineCameraTarget = targetObj.transform;
                }
            }

            m_InitialTargetLocalPosition = m_CinemachineCameraTarget.localPosition;

            // Ensure Main Camera is parented to CameraTarget if not using Cinemachine virtual camera
            Camera mainCam = Camera.main;
            if (mainCam == null)
            {
                mainCam = GetComponentInChildren<Camera>();
            }

            if (mainCam != null)
            {
                // Disable any Cinemachine brain that could override rotation
                var components = mainCam.GetComponents<Component>();
                foreach (var comp in components)
                {
                    if (comp != null && comp.GetType().Name.Contains("Cinemachine"))
                    {
                        if (comp is Behaviour b) b.enabled = false;
                    }
                }

                if (mainCam.transform.parent != m_CinemachineCameraTarget)
                {
                    mainCam.transform.SetParent(m_CinemachineCameraTarget);
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

            m_CurrentYaw = m_PlayerBody.eulerAngles.y;
            m_CurrentPitch = m_CinemachineCameraTarget.localEulerAngles.x;
            if (m_CurrentPitch > 180f) m_CurrentPitch -= 360f;
        }

        private void LateUpdate()
        {
            if (Cursor.lockState != CursorLockMode.Locked) return;

            Vector2 lookInput = m_InputReader != null ? m_InputReader.LookInput : Vector2.zero;
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

            float mouseX = lookInput.x * sensX * 0.1f;
            float mouseY = lookInput.y * sensY * 0.1f * (invertY ? -1f : 1f);

            float targetYaw = m_CurrentYaw + mouseX;
            float targetPitch = Mathf.Clamp(m_CurrentPitch - mouseY, minPitch, maxPitch);

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

            m_CurrentKick = Vector2.Lerp(m_CurrentKick, m_TargetKick, Time.deltaTime * kickReturnSpeed);
            m_TargetKick = Vector2.Lerp(m_TargetKick, Vector2.zero, Time.deltaTime * kickReturnSpeed);

            if (m_PlayerBody != null)
            {
                m_PlayerBody.rotation = Quaternion.Euler(0f, m_CurrentYaw + m_CurrentKick.x, 0f);
            }

            if (m_CinemachineCameraTarget != null)
            {
                m_CinemachineCameraTarget.localRotation = Quaternion.Euler(m_CurrentPitch - m_CurrentKick.y, 0f, 0f);
            }

            if (m_Trauma > 0f && m_CinemachineCameraTarget != null)
            {
                m_Trauma = Mathf.Max(0f, m_Trauma - Time.deltaTime * 0.8f);
                float shakeIntensity = m_Trauma * m_Trauma;
                float shakeX = (Mathf.PerlinNoise(Time.time * 20f, 0f) * 2f - 1f) * shakeIntensity * 0.04f;
                float shakeY = (Mathf.PerlinNoise(0f, Time.time * 20f) * 2f - 1f) * shakeIntensity * 0.04f;
                m_CinemachineCameraTarget.localPosition = m_InitialTargetLocalPosition + new Vector3(shakeX, shakeY, 0f);
            }
            else if (m_CinemachineCameraTarget != null)
            {
                m_CinemachineCameraTarget.localPosition = m_InitialTargetLocalPosition;
            }
        }

        public void AddKick(Vector2 kick)
        {
            m_TargetKick += kick;
        }

        public void AddTrauma(float amount)
        {
            m_Trauma = Mathf.Clamp01(m_Trauma + amount);
        }
    }
}
