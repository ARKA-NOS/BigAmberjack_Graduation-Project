using UnityEngine;

namespace Lrw.Script.Agent.Players.Modules
{
    public interface IControlMovement
    {
        float MovementDirectionX { get; }
        void SetMovementDirectionX(float movementXInput);
        void MoveLock(bool value);
        void AddForce(Vector2 force, ForceMode2D impulse = ForceMode2D.Impulse);
    }
}