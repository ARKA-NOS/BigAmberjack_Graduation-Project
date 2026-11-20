using _Scripts.Agent.SkillSystem.Interface;
using _Scripts.DevLib.ModuleSystem;

namespace _Scripts.Agent.SkillSystem
{
    public interface ISkillModule
    {
        ModuleOwner Owner { get; }
        ISkill GetSkill(SkillSO skill);
        public bool CanUseSkill(SkillSO skillSo);
        public void UseSkill(SkillSO skillSo, bool keyPressed);
        public void ForceStopCurrentSkill();
    }
}
