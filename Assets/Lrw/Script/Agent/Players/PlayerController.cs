using Lrw.Script.Agent.FSM;
using Lrw.Script.Agent.Players.Enum;
using Lrw.Script.Agent.Players.FSM;
using UnityEngine;

namespace Lrw.Script.Agent.Players
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : Agent, IHaveFsm<PlayerStateEnum>
    {
        [field: SerializeField] public PlayerInputSo PlayerInput { get; private set; }
        [SerializeField] private StateListSo playerStates;
        private AgentStateMachine agentStateMachine;
        private IControlDasher _dashController;
        
        protected override void Awake()
        {
            base.Awake();
            agentStateMachine = new AgentStateMachine(this, playerStates.states);
            agentStateMachine.ChangeState((int)PlayerStateEnum.IDLE);
            PlayerInput.OnDashKeyPressed += HandleDashStateChange;
            _dashController = GetModule<IControlDasher>();
        }

        protected virtual void OnDestroy()
        {
            agentStateMachine.StateExit();
            PlayerInput.OnDashKeyPressed -= HandleDashStateChange;
        }

        protected virtual void HandleDashStateChange()
        {
            if (_dashController.CanDash())
                _dashController.Dash();
        }

        protected virtual void Update()
        {
            agentStateMachine.UpdateMachine();
        }
        
        public void ChangeState(PlayerStateEnum newState)
            => agentStateMachine.ChangeState((int)newState);
    }
}