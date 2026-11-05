using DevLib.ModuleSystem;
using CTJ.UI;
using Lrw.Script.Agent.SkillSystem.NormalSkill;
using Lrw.Script.Agent.StatSystem;
using UnityEngine;

namespace CTJ.Enemies.Skills
{
    public abstract class EnemyAttackSkill : AbstractCoolTimeSkill
    {
        [Header("Attack Preparation")]
        [Tooltip("StatModule에서 읽는 준비 시간(초). 마지막 점멸을 포함하며, 점멸 후 유지 시간은 별도로 더합니다.")]
        [SerializeField] private StatData attackDelayStat;
        [SerializeField, Min(0f)] private float fallbackAttackDelay = 0.35f;
        [Tooltip("준비/점멸이 끝난 뒤 경고를 켜 둔 채 공격을 기다리는 시간(초). 점멸을 꺼도 유지됩니다.")]
        [SerializeField, Min(0f)] private float postBlinkDelay = 0.15f;
        [SerializeField] private bool blinkBeforeAttack = true;
        [Tooltip("공용 경고 색의 알파 배율. 0.6이면 기본 알파 50이 30이 됩니다.")]
        [SerializeField, Range(0f, 1f)] private float telegraphOpacity = 1f;
        [SerializeField] private AttackTelegraphUI telegraphPrefab;
        [Tooltip("선택 사항. 마지막 Post Blink Delay 동안 적 비주얼을 단색으로 표시합니다. 범위 UI와는 독립적입니다.")]
        [SerializeField] private EnemyAttackWarningModule preparationWarning;
        private enum PreparationPhase { Waiting, Blinking, Holding }
        private Stat _attackDelay;
        private PreparationPhase _preparationPhase;
        private float _phaseEndTime;
        private float _blinkDuration;
        private float _holdDuration;
        private bool _isRecovering;
        protected AttackTelegraphUI Telegraph { get; private set; }
        protected bool HasReleased { get; private set; }
        protected virtual bool ShouldShowTelegraph => true;
        public bool IsPreparing => IsUsing && !HasReleased;
        // 타격/발사 처리가 끝나면 파생 스킬이 BeginRecovery를 호출합니다.
        public bool IsRecovering => IsUsing && _isRecovering;
        public bool IsHoldingWarning => IsPreparing && _preparationPhase == PreparationPhase.Holding;
        public float AttackDelay => Mathf.Max(0f, _attackDelay?.Value ?? fallbackAttackDelay);
        public float PostBlinkDelay => Mathf.Max(0f, postBlinkDelay);
        protected EnemyBase Enemy { get; private set; }
        protected Transform CastTarget { get; private set; }
        protected bool CanContinue => isActiveAndEnabled && Enemy != null && Enemy.isActiveAndEnabled &&
            !Enemy.IsDead && CastTarget != null && CastTarget.gameObject.activeInHierarchy && Enemy.Target == CastTarget;

        public override void InitSkill(ModuleOwner owner)
        {
            base.InitSkill(owner);
            Enemy = owner as EnemyBase;
            if (attackDelayStat != null && owner.TryGetModule(out IStatModule stats))
                _attackDelay = stats.GetStat(attackDelayStat);
        }

        protected override bool CanUseSkillCondition() => isActiveAndEnabled && !IsUsing && Enemy != null &&
            Enemy.isActiveAndEnabled && !Enemy.IsDead && Enemy.Target != null && Enemy.Target.gameObject.activeInHierarchy;

        protected override void OnUseSkill()
        {
            base.OnUseSkill();
            CastTarget = Enemy.Target;
            HasReleased = false;
            _isRecovering = false;
            if (Telegraph == null && telegraphPrefab != null)
                Telegraph = Instantiate(telegraphPrefab, Enemy.transform);

            _blinkDuration = blinkBeforeAttack && Telegraph != null ? Telegraph.BlinkDuration : 0f;
            _holdDuration = PostBlinkDelay;
            _preparationPhase = PreparationPhase.Waiting;
            _phaseEndTime = Time.time + Mathf.Max(0f, AttackDelay - _blinkDuration);
            PrepareAttack();
            if (IsUsing && Telegraph != null && ShouldShowTelegraph)
            {
                Telegraph.Show(telegraphOpacity);
                UpdateTelegraph();
            }
        }

        public override void UpdateSkill()
        {
            if (!CanContinue)
            {
                StopSkill();
                return;
            }
            if (IsPreparing && Telegraph != null)
            {
                if (!ShouldShowTelegraph) Telegraph.Hide();
                else if (!Telegraph.IsVisible)
                {
                    Telegraph.Show(telegraphOpacity);
                    if (_preparationPhase == PreparationPhase.Blinking) Telegraph.BeginBlink();
                }
            }
            if (Telegraph != null && Telegraph.IsVisible) UpdateTelegraph();
            if (!IsPreparing || Time.time < _phaseEndTime) return;
            switch (_preparationPhase)
            {
                case PreparationPhase.Waiting:
                    if (_blinkDuration <= 0f)
                    {
                        BeginHold();
                        break;
                    }
                    Telegraph?.BeginBlink();
                    _preparationPhase = PreparationPhase.Blinking;
                    _phaseEndTime = Time.time + _blinkDuration;
                    break;
                case PreparationPhase.Blinking:
                    BeginHold();
                    break;
                case PreparationPhase.Holding:
                    ReleaseAttack();
                    break;
            }
        }

        private void BeginHold()
        {
            Telegraph?.EndBlink();
            if (_holdDuration > 0f && preparationWarning != null) preparationWarning.Show();
            _preparationPhase = PreparationPhase.Holding;
            _phaseEndTime = Time.time + _holdDuration;
        }

        protected virtual void PrepareAttack() { }
        protected virtual void UpdateTelegraph() { }
        protected virtual void OnReleaseAttack() { }

        protected void BeginRecovery()
        {
            if (IsUsing && HasReleased) _isRecovering = true;
        }

        private void ReleaseAttack()
        {
            if (!IsUsing || !CanContinue || HasReleased) return;
            HasReleased = true;
            if (preparationWarning != null) preparationWarning.Hide();
            OnReleaseAttack();
        }

        public virtual void OnAttackWarning() { }
        public virtual void OnAttackHit() { }
        public virtual void OnAttackFinished()
        {
            if (HasReleased) EndSkill();
        }
        protected override void OnEndSkill() => ClearAttack();
        protected override void OnStopSkill() => ClearAttack();
        private void ClearAttack()
        {
            Telegraph?.Hide();
            if (preparationWarning != null) preparationWarning.Hide();
            HasReleased = false;
            _isRecovering = false;
            CastTarget = null;
        }
        protected virtual void OnDisable() => StopSkill();
        protected virtual void OnDestroy()
        {
            if (Telegraph != null) Destroy(Telegraph.gameObject);
        }
    }
}
