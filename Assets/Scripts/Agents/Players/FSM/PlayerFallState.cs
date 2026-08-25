using Agents.Players.Enum;
using UnityEngine;

namespace Agents.Players.FSM
{
    public class PlayerFallState : AbstractPlayerAirState
    {
        private float _fallTime;
        private readonly IGroundChecker _groundChecker;
        private readonly IControlMultiJumper _controlMultiJumper;
        private readonly IControlDasher _controlDasher;
        
        public PlayerFallState(Agent agent, int stateClipHash, int layerIndex) : base(agent, stateClipHash, layerIndex)
        {
            _groundChecker = Player.GetModule<IGroundChecker>();
            Debug.Assert(_groundChecker != null, "GroundChecker is 없다.");            
            _controlMultiJumper = Player.GetModule<IControlMultiJumper>();
            Debug.Assert(_controlMultiJumper != null, "IControlDoubleJumper is 없다.");            
            _controlDasher = Player.GetModule<IControlDasher>();
            Debug.Assert(_controlDasher != null, "IControlDasher is 없다.");
        }

        public override void Enter(float transitionDuration = 0.1f)
        {
            base.Enter(transitionDuration);
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
            Player.ChangeState(PlayerStateEnum.IDLE, 0.1f);
            _controlMultiJumper.ResetMultiJumpCount();
            _controlDasher.ResetAirDashCount();
        }
        
        private void HandleJump()
        {
            if (_fallTime < ControlJumper.CoyoteTime && !ControlJumper.IsJumpFall || _controlMultiJumper.CanDoubleJump())
            {
                Player.ChangeState(PlayerStateEnum.JUMP, 0.1f);
            }
        }
    }
}