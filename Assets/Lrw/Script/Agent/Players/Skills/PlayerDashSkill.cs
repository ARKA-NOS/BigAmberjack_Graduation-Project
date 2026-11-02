using System.Collections;
using DevLib.BattleSystem;
using DevLib.ModuleSystem;
using DG.Tweening;
using Lrw.Script._Core._Debug;
using Lrw.Script.Agent.Players.Enum;
using Lrw.Script.Agent.Players.FSM;
using Lrw.Script.Agent.Players.Modules;
using Lrw.Script.Agent.SkillSystem.NormalSkill;
using Lrw.Script.Agent.StatSystem;
using Lrw.Script.VFXSystem.Effect;
using UnityEngine;

namespace Lrw.Script.Agent.Players.Skills
{
    public class PlayerDashSkill : AbstractCoolTimeSkill
    {
        [SerializeField] private float dashTime = 0.15f;
        
        [SerializeField] private StatData dashCountStat;
        [SerializeField] private StatData dashCoolTimeStat;
        [SerializeField] private StatData dashRangeStat;
        
        private Stat _dashCountStat;
        private Stat _dashCoolTime;
        private Stat _dashRange;
        public override float CoolTime => DashCoolTime;
        private float MaxAirDashCount => _dashCountStat.Value;
        private float DashCoolTime => _dashCoolTime.Value;
        private float DashRange => _dashRange.Value;
        
        private IHaveFsm<PlayerStateEnum> _haveFsm;
        
        private Rigidbody2D _playerRb;
        private IRenderer _renderer;
        private IGroundChecker _groundChecker;
        private IAfterImageEmitter _afterImageEmitter;
        private IAnimationTrigger _animationTrigger;
        
        private Tween _dashTween;
        private float _lastDashTime = float.NegativeInfinity;
        private int _currentAirDashCount;

        private bool _canDash = true;
        private bool _isGround = false;

        private IDamageable _damageable;
        
        public override void InitSkill(ModuleOwner owner)
        {
            base.InitSkill(owner);
            
            _damageable = owner as IDamageable;
            FDebug.Assert(_damageable != null, "owner is Not IDamageable");
            
            _haveFsm = owner as IHaveFsm<PlayerStateEnum>;
            FDebug.Assert(_haveFsm != null,"owner is Not IHaveFsm<PlayerStateEnum>");

            _groundChecker = Owner.GetModule<IGroundChecker>();
            _playerRb = Owner.GetComponent<Rigidbody2D>();
            _afterImageEmitter = Owner.GetModule<IAfterImageEmitter>();
            _renderer = Owner.GetModule<IRenderer>();
            _animationTrigger = Owner.GetModule<IAnimationTrigger>();
            
            FDebug.Assert(Owner.TryGetModule(out IStatModule statModule),
                "[PlayerDashSkill] IStatModule is not found");
            
            _dashCountStat = statModule.GetStat(dashCountStat);
            _dashCoolTime = statModule.GetStat(dashCoolTimeStat);
            _dashRange = statModule.GetStat(dashRangeStat);
            
            FDebug.Assert(_playerRb != null, "Player에는 Rigidbody2D가 필요합니다.");
            FDebug.Assert(_afterImageEmitter != null, "Player에는 IAfterImageEmitter 필요합니다.");
            FDebug.Assert(_groundChecker != null, "Player에는 IGroundChecker도 필요합니다.");
            FDebug.Assert(_renderer != null, "Player에는 IRenderer도 필요합니다.");
        }

        private void FixedUpdate()
        {
            _isGround = _groundChecker.FixedIsGround;

            if (_isGround)
            {
                ResetAirDashCount();
            }
        }

        protected override bool CanUseSkillCondition()
        {
            return CanDash();
        }

        protected override void OnUseSkill()
        {
            _damageable.SetInvincibility(this, true);
            Dash();
        }

        private bool CanDash()
        {
            if (!_canDash)
                return false;
            
            return _currentAirDashCount < MaxAirDashCount;
        }

        private void Dash()
        {
            Vector2 direction = _renderer.GetRight();
            direction.y = 0;
            
            if (direction.sqrMagnitude <= 0f)
                return;
            
            _haveFsm.ChangeState(PlayerStateEnum.DASH);
            
            direction.Normalize();
            
            _afterImageEmitter.Play(dashTime);

            Vector2 targetPosition =
                _playerRb.position + direction * DashRange;
            
            _lastDashTime = Time.time;

            _dashTween?.Kill();

            _playerRb.linearVelocity = Vector2.zero;

            _dashTween = _playerRb
                .DOMove(targetPosition, dashTime)
                .SetEase(Ease.Linear)
                .SetUpdate(UpdateType.Fixed)
                .OnComplete(EndSkill);
            
            if (!_groundChecker.FixedIsGround) _currentAirDashCount++;
        }

        protected override void OnEndSkill()
        {
            base.OnEndSkill();
            HandleDashEnd();
        }

        protected override void OnStopSkill()
        {
            base.OnStopSkill();
            HandleDashEnd();
        }
        
        private void HandleDashEnd()
        {
            _damageable.SetInvincibility(this, false);
            CoolTimeStartTime = Time.time;
            _haveFsm.ChangeState(PlayerStateEnum.IDLE);
        }

        private void ResetAirDashCount()
        {
            _currentAirDashCount = 0;
        }

        private void OnDestroy()
        {
            _dashTween?.Kill();
        }
        
    }
}