using Agents.Players.Enum;
using UnityEngine;

namespace Agents.Players.FSM
{
    public class PlayerIdleState : AbstractPlayerGroundState
    {
        public PlayerIdleState(Agent agent, int stateClipHash, int layerIndex) : base(agent, stateClipHash, layerIndex)
        {
        }

        public override void Enter(float transitionDuration = 0.1f)
        {
            base.Enter(transitionDuration);
            ControlMovement.SetMovementDirectionX(0);
        }

        public override void Update()
        {
            base.Update();
            if (TryChangeToFall())
                return;
            HandleMovementStateChange(Player.PlayerInput.MovementKey.x);
        }

        private void HandleMovementStateChange(float movementXKey)
        {
            if (Mathf.Abs(movementXKey) < INPUT_DEADZONE) return;
            Player.ChangeState(PlayerStateEnum.RUN, 0.1f);
        }
    }
}