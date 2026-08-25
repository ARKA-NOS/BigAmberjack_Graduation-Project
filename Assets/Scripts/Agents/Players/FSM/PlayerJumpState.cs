using Agents.Players.Enum;
using UnityEngine;

namespace Agents.Players.FSM
{
    public class PlayerJumpState : AbstractPlayerAirState
    {
        private readonly Rigidbody2D _playerRb;
        public PlayerJumpState(Agent agent, int stateClipHash, int layerIndex) : base(agent, stateClipHash, layerIndex)
        {
            _playerRb = agent.GetComponent<Rigidbody2D>();
            Debug.Assert(_playerRb != null, "플레이어한테 Rigidbody2D 안 붙였나요??");
        }

        public override void Enter(float transitionDuration = 0.1f)
        {
            base.Enter(transitionDuration);
            ControlJumper.Jump();
            Player.PlayerInput.OnJumpKeyReleased += HandleJumpReleased;
        }

        public override void Update()
        {
            base.Update();
            HandleFallStateChange(_playerRb.linearVelocityY);
        }

        public override void Exit()
        {
            base.Exit();
            Player.PlayerInput.OnJumpKeyReleased -= HandleJumpReleased;
        }

        private void HandleJumpReleased()
        {
            ControlJumper.CancelJump();
        }
        
        private void HandleFallStateChange(float velocityY)
        {
            if (velocityY >= 0f) return;

            Player.ChangeState(PlayerStateEnum.FALL, 0.1f);
            ControlJumper.IsJumpFall = true;
        }
    }
}