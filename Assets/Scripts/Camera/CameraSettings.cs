using UnityEngine;

namespace FlameFighter.CameraSystem
{
    [CreateAssetMenu(fileName = "CameraSettings", menuName = "Flame Fighter/Camera Settings")]
    public class CameraSettings : ScriptableObject
    {
        [Header("Sensitivity")]
        [Tooltip("Mouse horizontal sensitivity multiplier.")]
        [SerializeField] private float m_MouseSensitivityX = 1.8f;

        [Tooltip("Mouse vertical sensitivity multiplier.")]
        [SerializeField] private float m_MouseSensitivityY = 1.8f;

        [Tooltip("Invert vertical look axis.")]
        [SerializeField] private bool m_InvertY = false;

        [Header("Clamping")]
        [Tooltip("Minimum vertical look angle (looking down, degrees).")]
        [Range(-90f, 0f)]
        [SerializeField] private float m_MinPitch = -85f;

        [Tooltip("Maximum vertical look angle (looking up, degrees).")]
        [Range(0f, 90f)]
        [SerializeField] private float m_MaxPitch = 85f;

        [Header("Smoothing")]
        [Tooltip("Enable smooth damping for camera rotation.")]
        [SerializeField] private bool m_EnableSmoothing = false;

        [Tooltip("Smoothing time in seconds (lower is snappier).")]
        [Range(0.001f, 0.1f)]
        [SerializeField] private float m_SmoothTime = 0.02f;

        [Header("Recoil / Kick Recovery")]
        [Tooltip("Speed at which camera recoil/kick returns to center.")]
        [SerializeField] private float m_KickReturnSpeed = 10f;

        // Public Accessors
        public float MouseSensitivityX => m_MouseSensitivityX;
        public float MouseSensitivityY => m_MouseSensitivityY;
        public bool InvertY => m_InvertY;
        public float MinPitch => m_MinPitch;
        public float MaxPitch => m_MaxPitch;
        public bool EnableSmoothing => m_EnableSmoothing;
        public float SmoothTime => m_SmoothTime;
        public float KickReturnSpeed => m_KickReturnSpeed;
    }
}

