using DevLib.ModuleSystem;
using Lrw.Script.Agent.SkillSystem.Interface;
using UnityEngine;

namespace Lrw.Script.Agent.SkillSystem.NormalSkill
{
    public class AbstractChargeSkill : AbstractSkill, IChargeSkill
    {
        public override bool CanUseSkill()
        {
            return true;
        }
        

        public bool IsCharging { get; protected set; }
        public void ChargeStart()
        {
            
        }

        public void ChargeEnd()
        {
            
        }

        public void ChargeCancel()
        {
            
        }
    }
}