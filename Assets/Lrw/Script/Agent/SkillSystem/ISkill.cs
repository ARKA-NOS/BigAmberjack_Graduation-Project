using DevLib.ModuleSystem;

namespace Lrw.Script.Agent.SkillSystem
{
    public interface ISkill : ICooldown
    {
        void InitSkill(ModuleOwner owner);
        bool CanUseSkill();
        void UseSkill();
    }
}