using UnityEngine;
using FlameFighter.Input;

namespace FlameFighter.Interaction
{
    public class PlayerInteractionHandler : MonoBehaviour
    {
        [Header("Interaction Settings")]
        [Tooltip("Max reach distance for interacting with water sources, valves, hydrants.")]
        [SerializeField] private float m_InteractDistance = 2.5f;

        [Tooltip("Layer mask containing interactable objects.")]
        [SerializeField] private LayerMask m_InteractLayers = ~0;

        [Header("References")]
        [Tooltip("Camera or eye transform raycast origin. If null, uses Camera.main.")]
        [SerializeField] private Transform m_RaycastOrigin;

        [SerializeField] private PlayerInputReader m_InputReader;

        public IInteractable CurrentInteractable { get; private set; }

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
        }

        private void OnEnable()
        {
            if (m_InputReader != null)
            {
                m_InputReader.OnInteractTriggered += HandleInteract;
            }
        }

        private void OnDisable()
        {
            if (m_InputReader != null)
            {
                m_InputReader.OnInteractTriggered -= HandleInteract;
            }
        }

        private void Update()
        {
            DetectInteractable();
        }

        private void DetectInteractable()
        {
            Transform origin = m_RaycastOrigin != null ? m_RaycastOrigin : (UnityEngine.Camera.main != null ? UnityEngine.Camera.main.transform : null);
            if (origin == null)
            {
                CurrentInteractable = null;
                return;
            }

            if (Physics.Raycast(origin.position, origin.forward, out RaycastHit hit, m_InteractDistance, m_InteractLayers, QueryTriggerInteraction.Collide))
            {
                if (hit.collider.TryGetComponent<IInteractable>(out var interactable) && interactable.CanInteract(gameObject))
                {
                    CurrentInteractable = interactable;
                    return;
                }
            }

            CurrentInteractable = null;
        }

        private void HandleInteract()
        {
            if (CurrentInteractable != null && CurrentInteractable.CanInteract(gameObject))
            {
                CurrentInteractable.Interact(gameObject);
            }
        }
    }
}

