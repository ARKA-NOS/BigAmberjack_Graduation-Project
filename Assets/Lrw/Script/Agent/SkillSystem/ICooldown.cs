namespace Lrw.Script.Agent.SkillSystem
{
    public interface ICooldown
    {
        float NormalizeCooldown();
        float GetRemainingCooldown();
        float GetMaxCooldown();
    }
}