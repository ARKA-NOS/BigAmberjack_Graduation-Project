using DevLib.BattleSystem;
using Lrw.Script._Core._Debug;
using Lrw.Script._Core._ServiceLocator;
using Lrw.Script.Agent.FSM;
using Lrw.Script.Agent.Players.Enum;
using Lrw.Script.Agent.Players.FSM;
using Lrw.Script.Agent.Players.Modules;
using UnityEngine;

namespace Lrw.Script.Agent.Players
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : Agent, IHaveFsm<PlayerStateEnum>, IPlayerController
    {
        [field: SerializeField] public PlayerInputSo PlayerInput { get; private set; }
        [SerializeField] private StateListSo playerStates;
        private AgentStateMachine agentStateMachine;
        private IControlDasher _dashController;
        private IControlMovement _movementController;
        
        private ICanInputOnOff[] _canInputOnOffs;
        
        private bool _isDead;
        
        protected override void Awake()
        {
            base.Awake();
            agentStateMachine = new AgentStateMachine(this, playerStates.states);
            agentStateMachine.ChangeState((int)PlayerStateEnum.IDLE);
            
            PlayerInput.OnDashKeyPressed += HandleDashStateChange;
            
            _dashController = GetModule<IControlDasher>();
            
            _movementController = GetModule<IControlMovement>();

            _canInputOnOffs = GetModules<ICanInputOnOff>();
            
            HealthModule.OnDead += OnOnDead;
            
            ServiceLocator.Register<IPlayerController>(this);
        }
        
        private void OnDestroy()
        {
            agentStateMachine.StateExit();
            PlayerInput.OnDashKeyPressed -= HandleDashStateChange;
            HealthModule.OnDead -= OnOnDead;
            ServiceLocator.Register<IPlayerController>(new NullPlayerController());
        }

        private void HandleDashStateChange()
        {
            if (_dashController.CanDash())
                _dashController.Dash();
        }

        private void Update()
        {
            agentStateMachine.UpdateMachine();
        }
        
        public void ChangeState(PlayerStateEnum newState)
            => agentStateMachine.ChangeState((int)newState);
        
        public override void ApplyDamage(DamageData damageData, Vector2 hitPoint, Vector2 hitDirection, Vector2 hitNormal)
        {
            if(_isDead) return;
            base.ApplyDamage(damageData, hitPoint, hitDirection, hitNormal);
            if (!_isDead)
            {
                ChangeState(PlayerStateEnum.HIT);
                _movementController.AddForce(damageData.DirectedKBForce);
            }
        }

        public void SetInputActive(bool active)
        {
            foreach (ICanInputOnOff canInputOnOff in _canInputOnOffs)
            {
                canInputOnOff.SetActive(active);
            }
        }
        
        private void OnOnDead()
        {
            if(_isDead) return;
            ChangeState(PlayerStateEnum.Dead);
            _isDead = true;
            SetInputActive(false);
            FDebug.Log("Dead");
        }
        
    }
}