using Lrw.Script.Agent.SkillSystem.ReuseSkill;
using UnityEngine;

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
            Debug.Log("TestReuseSkillOwner");
        }
    }
}