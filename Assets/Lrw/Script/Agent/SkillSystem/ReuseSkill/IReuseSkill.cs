using DevLib.ModuleSystem;

namespace Lrw.Script.Agent.SkillSystem.ReuseSkill
{
    public interface IReuseSkill : ICooldown
    {
        ReuseSkillSO ReuseSkillSo { get; }
        
        void InitReuseSkill(ModuleOwner owner);
        bool CanUseReuseSkill();
        void UseReuseSkill();
    }
}