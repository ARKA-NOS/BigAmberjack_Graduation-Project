using DevLib.ModuleSystem;

namespace Lrw.Script.Agent.SkillSystem
{
    public interface ISkill
    {
        SkillSO SkillSo { get; }
        void InitSkill(ModuleOwner owner);
        bool CanUseSkill();
        void UseSkill();
        float NormalizeCooldown();
        float GetRemainingCooldown();
        float GetMaxCooldown();
    }
}