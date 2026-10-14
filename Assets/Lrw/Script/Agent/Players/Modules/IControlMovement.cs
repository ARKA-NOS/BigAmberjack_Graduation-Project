namespace Lrw.Script.Agent.Players.Modules
{
    public interface IControlMovement
    {
        void SetMovementDirectionX(float movementXInput);
        void MoveLock(bool value);
    }
}