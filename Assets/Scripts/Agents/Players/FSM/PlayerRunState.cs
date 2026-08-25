using Agents.Players.Enum;
using UnityEngine;

namespace Agents.Players.FSM
{
    public class PlayerRunState : AbstractPlayerGroundState
    {
        public PlayerRunState(Agent agent, int stateClipHash, int layerIndex) : base(agent, stateClipHash, layerIndex)
        {
        }

        public override void Update()
        {
            base.Update();
            if (TryChangeToFall())
                return;
            float inputX = Player.PlayerInput.MovementKey.x;
            UpdateMovementDirectionX(inputX);
            HandleIdleStateChange(inputX);
        }

        private void UpdateMovementDirectionX(float movementXKey)
        {
            ControlMovement.SetMovementDirectionX(movementXKey);
            ControlMovement.UpdateFacingDirection(movementXKey);
        }

        private void HandleIdleStateChange(float movementXKey)
        {
            if (Mathf.Abs(movementXKey) > INPUT_DEADZONE) return;
            Player.ChangeState(PlayerStateEnum.IDLE, 0.1f);
        }
    }
}