using DevLib.ModuleSystem;
using Lrw.Script.Agent.SkillSystem.NormalSkill;
using UnityEngine;

namespace Lrw.Script.Test.Skill
{
    public class TestNormalSkill : AbstractNormalSkill
    {
        public override float GetMaxCooldown()
        {
            return SkillSo.BaseCooldown;
        }

        public override void InitSkill(ModuleOwner owner)
        {
            
        }

        public override void UseSkill()
        {
            base.UseSkill();
            Debug.Log("123");
        }
        
    }
}