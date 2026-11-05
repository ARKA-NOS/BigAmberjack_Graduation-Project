using _Scripts.Agent.SkillSystem;

namespace _Scripts.Agent.Players.Modules
{
    public interface IPlayerSkillModule : ISkillModule
    {
        void SetSkill(PlayerSkillSlot slot,SkillSO skill);
    }
}