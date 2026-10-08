using Agents.FSM;
using Agents.Players.Enum;
using Agents.Players.FSM;
using UnityEngine;

namespace Agents.Players
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : Agent, IHaveFsm<PlayerStateEnum>
    {
        [field: SerializeField] public PlayerInputSo PlayerInput { get; private set; }
        [SerializeField] private StateListSo playerStates;
        private StateMachine _stateMachine;
        private IControlDasher _dashController;
        
        protected override void Awake()
        {
            base.Awake();
            _stateMachine = new StateMachine(this, playerStates.states);
            _stateMachine.ChangeState((int)PlayerStateEnum.IDLE);
            PlayerInput.OnDashKeyPressed += HandleDashStateChange;
            _dashController = GetModule<IControlDasher>();
        }

        private void OnDestroy()
        {
            PlayerInput.OnDashKeyPressed -= HandleDashStateChange;
        }

        private void HandleDashStateChange()
        {
            if (_dashController.CanDash())
                _dashController.Dash();
        }

        private void Update()
        {
            _stateMachine.UpdateMachine();
        }
        
        public void ChangeState(PlayerStateEnum newState, float transitionDuration = 0.1f)
            => _stateMachine.ChangeState((int)newState, transitionDuration);
    }
}