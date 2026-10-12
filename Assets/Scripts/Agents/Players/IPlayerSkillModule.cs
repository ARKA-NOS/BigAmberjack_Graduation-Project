using Lrw.Script.Agent.SkillSystem;

namespace Agents.Players
{
    public interface IPlayerSkillModule : ISkillModule
    {
        void SetSkill(SkillSlot slot,SkillSO skill);
    }
}