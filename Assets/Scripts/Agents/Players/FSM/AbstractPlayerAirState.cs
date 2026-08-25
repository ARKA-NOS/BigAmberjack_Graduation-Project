using UnityEngine;

namespace Agents.Players.FSM
{
    public abstract class AbstractPlayerAirState : AbstractPlayerState
    {
        protected IControlJumper ControlJumper;
        
        protected AbstractPlayerAirState(Agent agent, int stateClipHash, int layerIndex) : base(agent, stateClipHash, layerIndex)
        {
            ControlJumper = Player.GetModule<IControlJumper>();
            Debug.Assert(ControlJumper != null, "플레이어 점퍼가 없어요!!!");
        }

        public override void Update()
        {
            base.Update();
            float inputX = Player.PlayerInput.MovementKey.x;
            UpdateMovementDirectionX(inputX);
        }

        private void UpdateMovementDirectionX(float movementXKey)
        {
            ControlMovement.SetMovementDirectionX(movementXKey);
            ControlMovement.UpdateFacingDirection(movementXKey);
        }
    }
}