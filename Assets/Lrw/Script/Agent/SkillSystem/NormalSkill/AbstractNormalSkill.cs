using UnityEngine;

namespace Lrw.Script.Agent.SkillSystem.NormalSkill
{
    public abstract class AbstractNormalSkill : AbstractBaseSkill, INormalSkill
    {
        [field:SerializeField] public SkillSO SkillSo { get; private set; }
        
        
    }
}