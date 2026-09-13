using DevLib.ModuleSystem;
using Lrw.Script.Agent.SkillSystem.ReuseSkill;
using UnityEngine;

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