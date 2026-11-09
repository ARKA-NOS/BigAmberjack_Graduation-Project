namespace _Scripts.Agent.Players.FSM
{
    public class PlayerDeadState : AbstractPlayerState
    {
        public PlayerDeadState(Agent agent, int stateClipHash, int layerIndex) : base(agent, stateClipHash, layerIndex)
        {
            
        }

        public override void Enter()
        {
            base.Enter();
            ControlMovement.MoveXLock(true);
        }

        public override void Exit()
        {
            ControlMovement.MoveXLock(false);
        }
    }
}