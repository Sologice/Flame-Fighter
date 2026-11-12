using UnityEngine;
using FlameFighter.Hose;
using FlameFighter.Interaction;

namespace FlameFighter.Environment
{
    public class WaterSource : MonoBehaviour, IInteractable
    {
        [Header("Water Source Configuration")]
        [Tooltip("Amount of water refilled per interaction (or instant full refill).")]
        [SerializeField] private float m_RefillAmount = 100f;

        [Tooltip("If true, refilling completely fills the tank instantly.")]
        [SerializeField] private bool m_InstantFullRefill = true;

        [Tooltip("Audio played when refilling.")]
        [SerializeField] private AudioSource m_RefillAudio;

        public string PromptText => "Hold [E] to Refill Water";

        public bool CanInteract(GameObject interactor)
        {
            if (interactor == null) return false;
            HoseController hose = interactor.GetComponentInChildren<HoseController>();
            if (hose == null) return false;

            return hose.CurrentWater < hose.MaxWater;
        }

        public void Interact(GameObject interactor)
        {
            if (interactor == null) return;
            HoseController hose = interactor.GetComponentInChildren<HoseController>();
            if (hose == null) return;

            float amountToRefill = m_InstantFullRefill ? hose.MaxWater : m_RefillAmount;
            hose.Refill(amountToRefill);

            if (m_RefillAudio != null)
            {
                m_RefillAudio.Play();
            }

            Debug.Log($"<color=#29B6F6><b>[WaterSource]</b></color> Refilled water tank! Current: {hose.CurrentWater:F0}L");
        }
    }
}

