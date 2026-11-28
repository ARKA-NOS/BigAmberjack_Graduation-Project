using _Scripts.Agent.Players.Modules;
using _Scripts.Agent.SkillSystem;
using _Scripts.Agent.SkillSystem.Interface;
using _Scripts.DevLib.ModuleSystem;
using UnityEngine;

namespace Scripts.Upgrade.Upgrader
{
    public interface IPlayerUpgraderModule
    {
        void SetUpdae(PlayerSkillSlot slot, GameObject UpdaderSystem);
    }
}