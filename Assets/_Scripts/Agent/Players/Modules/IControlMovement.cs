using UnityEngine;

namespace _Scripts.Agent.Players.Modules
{
    public interface IControlMovement
    {
        float MovementDirectionX { get; }
        void SetMovementDirectionX(float movementXInput);
        void MoveXLock(bool value);
        void SetVelocityX(float velocityX);
        void AddForce(Vector2 force, ForceMode2D impulse = ForceMode2D.Impulse);
    }
}