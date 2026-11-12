using System;
using FlameFighter.Input;
using UnityEngine;

namespace FlameFighter.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMovementController : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private PlayerMovementSettings m_Settings;

        [Header("References")]
        [SerializeField] private PlayerInputReader m_InputReader;

        // Components
        private CharacterController m_CharacterController;

        // State
        private Vector3 m_HorizontalVelocity;
        private float m_VerticalVelocity;
        private bool m_IsGrounded;
        private Vector3 m_GroundNormal = Vector3.up;
        private float m_StepDistanceAccumulator;

        // Spherecast buffer
        private readonly RaycastHit[] m_GroundHits = new RaycastHit[4];

        // Public Events & Properties
        public event Action<bool> OnStepTaken; // bool isSprinting
        public bool IsGrounded => m_IsGrounded;
        public float CurrentSpeed => m_HorizontalVelocity.magnitude;
        public Vector3 Velocity => m_HorizontalVelocity + Vector3.up * m_VerticalVelocity;

        private void Awake()
        {
            m_CharacterController = GetComponent<CharacterController>();

            if (m_InputReader == null)
            {
                m_InputReader = GetComponent<PlayerInputReader>();
                if (m_InputReader == null)
                {
                    m_InputReader = GetComponentInParent<PlayerInputReader>();
                }
            }
        }

        private void OnEnable()
        {
            if (m_InputReader != null)
            {
                m_InputReader.OnJumpTriggered += HandleJump;
            }
        }

        private void OnDisable()
        {
            if (m_InputReader != null)
            {
                m_InputReader.OnJumpTriggered -= HandleJump;
            }
        }

        private void Update()
        {
            if (m_Settings == null) return;

            UpdateGroundCheck();
            UpdateHorizontalMovement();
            UpdateVerticalMovement();
            ApplyMotion();
            TrackFootsteps();
        }

        private void UpdateGroundCheck()
        {
            Vector3 spherePosition = transform.position + m_CharacterController.center;
            spherePosition.y -= (m_CharacterController.height * 0.5f) - m_Settings.GroundCheckRadius + m_Settings.GroundCheckOffset;

            int hitCount = Physics.SphereCastNonAlloc(
                spherePosition,
                m_Settings.GroundCheckRadius,
                Vector3.down,
                m_GroundHits,
                m_Settings.GroundCheckOffset * 2f,
                m_Settings.GroundLayers,
                QueryTriggerInteraction.Ignore
            );

            if (hitCount > 0)
            {
                m_IsGrounded = true;
                m_GroundNormal = m_GroundHits[0].normal;
            }
            else
            {
                m_IsGrounded = m_CharacterController.isGrounded;
                m_GroundNormal = Vector3.up;
            }
        }

        private void UpdateHorizontalMovement()
        {
            Vector2 moveInput = m_InputReader != null ? m_InputReader.MoveInput : Vector2.zero;

            // Project input to world direction relative to facing
            Vector3 targetDirection = (transform.right * moveInput.x + transform.forward * moveInput.y);
            if (targetDirection.sqrMagnitude > 1f)
            {
                targetDirection.Normalize();
            }

            // Determine target speed
            bool isSprinting = m_InputReader != null && m_InputReader.IsSprinting && moveInput.y > 0.1f;
            float targetSpeed = (moveInput.sqrMagnitude > 0.01f)
                ? (isSprinting ? m_Settings.SprintSpeed : m_Settings.WalkSpeed)
                : 0f;

            Vector3 targetVelocity = targetDirection * targetSpeed;

            // Accelerate or decelerate smoothly
            float rate = (targetSpeed > m_HorizontalVelocity.magnitude) ? m_Settings.Acceleration : m_Settings.Deceleration;
            m_HorizontalVelocity = Vector3.MoveTowards(m_HorizontalVelocity, targetVelocity, rate * Time.deltaTime);
        }

        private void UpdateVerticalMovement()
        {
            if (m_IsGrounded)
            {
                // Stick-to-ground downward pressure while grounded
                if (m_VerticalVelocity < 0f)
                {
                    m_VerticalVelocity = m_Settings.StickToGroundForce;
                }
            }
            else
            {
                // Apply heavier gravity while descending to eliminate floatiness
                float fallMultiplier = (m_VerticalVelocity < 0f) ? m_Settings.FallGravityMultiplier : 3.0f;
                m_VerticalVelocity += m_Settings.Gravity * fallMultiplier * Time.deltaTime;

                if (m_VerticalVelocity < m_Settings.TerminalVelocity)
                {
                    m_VerticalVelocity = m_Settings.TerminalVelocity;
                }
            }
        }

        private void HandleJump()
        {
            if (!m_IsGrounded || m_Settings == null) return;

            // v = sqrt(2 * -g * h)
            m_VerticalVelocity = Mathf.Sqrt(m_Settings.JumpHeight * -1f * m_Settings.Gravity);
            m_IsGrounded = false;
        }

        private void ApplyMotion()
        {
            Vector3 finalMotion = (m_HorizontalVelocity + Vector3.up * m_VerticalVelocity) * Time.deltaTime;
            m_CharacterController.Move(finalMotion);
        }

        private void TrackFootsteps()
        {
            if (!m_IsGrounded || m_HorizontalVelocity.sqrMagnitude < 0.1f)
            {
                return;
            }

            float distanceThisFrame = m_HorizontalVelocity.magnitude * Time.deltaTime;
            m_StepDistanceAccumulator += distanceThisFrame;

            bool isSprinting = m_InputReader != null && m_InputReader.IsSprinting;
            float stepDistance = isSprinting ? m_Settings.SprintStepDistance : m_Settings.WalkStepDistance;

            if (m_StepDistanceAccumulator >= stepDistance)
            {
                m_StepDistanceAccumulator -= stepDistance;
                OnStepTaken?.Invoke(isSprinting);
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (m_Settings == null) return;

            Gizmos.color = m_IsGrounded ? Color.green : Color.red;
            var cc = GetComponent<CharacterController>();
            if (cc != null)
            {
                Vector3 spherePosition = transform.position + cc.center;
                spherePosition.y -= (cc.height * 0.5f) - m_Settings.GroundCheckRadius + m_Settings.GroundCheckOffset;
                Gizmos.DrawWireSphere(spherePosition, m_Settings.GroundCheckRadius);
            }
        }
    }
}

