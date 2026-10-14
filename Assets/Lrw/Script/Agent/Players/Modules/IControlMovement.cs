namespace Lrw.Script.Agent.Players
{
    public interface IControlMovement
    {
        void SetMovementDirectionX(float movementXInput);
        void MoveLock(bool value);
    }
}