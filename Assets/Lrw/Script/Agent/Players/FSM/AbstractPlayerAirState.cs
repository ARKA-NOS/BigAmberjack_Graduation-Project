using Lrw.Script.Agent.Players.Modules;
using UnityEngine;

namespace Lrw.Script.Agent.Players.FSM
{
    public abstract class AbstractPlayerAirState : AbstractPlayerState
    {

        protected AbstractPlayerAirState(Agent agent, int stateClipHash, int layerIndex) : base(agent, stateClipHash, layerIndex)
        {
            
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
            _renderer.SetFlip(movementXKey);
        }
    }
}