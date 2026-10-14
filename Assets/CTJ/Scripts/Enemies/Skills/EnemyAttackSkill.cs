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
        [Tooltip("StatModule에서 읽는 준비 시간(초). 근접은 이 시간이 지난 후 공격 애니메이션을 시작합니다.")]
        [SerializeField] private StatData attackDelayStat;
        [SerializeField, Min(0f)] private float fallbackAttackDelay = 0.35f;
        [SerializeField] private AttackTelegraphUI telegraphPrefab;
        private Stat _attackDelay;
        private float _releaseTime;
        protected AttackTelegraphUI Telegraph { get; private set; }
        protected bool HasReleased { get; private set; }
        protected virtual bool BlinkDuringPreparation => true;
        public bool IsPreparing => IsUsing && !HasReleased;
        public float AttackDelay => Mathf.Max(0f, _attackDelay?.Value ?? fallbackAttackDelay);
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
            _releaseTime = Time.time + AttackDelay; // 진행 중 스탯이 바뀌어도 이번 준비 시간은 시작 시 값으로 고정합니다.
            PrepareAttack();
            if (Telegraph == null && telegraphPrefab != null)
                Telegraph = Instantiate(telegraphPrefab, Enemy.transform);
            if (Telegraph != null)
            {
                Telegraph.Show();
                UpdateTelegraph();
            }
            if (AttackDelay <= 0f) ReleaseAttack();
        }

        public override void UpdateSkill()
        {
            if (!CanContinue)
            {
                StopSkill();
                return;
            }
            if (Telegraph != null && Telegraph.IsVisible) UpdateTelegraph();
            if (!IsPreparing) return;
            if (BlinkDuringPreparation && Telegraph != null && Time.time >= _releaseTime - Telegraph.BlinkDuration)
                Telegraph.BeginBlink();
            if (Time.time >= _releaseTime) ReleaseAttack();
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

        public virtual void OnAttackWarning()
        {
            if (IsUsing && HasReleased && CanContinue) Telegraph?.BeginBlink();
        }
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
