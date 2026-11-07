using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FlameFighter.Input
{
    [DefaultExecutionOrder(-100)]
    public class PlayerInputReader : MonoBehaviour
    {
        [Header("Action Asset")]
        [Tooltip("The dedicated FlameFighter_Actions asset. If not assigned, will attempt to load from Resources or create fallback bindings.")]
        [SerializeField] private InputActionAsset m_ActionsAsset;

        [Header("Cursor Settings")]
        [SerializeField] private bool m_LockCursorOnStart = true;

        // Action Map and Actions
        private InputActionMap m_PlayerMap;
        private InputAction m_MoveAction;
        private InputAction m_LookAction;
        private InputAction m_JumpAction;
        private InputAction m_SprintAction;
        private InputAction m_SprayAction;
        private InputAction m_InteractAction;

        // Current Input States
        public Vector2 MoveInput { get; private set; }
        public Vector2 LookInput { get; private set; }
        public bool IsSprinting { get; private set; }
        public bool IsSpraying { get; private set; }

        // Action Events
        public event Action OnJumpTriggered;
        public event Action OnInteractTriggered;

        private void Awake()
        {
            InitializeActions();
        }

        private void InitializeActions()
        {
            if (m_ActionsAsset != null)
            {
                m_PlayerMap = m_ActionsAsset.FindActionMap("Player");
            }

            // Fallback: If no asset was assigned, create the action map programmatically
            if (m_PlayerMap == null)
            {
                CreateFallbackActionMap();
            }

            if (m_PlayerMap != null)
            {
                m_MoveAction = m_PlayerMap.FindAction("Move");
                m_LookAction = m_PlayerMap.FindAction("Look");
                m_JumpAction = m_PlayerMap.FindAction("Jump");
                m_SprintAction = m_PlayerMap.FindAction("Sprint");
                m_SprayAction = m_PlayerMap.FindAction("Spray");
                m_InteractAction = m_PlayerMap.FindAction("Interact");

                if (m_JumpAction != null)
                {
                    m_JumpAction.performed += OnJumpPerformed;
                }

                if (m_InteractAction != null)
                {
                    m_InteractAction.performed += OnInteractPerformed;
                }
            }
        }

        private void CreateFallbackActionMap()
        {
            m_PlayerMap = new InputActionMap("Player");

            // Move (WASD / Left Stick)
            m_MoveAction = m_PlayerMap.AddAction("Move", InputActionType.Value);
            m_MoveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            m_MoveAction.AddBinding("<Gamepad>/leftStick");

            // Look (Mouse Delta / Right Stick)
            m_LookAction = m_PlayerMap.AddAction("Look", InputActionType.Value);
            m_LookAction.AddBinding("<Mouse>/delta");
            m_LookAction.AddBinding("<Pointer>/delta");
            m_LookAction.AddBinding("<Gamepad>/rightStick");

            // Jump (Space / Button South)
            m_JumpAction = m_PlayerMap.AddAction("Jump", InputActionType.Button);
            m_JumpAction.AddBinding("<Keyboard>/space");
            m_JumpAction.AddBinding("<Gamepad>/buttonSouth");

            // Sprint (Shift / Left Stick Press)
            m_SprintAction = m_PlayerMap.AddAction("Sprint", InputActionType.Button);
            m_SprintAction.AddBinding("<Keyboard>/leftShift");
            m_SprintAction.AddBinding("<Gamepad>/leftStickPress");

            // Spray (LMB / Right Trigger)
            m_SprayAction = m_PlayerMap.AddAction("Spray", InputActionType.Button);
            m_SprayAction.AddBinding("<Mouse>/leftButton");
            m_SprayAction.AddBinding("<Gamepad>/rightTrigger");

            // Interact (E / Button West)
            m_InteractAction = m_PlayerMap.AddAction("Interact", InputActionType.Button);
            m_InteractAction.AddBinding("<Keyboard>/e");
            m_InteractAction.AddBinding("<Gamepad>/buttonWest");
        }

        private void OnEnable()
        {
            m_PlayerMap?.Enable();

            if (m_LockCursorOnStart)
            {
                SetCursorLocked(true);
            }
        }

        private void OnDisable()
        {
            m_PlayerMap?.Disable();
            SetCursorLocked(false);
        }

        private void OnDestroy()
        {
            if (m_JumpAction != null)
            {
                m_JumpAction.performed -= OnJumpPerformed;
            }

            if (m_InteractAction != null)
            {
                m_InteractAction.performed -= OnInteractPerformed;
            }
        }

        private void Update()
        {
            if (m_PlayerMap == null) return;

            // Poll continuous movement and look values
            MoveInput = m_MoveAction != null ? m_MoveAction.ReadValue<Vector2>() : Vector2.zero;
            LookInput = m_LookAction != null ? m_LookAction.ReadValue<Vector2>() : Vector2.zero;

            IsSprinting = m_SprintAction != null && m_SprintAction.IsPressed();
            IsSpraying = m_SprayAction != null && m_SprayAction.IsPressed();

            // Press Escape to toggle cursor lock in Editor
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                SetCursorLocked(Cursor.lockState != CursorLockMode.Locked);
            }
        }

        private void OnJumpPerformed(InputAction.CallbackContext context)
        {
            OnJumpTriggered?.Invoke();
        }

        private void OnInteractPerformed(InputAction.CallbackContext context)
        {
            OnInteractTriggered?.Invoke();
        }

        public void SetCursorLocked(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
