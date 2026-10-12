namespace Lrw.Script.Agent.SkillSystem.Interface
{
    public interface IHasCoolTime
    {
        float SkillStartTime { get; }
        float CoolTime { get; }
    }
}