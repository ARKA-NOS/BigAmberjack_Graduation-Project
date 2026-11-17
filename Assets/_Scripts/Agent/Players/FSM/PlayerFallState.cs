using _Scripts.Agent.Players.Enum;

namespace _Scripts.Agent.Players.FSM
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
            ControlJumper.ResetCoyote();
            Player.ChangeState(PlayerStateEnum.IDLE);
        }
        
    }
}