namespace Agents.Players
{
    public interface IControlMovement
    {
        void SetMovementDirectionX(float movementXInput);
        void UpdateFacingDirection(float movementXKey);
    }
}