using _Scripts._Core._Debug;
using _Scripts.Agent.FSM;
using _Scripts.Agent.HealthSystem.NewHealthSystem;
using _Scripts.Agent.Players.Enum;
using _Scripts.Agent.Players.Modules;
using _Scripts.Agent.SkillSystem.NormalSkill;
using _Scripts.Agent.StatSystem;
using _Scripts.DevLib.AnimatorSystem;
using _Scripts.DevLib.BattleSystem;
using _Scripts.DevLib.ModuleSystem;
using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;

namespace _Scripts.Agent.Players.Skills
{
    public class PlayerMeleeAttackSkill : AbstractCoolTimeSkill
    {
        [Header("Stat Data")]
        [SerializeField] private StatData physicalDamage;
        [SerializeField] private StatData attackSpeed;
        
        [Header("Animation Setting")]
        [SerializeField] private float defaultAnimationSpeed = 1f;
        [SerializeField] private AnimationCurve animationSpeedCurve;
        [SerializeField] private float maxAnimationSpeedScale = 2f;
        [SerializeField] private StateSo[] comboStates;
        [SerializeField] private int animatorLayer = 0;
        
        [Header("DamageCast Setting")]
        [SerializeField] private AbstractDamageCaster damageCaster;
        [SerializeField] private float kbForce = 3f;
        
        [Header("AttackMove")]
        [SerializeField] private AnimationCurve attackMoveCurve;
        [SerializeField] private float lungeDistance = 1f;

        [Header("Combotime")]
        [SerializeField] private float combotime = 0.4f; // 콤보가 이어지는 시간
        [SerializeField] private float moveCombotime = 0.6f; // 무빙콤보가 이어지는 시간
        [SerializeField] private float moveFreezeTime = 0.4f;

        private const int CurveAreaSampleCount = 100;
        private const int CurveStepSampleCount = 10;
        private float _attackMoveCurveArea;
        private float _prevMoveNormal;

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
        private bool _isAttackState;

        private float _moveAttackLastTime;

        private bool moveComboWindowExhaust => Time.time >= (_moveAttackLastTime + moveCombotime);
        private bool _isLunge;
        private CancellationTokenSource _moveLockCts;
        private INewHealthModule _healthModule;
        private bool _isEndFreeze;
        private bool _isRecovery;
        private int _freezeStateHash;

        public int ComboCount { get; private set; } = 0;


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

            FDebug.Assert(owner.TryGetModule(out _healthModule),
                $"[MeleeAttackSkill : {owner.name}] INewHealthModule is not found");

            _healthModule.OnHealthChanged += HandleHealthChanged;
            _healthModule.OnDead += ReleaseEndFreeze;

            damageStat = statModule.GetStat(physicalDamage);
            attackSpeedStat = statModule.GetStat(attackSpeed);
            
            damageCaster.InitCaster(owner);

            _offset = damageCaster.transform.position - owner.transform.position;
        }

        private static float IntegrateCurve(AnimationCurve curve, float from, float to, int sampleCount)
        {
            float step = (to - from) / sampleCount;
            float area = 0f;
            for (int i = 0; i < sampleCount; i++)
            {
                float t = from + (i + 0.5f) * step;
                area += curve.Evaluate(t) * step;
            }
            return area;
        }

        protected override void Update()
        {
            base.Update();
            Vector3 currentPos = _offset;
            Vector3 right = _renderer.GetRight();
            if (right.x < 0) currentPos.x *= -1;
            damageCaster.transform.position = Owner.transform.position + currentPos;

            if (_isEndFreeze || _isRecovery)
                UpdateEndHold();
        }

        private void UpdateEndHold()
        {
            AnimatorStateInfo info = _animator.GetCurrentAnimatorStateInfo(animatorLayer);
            if (info.shortNameHash != _freezeStateHash)
            {
                ReleaseEndFreeze();
                _movement.MoveXLock(false);
                return;
            }

            if (!_isRecovery || info.length <= 0f) return;

            float nextNormalizedTime = info.normalizedTime + Time.deltaTime * _animator.speed / info.length;
            if (nextNormalizedTime < 1f) return;

            ReleaseEndFreeze();
            SkillEndSetting();
        }

        private void OnDestroy()
        {
            if (_healthModule == null) return;
            _healthModule.OnHealthChanged -= HandleHealthChanged;
            _healthModule.OnDead -= ReleaseEndFreeze;
        }

        private void HandleHealthChanged(float newValue, float delta, float max)
        {
            if (delta < 0f) ReleaseEndFreeze();
        }


