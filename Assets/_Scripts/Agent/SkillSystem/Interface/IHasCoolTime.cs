namespace _Scripts.Agent.SkillSystem.Interface
{
    public interface IHasCoolTime
    {
        float CoolTimeStartTime { get; }
        float CoolTime { get; }
        float RemainingCoolTime { get; }
    }
}
