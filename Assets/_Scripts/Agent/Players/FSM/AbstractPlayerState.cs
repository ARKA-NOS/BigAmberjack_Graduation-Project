using _Scripts.Agent.FSM;
using _Scripts.Agent.Players.Enum;
using _Scripts.Agent.Players.Modules;
using UnityEngine;

namespace _Scripts.Agent.Players.FSM
{
    public abstract class AbstractPlayerState : AgentState
    {
        protected readonly PlayerController Player;
        protected readonly IControlMovement ControlMovement;
        protected const float InputDeadZone = 0.1f;

        protected readonly IGroundChecker GroundChecker;
        protected readonly IControlMultiJumper ControlMultiJumper;
        protected readonly IControlJumper ControlJumper;
        
        protected AbstractPlayerState(Agent agent, int stateClipHash, int layerIndex) : base(agent, stateClipHash,
            layerIndex)
        {
            Player = agent as PlayerController;
            Debug.Assert(Player != null, "플레이어 상태는 플레이어한테 붙이세요.");
            ControlMovement = agent.GetModule<IControlMovement>();
            Debug.Assert(ControlMovement != null, "플레이어한테 ControlMovement 붙이세요.");
            GroundChecker = Player.GetModule<IGroundChecker>();
            Debug.Assert(GroundChecker != null, "GroundChecker is 없다.");            
            ControlMultiJumper = Player.GetModule<IControlMultiJumper>();
            Debug.Assert(ControlMultiJumper != null, "IControlDoubleJumper is 없다.");
            ControlJumper = Player.GetModule<IControlJumper>();
            Debug.Assert(ControlJumper != null, "플레이어 점퍼가 없어요!!!");
        }
        
        public override void Enter()
        {
            base.Enter();
            Player.PlayerInput.OnJumpKeyPressed += HandleJump;
        }
        
        public override void Exit()
        {
            base.Exit();
            Player.PlayerInput.OnJumpKeyPressed -= HandleJump;
            ControlJumper.IsJumpFall = false;
        }

        private bool PlatformKeyPressed => Player.PlayerInput.PlatformKey;
        
        protected virtual void HandleJump()
        {
            bool canGroundJump = (GroundChecker.FixedIsGround || ControlJumper.CanCoyoteJump()) && !PlatformKeyPressed;
            if (canGroundJump || ControlMultiJumper.CanDoubleJump())
            {
                Player.ChangeState(PlayerStateEnum.JUMP);
            }
        }
        
    }
}