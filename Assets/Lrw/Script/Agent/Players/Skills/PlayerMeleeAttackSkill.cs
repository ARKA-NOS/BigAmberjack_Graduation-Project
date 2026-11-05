using System;
using DevLib.AnimatorSystem;
using DevLib.BattleSystem;
using DevLib.ModuleSystem;
using Lrw.Script._Core._Debug;
using Lrw.Script.Agent.Players.Enum;
using Lrw.Script.Agent.Players.Modules;
using Lrw.Script.Agent.SkillSystem.NormalSkill;
using Lrw.Script.Agent.StatSystem;
using UnityEngine;

namespace Lrw.Script.Agent.Players.Skills
{
    public class PlayerMeleeAttackSkill : AbstractCoolTimeSkill
    {
        [Header("Stat Data")]
        [SerializeField] private StatData physicalDamage;
        [SerializeField] private StatData attackSpeed;
        
        [Header("Animation Setting")]
        [SerializeField] private float defaultAnimationSpeed = 1f;
        [SerializeField] private AnimationCurve animationSpeedCurve;
        [SerializeField] private HashDataSO animHash;
        [SerializeField] private int animatorLayer = 0;
        
        [Header("DamageCast Setting")]
        [SerializeField] private AbstractDamageCaster damageCaster;
        [SerializeField] private float kbForce = 3f;
        
        [Header("AttackMove")]
        [SerializeField] private AnimationCurve attackMoveCurve;
        [SerializeField] private float moveSpeed = 1f;

        private IRenderer _renderer;
        private IAnimationTrigger _trigger;
        private Animator _animator;
        
        private IGroundChecker _groundChecker;
        private IControlMovement _movement;
        
        private Stat damageStat;
        private Stat attackSpeedStat;
        
        private PlayerController _playerController;

        private float _prevSpeed;
        
        public override float CoolTime => BaseCoolTime / attackSpeedStat.Value;

        private Vector3 _offset;
        private float _currentAnimationNormal;
        
        public override void InitSkill(ModuleOwner owner)
        {
            base.InitSkill(owner);
            
            _playerController = owner as PlayerController;
            
            FDebug.Assert(_playerController != null,
                $"[MeleeAttackSkill : {owner.name}] owner is not PlayerController");
            
            FDebug.Assert(owner.TryGetModule(out IStatModule statModule),
                $"[MeleeAttackSkill : {owner.name}] IStatModule is not found");
            
            FDebug.Assert(owner.TryGetModule(out _renderer),
                $"[MeleeAttackSkill : {owner.name}] IRenderer is not found");

            _animator = _renderer.Animator;
            
            FDebug.Assert(owner.TryGetModule(out _trigger),
                $"[MeleeAttackSkill : {owner.name}] IAnimationTrigger is not found");
            
            FDebug.Assert(owner.TryGetModule(out _movement),
                $"[MeleeAttackSkill : {owner.name}] IControlMovement is not found");
            
            FDebug.Assert(owner.TryGetModule(out _groundChecker),
                $"[MeleeAttackSkill : {owner.name}] IGroundChecker is not found");
            
            damageStat = statModule.GetStat(physicalDamage);
            attackSpeedStat = statModule.GetStat(attackSpeed);
            
            damageCaster.InitCaster(owner);

            _offset = damageCaster.transform.position - owner.transform.position;
        }

        protected override void Update()
        {
            base.Update();
            Vector3 currentPos = _offset;
            Vector3 right = _renderer.GetRight();
            if (right.x < 0) currentPos.x *= -1;
            damageCaster.transform.position = Owner.transform.position + currentPos;
        }


        protected override void OnUseSkill()
        {
            base.OnUseSkill();
            _playerController.ChangeState(PlayerStateEnum.NORMAL_SKILL);
            _trigger.OnAttackTrigger += OnTrigger;
            _trigger.OnAnimationEnd += EndSkill;
            _prevSpeed = _animator.speed;
            SetAnimationSpeed();
            PlayerSkillEvents.RaiseMeleeAttackUsed(Owner);
        }

        private void OnTrigger()
        {
            if (damageCaster.CastDamage(damageStat.Value, GetMouseDirection(), kbForce))
                PlayerSkillEvents.RaiseMeleeAttackHit(Owner);
        }

        private Vector2 GetMouseDirection()
            => (Vector2)_playerController.transform.position - _playerController.PlayerInput.GetMouseWorldPosition();

        public override void UpdateSkill()
        {
            AnimatorStateInfo info = _animator.GetCurrentAnimatorStateInfo(animatorLayer);
            _currentAnimationNormal = info.normalizedTime;
            base.UpdateSkill();
            GroundStop();
            SetAnimationSpeed();
            AttackMove();
        }

        protected override void OnEndSkill()
        {
            base.OnEndSkill();
            SkillEndSetting();
        }

        protected override void OnStopSkill()
        {
            base.OnStopSkill();
            SkillEndSetting();
        }
        
        private void SkillEndSetting()
        {
            _trigger.OnAttackTrigger -= OnTrigger;
            _trigger.OnAnimationEnd -= EndSkill;
            _movement.MoveXLock(false);
            _animator.speed = _prevSpeed;
            _playerController.ChangeState(PlayerStateEnum.IDLE);
        }

        private void GroundStop()
        {
            if(_groundChecker.FixedIsGround)
                _movement.MoveXLock(true);
        }

        private void SetAnimationSpeed()
        {
            _animator.speed = GetAnimationSpeed(_currentAnimationNormal);
        }

        private float GetAnimationSpeed(float normalizedTime)
        {
            return defaultAnimationSpeed * animationSpeedCurve.Evaluate(normalizedTime) * attackSpeedStat.Value;
        }
        
        private void AttackMove()
        {
            Vector2 dir = _renderer.GetRight();
            Owner.transform.Translate(dir * (moveSpeed * attackMoveCurve.Evaluate(_currentAnimationNormal) * Time.time));
        }
        
    }
}