using Lrw.Script.Agent.Players.Enum;
using Lrw.Script.Agent.Players.Modules;
using UnityEngine;

namespace Lrw.Script.Agent.Players.FSM
{
    public class PlayerFallState : AbstractPlayerAirState
    {
        private float _fallTime;
        
        public PlayerFallState(Agent agent, int stateClipHash, int layerIndex) : base(agent, stateClipHash, layerIndex)
        {
                        
        }
        
        public override void Enter()
        {
            base.Enter();
            _fallTime = 0;
        }
        
        public override void Update()
        {
            base.Update();
            HandleIdleStateChange();
            
            _fallTime += Time.deltaTime;
        }
        
        private void HandleIdleStateChange()
        {
            if (!GroundChecker.FixedIsGround) return;
            Player.ChangeState(PlayerStateEnum.IDLE);
            ControlMultiJumper.ResetMultiJumpCount();
        }
        
        protected override void HandleJump()
        {
            base.HandleJump();
            if (_fallTime < ControlJumper.CoyoteTime)
            {
                Player.ChangeState(PlayerStateEnum.JUMP);
            }
        }
        
    }
}