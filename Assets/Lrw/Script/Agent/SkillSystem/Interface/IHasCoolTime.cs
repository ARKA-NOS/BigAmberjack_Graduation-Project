namespace Lrw.Script.Agent.SkillSystem.Interface
{
    public interface IHasCoolTime
    {
        float CoolTimeStartTime { get; }
        float CoolTime { get; }
        float RemainingCoolTime { get; }
    }
}
