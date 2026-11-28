using _Scripts.Agent.Players.Modules;
using _Scripts.Agent.SkillSystem;
using UnityEngine;

namespace Scripts.Upgrade.Upgrader
{
    public class PlayerUpgraderModule : MonoBehaviour, IPlayerUpgraderModule
    {
        public void SetUpdae(PlayerSkillSlot slot, GameObject UpdaderSystem)
        {
            Instantiate(UpdaderSystem, new Vector3(0, 0, 0), Quaternion.identity, transform);
        }


    }
}
