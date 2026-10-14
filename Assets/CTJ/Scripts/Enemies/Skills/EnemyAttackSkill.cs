using DevLib.ModuleSystem;
using Lrw.Script.Agent.SkillSystem.NormalSkill;
using UnityEngine;

namespace CTJ.Enemies.Skills
{
    // 두 공격에서 실제로 공유하는 소유자/타깃 고정/취소 처리를 모읍니다.
    public abstract class EnemyAttackSkill : AbstractCoolTimeSkill
    {
        protected EnemyBase Enemy { get; private set; }
        protected Transform CastTarget { get; private set; }
        protected bool CanContinue => isActiveAndEnabled && Enemy != null && Enemy.isActiveAndEnabled &&
            !Enemy.IsDead && CastTarget != null && CastTarget.gameObject.activeInHierarchy && Enemy.Target == CastTarget;

        public override void InitSkill(ModuleOwner owner)
        {
            base.InitSkill(owner);
            Enemy = owner as EnemyBase;
        }

        protected override bool CanUseSkillCondition() => isActiveAndEnabled && !IsUsing && Enemy != null &&
            Enemy.isActiveAndEnabled && !Enemy.IsDead && Enemy.Target != null && Enemy.Target.gameObject.activeInHierarchy;

        protected override void OnUseSkill()
        {
            // 이 호출이 공용 쿨타임을 시작합니다. SO에 실행 상태를 저장하지 않습니다.
            base.OnUseSkill();
            CastTarget = Enemy.Target;
        }

        public override void UpdateSkill()
        {
            if (!CanContinue) StopSkill();
        }

        public virtual void OnAttackHit() { }
        public virtual void OnAttackFinished() => EndSkill();
        protected override void OnEndSkill() => CastTarget = null;
        protected override void OnStopSkill() => CastTarget = null;
        protected virtual void OnDisable() => StopSkill();
    }
}
