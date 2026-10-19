using DevLib.ModuleSystem;
using CTJ.UI;
using Lrw.Script.Agent.SkillSystem.NormalSkill;
using Lrw.Script.Agent.StatSystem;
using UnityEngine;

namespace CTJ.Enemies.Skills
{
    // 두 공격에서 실제로 공유하는 소유자/타깃 고정/취소 처리를 모읍니다.
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
        private enum PreparationPhase { Waiting, Blinking, Holding }
        private Stat _attackDelay;
        private PreparationPhase _preparationPhase;
        private float _phaseEndTime;
        private float _blinkDuration;
        private float _holdDuration;
        protected AttackTelegraphUI Telegraph { get; private set; }
        protected bool HasReleased { get; private set; }
        public bool IsPreparing => IsUsing && !HasReleased;
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
            // 이 호출이 공용 쿨타임을 시작합니다. SO에 실행 상태를 저장하지 않습니다.
            base.OnUseSkill();
            CastTarget = Enemy.Target;
            HasReleased = false;
            if (Telegraph == null && telegraphPrefab != null)
                Telegraph = Instantiate(telegraphPrefab, Enemy.transform);
            // 이번 공격의 시간은 시작 시 고정합니다. 준비 스탯이 짧아도 점멸/마지막 유지는 생략하지 않습니다.
            _blinkDuration = blinkBeforeAttack && Telegraph != null ? Telegraph.BlinkDuration : 0f;
            _holdDuration = PostBlinkDelay;
            _preparationPhase = PreparationPhase.Waiting;
            _phaseEndTime = Time.time + Mathf.Max(0f, AttackDelay - _blinkDuration);
            PrepareAttack();
            if (IsUsing && Telegraph != null)
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
            _preparationPhase = PreparationPhase.Holding;
            // 실제로 경고가 다시 켜진 프레임부터 셉니다. 저프레임에서도 유지 시간이 사라지지 않습니다.
            _phaseEndTime = Time.time + _holdDuration;
        }

        protected virtual void PrepareAttack() { }
        protected virtual void UpdateTelegraph() { }
        protected virtual void OnReleaseAttack() { }

        private void ReleaseAttack()
        {
            if (!IsUsing || !CanContinue || HasReleased) return;
            HasReleased = true;
            OnReleaseAttack();
        }

        // 구형 클립/Relay 호환용. 점멸은 공통 준비 타이머만 시작하며 늦은 이벤트로 재시작하지 않습니다.
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
            HasReleased = false;
            CastTarget = null;
        }
        protected virtual void OnDisable() => StopSkill();
        protected virtual void OnDestroy()
        {
            if (Telegraph != null) Destroy(Telegraph.gameObject);
        }
    }
}
