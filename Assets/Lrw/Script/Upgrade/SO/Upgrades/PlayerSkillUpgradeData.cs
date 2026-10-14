using DevLib.ModuleSystem;
using Lrw.Script.Agent.Players;
using Lrw.Script.Agent.Players.Modules;
using Lrw.Script.Agent.SkillSystem;
using UnityEngine;

namespace Lrw.Script.Upgrade.SO.Upgrades
{
    [CreateAssetMenu(fileName = "Skill Upgrade Data", menuName = "Upgrade/Upgrade Data/Skill Upgrade Data", order = 0)]
    public class PlayerSkillUpgradeData : AbstractUpgradeData
    {
        [SerializeField] private PlayerSkillSlot playerSkillSlot;
        [SerializeField] private SkillSO skill;
        
        public override void AgentUpgrade(ModuleOwner owner)
        {
            if(!owner.TryGetModule(out IPlayerSkillModule skillModule)) return;
            
            skillModule.SetSkill(playerSkillSlot,skill);
        }
        
    }
}