using UnityEngine;

namespace Lrw.Script.Agent.SkillSystem.ReuseSkill
{
    public abstract class AbstractReuseSkill : AbstractBaseSkill,IReuseSkill
    {
        [field:SerializeField] public ReuseSkillSO ReuseSkillSo { get; private set; }
    }
}