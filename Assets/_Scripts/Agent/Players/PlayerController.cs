using System.Collections;
using _Scripts._Core._Debug;
using _Scripts._Core._ServiceLocator;
using _Scripts.Agent.FSM;
using _Scripts.Agent.Players.Enum;
using _Scripts.Agent.Players.FSM;
using _Scripts.Agent.Players.Modules;
using _Scripts.DevLib.BattleSystem;
using UnityEngine;

namespace _Scripts.Agent.Players
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : Agent, IHaveFsm<PlayerStateEnum>, IPlayerController
    {
        [field: SerializeField] public PlayerInputSo PlayerInput { get; private set; }
        [SerializeField] private StateListSo playerStates;

        [SerializeField] private float InvincibilityTime = 1f;
        private AgentStateMachine agentStateMachine;
        
        private IControlMovement _movementController;
        
        private ICanInputOnOff[] _canInputOnOffs;
        
        private bool _isDead;
        
        protected override void Awake()
        {
            base.Awake();
            agentStateMachine = new AgentStateMachine(this, playerStates.states);
            agentStateMachine.ChangeState((int)PlayerStateEnum.IDLE);
            
            _movementController = GetModule<IControlMovement>();

            _canInputOnOffs = GetModules<ICanInputOnOff>();
            
            NewHealthModule.OnDead += OnOnDead;
            
            ServiceLocator.Register<IPlayerController>(this);
        }
        
        protected override void Start()
        {
            base.Start();
            PlayerInput.EnableInput();
        }
        
        private void OnDestroy()
        {
            agentStateMachine.StateExit();
            NewHealthModule.OnDead -= OnOnDead;
            ServiceLocator.Register<IPlayerController>(new NullPlayerController());
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
            if(IsInvincibility) return;
            
            ChangeState(PlayerStateEnum.HIT);
            _movementController.AddForce(damageData.DirectedKBForce);
            base.ApplyDamage(damageData, hitPoint, hitDirection, hitNormal);
            StartCoroutine(HitInvincibilityTime());
        }
        
        private IEnumerator HitInvincibilityTime()
        {
            SetInvincibility(this, true);
            yield return new WaitForSeconds(InvincibilityTime);
            SetInvincibility(this, false);
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
            FDebug.Log("Player Dead");
        }
        
    }
}