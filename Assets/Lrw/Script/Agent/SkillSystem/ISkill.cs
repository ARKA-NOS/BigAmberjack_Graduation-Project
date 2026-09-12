using DevLib.ModuleSystem;

namespace Lrw.Script.Agent.SkillSystem
{
    public interface ISkill
    {
        void InitSkill(ModuleOwner owner);
        bool CanUseSkill();
        void UseSkill();
        float NormalizeCooldown();
        float GetRemainingCooldown();
        float GetMaxCooldown();
    }
}