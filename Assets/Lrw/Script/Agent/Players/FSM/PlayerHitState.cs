using Lrw.Script.Agent.Players.Enum;

namespace Lrw.Script.Agent.Players.FSM
{
    public class PlayerHitState : AbstractPlayerState
    {
        public PlayerHitState(Agent agent, int stateClipHash, int layerIndex) : base(agent, stateClipHash, layerIndex)
        {
            _animationTrigger = agent.GetModule<IAnimationTrigger>();
        }
        
        private IAnimationTrigger _animationTrigger;
        
        public override void Enter()
        {
            base.Enter();
            ControlMovement.MoveLock(true);
            _animationTrigger.OnAnimationEnd += OnOnAnimationEnd;
        }

        private void OnOnAnimationEnd()
        {
            Player.ChangeState(PlayerStateEnum.IDLE);
        }

        public override void Exit()
        {
            base.Exit();
            ControlMovement.MoveLock(false);
            _animationTrigger.OnAnimationEnd -= OnOnAnimationEnd;
        }
        
        
    }
}