        protected override void OnUseSkill()
        {
            float readyTime = CoolTimeStartTime + CoolTime;
            base.OnUseSkill();
            ReleaseEndFreeze();
            bool comboCounterOver = ComboCount >= comboStates.Length;
            bool comboWindowExhaust = Time.time >= readyTime + combotime;
            if (comboCounterOver || comboWindowExhaust)
            {
                ComboCount = 0;
            }

            if (!Mathf.Approximately(_playerController.PlayerInput.MovementKey.x, 0) && moveComboWindowExhaust)
            {
                ComboCount = 1;
            }
            _playerController.ChangeState((PlayerStateEnum)comboStates[ComboCount].assetIndex);
            _trigger.OnAttackTrigger += OnTrigger;
            _trigger.OnAnimationEnd += EndSkill;
            _prevSpeed = _animator.speed;
            float inputX = _playerController.PlayerInput.MovementKey.x;
            if (Mathf.Abs(inputX) > 0.1f)
                _renderer.SetFlip(inputX);
            _isLunge = Mathf.Abs(inputX) > 0.1f && _groundChecker.FixedIsGround;
            if (_isLunge)
            {
                _movement.MoveXLock(true);
                _attackMoveCurveArea = IntegrateCurve(attackMoveCurve, 0f, 1f, CurveAreaSampleCount);
                _prevMoveNormal = 0f;
            }
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
            AnimatorStateInfo info = _animator.IsInTransition(animatorLayer)
                ? _animator.GetNextAnimatorStateInfo(animatorLayer)
                : _animator.GetCurrentAnimatorStateInfo(animatorLayer);
            _currentAnimationNormal = info.normalizedTime;
            _isAttackState = info.shortNameHash == comboStates[ComboCount].stateParam.HashValue;
            GroundStop();
            AttackMove();
            base.UpdateSkill();
            SetAnimationSpeed();
        }

        protected override void OnEndSkill()
        {
            base.OnEndSkill();
            _freezeStateHash = comboStates[ComboCount].stateParam.HashValue;
            ComboCount++;
            UnsubscribeTrigger();
            if (_isLunge)
            {
                _moveAttackLastTime = Time.time;
                MoveLockTime().Forget();
            }
            else
            {
                _isRecovery = true;
            }
        }

        protected override void OnStopSkill()
        {
            base.OnStopSkill();
            UnsubscribeTrigger();
            SkillEndSetting();
        }

        private void UnsubscribeTrigger()
        {
            _trigger.OnAttackTrigger -= OnTrigger;
            _trigger.OnAnimationEnd -= EndSkill;
        }

        private void SkillEndSetting()
        {
            _movement.MoveXLock(false);
            _animator.speed = _prevSpeed;
            _playerController.ChangeState(PlayerStateEnum.IDLE);
        }

        private void ReleaseEndFreeze()
        {
            CancelMoveLockTime();
            if (!_isEndFreeze && !_isRecovery) return;
            _isEndFreeze = false;
            _isRecovery = false;
            _animator.speed = _prevSpeed;
        }

        private void GroundStop()
        {
            if(_groundChecker.FixedIsGround)
                _movement.MoveXLock(true);
        }

        private async UniTask MoveLockTime()
        {
            CancelMoveLockTime();
            _moveLockCts = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
            CancellationToken token = _moveLockCts.Token;

            _isEndFreeze = true;
            _animator.speed = 0f;

            float remainTime = _moveAttackLastTime + moveFreezeTime - Time.time;
            bool isCanceled = await UniTask.Delay(System.TimeSpan.FromSeconds(remainTime), cancellationToken: token)
                .SuppressCancellationThrow();
            if (isCanceled) return;

            ReleaseEndFreeze();
            SkillEndSetting();
        }

        private void CancelMoveLockTime()
        {
            if (_moveLockCts == null) return;
            _moveLockCts.Cancel();
            _moveLockCts.Dispose();
            _moveLockCts = null;
        }

        private void SetAnimationSpeed()
        {
            _animator.speed = GetAnimationSpeed(_currentAnimationNormal);
        }

        private float GetAnimationSpeed(float normalizedTime)
        {
            float attackSpeedScale = Mathf.Min(attackSpeedStat.Value, maxAnimationSpeedScale);
            return defaultAnimationSpeed * animationSpeedCurve.Evaluate(normalizedTime) * attackSpeedScale;
        }
        
        private void AttackMove()
        {
            if (!_isLunge || _attackMoveCurveArea <= 0f || !_isAttackState)
                return;

            float current = Mathf.Clamp01(_currentAnimationNormal);
            float prev = _prevMoveNormal;
            _prevMoveNormal = Mathf.Max(prev, current);

            if (current <= prev || Time.deltaTime <= 0f)
            {
                _movement.SetVelocityX(0f);
                return;
            }

            float stepArea = IntegrateCurve(attackMoveCurve, prev, current, CurveStepSampleCount);
            float distance = lungeDistance * stepArea / _attackMoveCurveArea;
            _movement.SetVelocityX(_renderer.GetRight().x * distance / Time.deltaTime);
        }
        
    }
}