using DevLib.ModuleSystem;
using Lrw.Script._Core._Debug;
using Lrw.Script.Agent.SkillSystem.NormalSkill;

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
            FDebug.Log("123");
        }
        
    }
}