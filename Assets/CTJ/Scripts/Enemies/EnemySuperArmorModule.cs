using CTJ.Enemies.Skills;
using DevLib.ModuleSystem;
using Lrw.Script.Agent.SkillSystem.Interface;
using UnityEngine;

namespace CTJ.Enemies
{
    // 선택형 슈퍼아머. 피해량/시각 효과는 유지하고 새 피격의 행동 방해만 막습니다.
    [DisallowMultipleComponent]
    public sealed class EnemySuperArmorModule : Module
    {
        private EnemyBase _enemy;
        private EnemySkillModule _skills;

        public override void Initialize(ModuleOwner owner)
        {
            base.Initialize(owner);
            _enemy = owner as EnemyBase;
            _skills = owner.GetModule<EnemySkillModule>();
        }

        public bool IsProtected
        {
            get
            {
                if (!isActiveAndEnabled || _enemy == null || !_enemy.isActiveAndEnabled || _enemy.IsDead ||
                    _skills == null || !_skills.isActiveAndEnabled) return false;
                var skill = _skills.ActiveSkill;
                if (skill == null || !skill.isActiveAndEnabled) return false;
                // 준비/공격 구간은 보호하고, 후딜/쿨타임 대기에는 보호하지 않습니다.
                return skill is EnemyAttackSkill { IsUsing: true, IsRecovering: false } ||
                       skill is IChargeSkill { IsCharging: true };
            }
        }
    }
}
