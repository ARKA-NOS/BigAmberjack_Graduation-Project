using Agents.Players.Enum;
using UnityEngine;

namespace Agents.Players.FSM
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
            if (_groundChecker.IsGroundChecking())
                return false;

            Player.ChangeState(PlayerStateEnum.FALL, 0.1f);
            return true;
        }

        public override void Enter(float transitionDuration = 0.1f)
        {
            base.Enter(transitionDuration);
            Player.PlayerInput.OnJumpKeyPressed += HandleJumpStateChange;
        }
        
        public override void Exit()
        {
            base.Exit();
            Player.PlayerInput.OnJumpKeyPressed -= HandleJumpStateChange;
        }

        private void HandleJumpStateChange()
        {
            Player.ChangeState(PlayerStateEnum.JUMP, 0.1f);
        }
    }
}