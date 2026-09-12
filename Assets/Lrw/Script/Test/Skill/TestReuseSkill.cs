using DevLib.ModuleSystem;
using Lrw.Script._Core._Debug;
using Lrw.Script.Agent.SkillSystem.ReuseSkill;

namespace Lrw.Script.Test.Skill
{
    public class TestReuseSkill : AbstractReuseSkill
    {
        public override void InitReuseSkill(ModuleOwner owner)
        {
            
        }


        public override void UseReuseSkill()
        {
            base.UseReuseSkill();
            FDebug.Log($"TestReuseSkill : {name}");
        }
    }
}