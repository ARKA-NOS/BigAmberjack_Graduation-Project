namespace SDW.Scripts.CoreSystem
{
    public interface IInteraction
    {
        bool CanInteract { get; }

        string InteractionPrompt { get; }

        void Interaction();
    }
}