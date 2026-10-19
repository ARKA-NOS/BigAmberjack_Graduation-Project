namespace Lrw.Script.Agent.Players.FSM
{
    public class PlayerDeadState : AbstractPlayerState
    {
        public PlayerDeadState(Agent agent, int stateClipHash, int layerIndex) : base(agent, stateClipHash, layerIndex)
        {
            
        }

        public override void Enter()
        {
            base.Enter();
            ControlMovement.MoveLock(true);
        }

        public override void Exit()
        {
            ControlMovement.MoveLock(false);
        }
    }
}