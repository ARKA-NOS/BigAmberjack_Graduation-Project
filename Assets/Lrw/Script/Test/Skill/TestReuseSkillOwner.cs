using Lrw.Script._Core._Debug;
using Lrw.Script.Agent.SkillSystem.ReuseSkill;

namespace Lrw.Script.Test.Skill
{
    public class TestReuseSkillOwner : AbstractReuseSkillOwner
    {
        public override float GetMaxCooldown()
        {
            return SkillSo.BaseCooldown;
        }

        protected override bool CanUseFirstSkill()
        {
            return true;
        }

        protected override void UseFirstSkill()
        {
            FDebug.Log("TestReuseSkillOwner");
        }
    }
}