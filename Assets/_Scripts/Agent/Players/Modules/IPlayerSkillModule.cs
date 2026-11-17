using _Scripts.Agent.SkillSystem;
using _Scripts.Agent.SkillSystem.Interface;

namespace _Scripts.Agent.Players.Modules
{
    public interface IPlayerSkillModule : ISkillModule
    {
        void SetSkill(PlayerSkillSlot slot,SkillSO skill);

        // 슬롯에 지금 장착된 스킬. 비어 있으면 null.
        ISkill GetSlotSkill(PlayerSkillSlot slot);
    }
}