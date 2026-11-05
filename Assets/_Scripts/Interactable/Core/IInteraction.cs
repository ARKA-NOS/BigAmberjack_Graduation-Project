namespace _Scripts.Interactable.Core
{
    public interface IInteraction
    {
        bool CanInteract { get; }

        string InteractionPrompt { get; }

        void Interaction();
    }
}