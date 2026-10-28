using Lrw.Script.Agent.Players.Enum;
using Lrw.Script.Agent.Players.Modules;
using UnityEngine;

namespace Lrw.Script.Agent.Players.FSM
{
    public class PlayerFallState : AbstractPlayerAirState
    {
        private float _fallTime;
        private readonly IGroundChecker _groundChecker;
        private readonly IControlMultiJumper _controlMultiJumper;
        
        public PlayerFallState(Agent agent, int stateClipHash, int layerIndex) : base(agent, stateClipHash, layerIndex)
        {
            _groundChecker = Player.GetModule<IGroundChecker>();
            Debug.Assert(_groundChecker != null, "GroundChecker is 없다.");            
            _controlMultiJumper = Player.GetModule<IControlMultiJumper>();
            Debug.Assert(_controlMultiJumper != null, "IControlDoubleJumper is 없다.");            
        }

        public override void Enter()
        {
            base.Enter();
            Player.PlayerInput.OnJumpKeyPressed += HandleJump;
            _fallTime = 0;
        }

        public override void Update()
        {
            base.Update();
            HandleIdleStateChange();
            
            _fallTime += Time.deltaTime;
        }

        public override void Exit()
        {
            base.Exit();
            Player.PlayerInput.OnJumpKeyPressed -= HandleJump;
            ControlJumper.IsJumpFall = false;
        }

        private void HandleIdleStateChange()
        {
            if (!_groundChecker.IsGroundChecking()) return;
            Player.ChangeState(PlayerStateEnum.IDLE);
            _controlMultiJumper.ResetMultiJumpCount();
        }
        
        private void HandleJump()
        {
            if (_fallTime < ControlJumper.CoyoteTime && !ControlJumper.IsJumpFall || _controlMultiJumper.CanDoubleJump())
            {
                Player.ChangeState(PlayerStateEnum.JUMP);
            }
        }
    }
}