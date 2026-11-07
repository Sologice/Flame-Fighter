using UnityEngine;

namespace FlameFighter.Player
{
    [CreateAssetMenu(fileName = "PlayerMovementSettings", menuName = "Flame Fighter/Player Movement Settings")]
    public class PlayerMovementSettings : ScriptableObject
    {
        [Header("Movement Speeds")]
        [Tooltip("Standard walking speed in m/s.")]
        [SerializeField] private float m_WalkSpeed = 4.5f;

        [Tooltip("Sprinting speed in m/s.")]
        [SerializeField] private float m_SprintSpeed = 7.5f;

        [Tooltip("Rate of acceleration when speeding up (units/sec^2).")]
        [SerializeField] private float m_Acceleration = 16f;

        [Tooltip("Rate of deceleration when stopping or changing direction (units/sec^2).")]
        [SerializeField] private float m_Deceleration = 20f;

        [Header("Jumping & Gravity")]
        [Tooltip("Height of jump in meters (0.6 - 0.8 is standard for grounded FPS).")]
        [SerializeField] private float m_JumpHeight = 0.75f;

        [Tooltip("Downward acceleration due to gravity (should be negative, -28 to -35 for snappy feel).")]
        [SerializeField] private float m_Gravity = -32f;

        [Tooltip("Gravity multiplier applied while falling downwards to remove floatiness.")]
        [SerializeField] private float m_FallGravityMultiplier = 1.3f;

        [Tooltip("Maximum downward falling speed.")]
        [SerializeField] private float m_TerminalVelocity = -45f;

        [Tooltip("Constant downward velocity applied while grounded to stick to uneven terrain.")]
        [SerializeField] private float m_StickToGroundForce = -3f;

        [Header("Ground Check")]
        [Tooltip("Radius of the spherecast used for ground detection.")]
        [SerializeField] private float m_GroundCheckRadius = 0.28f;

        [Tooltip("Vertical offset from the bottom of the character controller for ground spherecast.")]
        [SerializeField] private float m_GroundCheckOffset = 0.15f;

        [Tooltip("Layers recognized as walkable solid ground.")]
        [SerializeField] private LayerMask m_GroundLayers = ~0;

        [Header("Footsteps Cadence")]
        [Tooltip("Accumulated distance in meters between footstep triggers.")]
        [SerializeField] private float m_WalkStepDistance = 2.0f;

        [Tooltip("Accumulated distance in meters between footstep triggers while sprinting.")]
        [SerializeField] private float m_SprintStepDistance = 1.4f;

        // Public Accessors
        public float WalkSpeed => m_WalkSpeed;
        public float SprintSpeed => m_SprintSpeed;
        public float Acceleration => m_Acceleration;
        public float Deceleration => m_Deceleration;
        public float JumpHeight => m_JumpHeight;
        public float Gravity => m_Gravity;
        public float FallGravityMultiplier => m_FallGravityMultiplier;
        public float TerminalVelocity => m_TerminalVelocity;
        public float StickToGroundForce => m_StickToGroundForce;
        public float GroundCheckRadius => m_GroundCheckRadius;
        public float GroundCheckOffset => m_GroundCheckOffset;
        public LayerMask GroundLayers => m_GroundLayers;
        public float WalkStepDistance => m_WalkStepDistance;
        public float SprintStepDistance => m_SprintStepDistance;
    }
}
