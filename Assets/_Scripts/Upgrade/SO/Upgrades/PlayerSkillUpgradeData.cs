using _Scripts.Agent.Players.Modules;
using _Scripts.Agent.SkillSystem;
using _Scripts.DevLib.ModuleSystem;
using UnityEngine;

namespace _Scripts.Upgrade.SO.Upgrades
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