using DevLib.ModuleSystem;
using Lrw.Script.Agent.SkillSystem;
using UnityEngine;

namespace Lrw.Script.Agent.Skills
{
    [CreateAssetMenu(fileName = " True Skill Condition", menuName = "Skill System/True Skill Condition", order = 0)]
    public class TestCondition : AbstractSkillUseCondition
    {
        public override bool UseCondition(ModuleOwner owner, ISkillPlayer skillPlayer)
        {
            return true;
        }
        
    }
}