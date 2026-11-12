using UnityEngine;

namespace FlameFighter.Interaction
{
    public interface IInteractable
    {
        string PromptText { get; }
        bool CanInteract(GameObject interactor);
        void Interact(GameObject interactor);
    }
}

