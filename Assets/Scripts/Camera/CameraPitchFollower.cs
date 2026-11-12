using UnityEngine;
using FlameFighter.Input;

namespace FlameFighter.CameraSystem
{
    /// <summary>
    /// Attaches to any mesh, weapon, or arms rig to make it pitch up and down with the camera.
    /// Can also add procedural weapon sway/lag when rotating.
    /// </summary>
    public class CameraPitchFollower : MonoBehaviour
    {
        [Header("Target")]
        [Tooltip("The camera pitch pivot to follow. If null, automatically finds CameraTarget or Camera.main.")]
        [SerializeField] private Transform m_CameraPitchRoot;

        [Header("Pitch Settings")]
        [Tooltip("How strongly this object follows the camera pitch (1 = 100% exact match).")]
        [Range(0f, 1f)]
        [SerializeField] private float m_PitchWeight = 1.0f;

        [Tooltip("Smooth time for pitch following. Set to 0 for instant locking.")]
        [SerializeField] private float m_PitchSmoothTime = 0.01f;

        [Header("Weapon Look Sway (Optional)")]
        [Tooltip("Enables natural inertia/sway when moving the mouse.")]
        [SerializeField] private bool m_EnableSway = true;

        [Tooltip("Intensity of the sway rotation.")]
        [SerializeField] private float m_SwayIntensity = 1.5f;

        [Tooltip("Maximum angle the weapon can sway away from center.")]
        [SerializeField] private float m_MaxSwayAngle = 8f;

        [Tooltip("Speed at which weapon sway returns to center.")]
        [SerializeField] private float m_SwayResetSpeed = 8f;

        [Header("Input Source")]
        [SerializeField] private PlayerInputReader m_InputReader;

        private Quaternion m_InitialLocalRotation;
        private Vector3 m_InitialLocalPosition;
        private float m_CurrentPitch;
        private float m_PitchVelocity;
        private Quaternion m_SwayRotation = Quaternion.identity;

        private void Awake()
        {
            m_InitialLocalRotation = transform.localRotation;
            m_InitialLocalPosition = transform.localPosition;

            if (m_CameraPitchRoot == null)
            {
                // Find CameraTarget on player or in scene
                var camController = GetComponentInParent<FirstPersonCameraController>();
                if (camController != null && camController.CameraPitchRoot != null)
                {
                    m_CameraPitchRoot = camController.CameraPitchRoot;
                }
                else if (Camera.main != null)
                {
                    m_CameraPitchRoot = Camera.main.transform.parent != null ? Camera.main.transform.parent : Camera.main.transform;
                }
            }

            if (m_InputReader == null)
            {
                m_InputReader = GetComponentInParent<PlayerInputReader>();
            }
        }

        private void LateUpdate()
        {
            if (m_CameraPitchRoot == null) return;

            // Get target pitch angle from camera pitch root
            float targetPitch = m_CameraPitchRoot.localEulerAngles.x;
            if (targetPitch > 180f) targetPitch -= 360f;

            targetPitch *= m_PitchWeight;

            // Smooth or instant pitch tracking
            if (m_PitchSmoothTime > 0.001f)
            {
                m_CurrentPitch = Mathf.SmoothDampAngle(m_CurrentPitch, targetPitch, ref m_PitchVelocity, m_PitchSmoothTime);
            }
            else
            {
                m_CurrentPitch = targetPitch;
            }

            Quaternion pitchRot = Quaternion.Euler(m_CurrentPitch, 0f, 0f);

            // Optional procedural look sway (weapon lag)
            if (m_EnableSway && m_InputReader != null)
            {
                Vector2 look = m_InputReader.LookInput;
                float swayYaw = Mathf.Clamp(-look.x * m_SwayIntensity * 0.1f, -m_MaxSwayAngle, m_MaxSwayAngle);
                float swayPitch = Mathf.Clamp(look.y * m_SwayIntensity * 0.1f, -m_MaxSwayAngle, m_MaxSwayAngle);

                Quaternion targetSway = Quaternion.Euler(swayPitch, swayYaw, -swayYaw * 0.5f);
                m_SwayRotation = Quaternion.Slerp(m_SwayRotation, targetSway, Time.deltaTime * m_SwayResetSpeed);
            }
            else
            {
                m_SwayRotation = Quaternion.Slerp(m_SwayRotation, Quaternion.identity, Time.deltaTime * m_SwayResetSpeed);
            }

            // Apply combined pitch + sway + base rotation
            transform.localRotation = pitchRot * m_SwayRotation * m_InitialLocalRotation;
        }

        /// <summary>
        /// Explicitly set the camera pitch root to follow.
        /// </summary>
        public void SetCameraTarget(Transform cameraPitchRoot)
        {
            m_CameraPitchRoot = cameraPitchRoot;
        }
    }
}

