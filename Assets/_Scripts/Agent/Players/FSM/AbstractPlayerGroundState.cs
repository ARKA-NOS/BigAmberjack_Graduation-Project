using _Scripts.Agent.Players.Enum;
using UnityEngine;

namespace _Scripts.Agent.Players.FSM
{
    public abstract class AbstractPlayerGroundState : AbstractPlayerState
    {
        private readonly IGroundChecker _groundChecker;

        protected AbstractPlayerGroundState(Agent agent, int stateClipHash, int layerIndex) : base(agent, stateClipHash, layerIndex)
        {
            _groundChecker = Player.GetModule<IGroundChecker>();
            Debug.Assert(_groundChecker != null, "GroundChecker is 없다.");
        }
        
        protected bool TryChangeToFall()
        {
            if (_groundChecker.FixedIsGround)
                return false;
            
            Player.ChangeState(PlayerStateEnum.FALL);
            return true;
        }
        
        
    }
}