using _Scripts.Agent.Players.Enum;
using UnityEngine;

namespace _Scripts.Agent.Players.FSM
{
    public class PlayerIdleState : AbstractPlayerGroundState
    {
        public PlayerIdleState(Agent agent, int stateClipHash, int layerIndex) : base(agent, stateClipHash, layerIndex)
        {
        }

        public override void Enter()
        {
            base.Enter();
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
            if (Mathf.Abs(movementXKey) < InputDeadZone) return;
            Player.ChangeState(PlayerStateEnum.RUN);
        }
    }
}