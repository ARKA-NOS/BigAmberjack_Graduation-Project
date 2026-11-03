using Lrw.Script.Agent.Players.Enum;
using Lrw.Script.Agent.Players.Modules;
using UnityEngine;

namespace Lrw.Script.Agent.Players.FSM
{
    public class PlayerFallState : AbstractPlayerAirState
    {
        public PlayerFallState(Agent agent, int stateClipHash, int layerIndex) : base(agent, stateClipHash, layerIndex)
        {
            
        }
        
        public override void Update()
        {
            base.Update();
            HandleIdleStateChange();
        }
        
        private void HandleIdleStateChange()
        {
            if (!GroundChecker.FixedIsGround) return;
            ControlMultiJumper.ResetMultiJumpCount();
            Player.ChangeState(PlayerStateEnum.IDLE);
        }
        
    }
}