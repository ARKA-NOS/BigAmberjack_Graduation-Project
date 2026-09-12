using DevLib.ModuleSystem;
using Lrw.Script.Agent.SkillSystem.ReuseSkill;
using UnityEngine;

namespace Lrw.Script.Test.Skill
{
    public class TestReuseSkill : AbstractReuseSkill
    {
        public override void InitSkill(ModuleOwner owner)
        {
            
        }
        
        public override bool CanUseSkill()
        {
            return true;
        }

        public override void UseSkill()
        {
            Debug.Log($"TestReuseSkill : {name}");
        }
    }
}