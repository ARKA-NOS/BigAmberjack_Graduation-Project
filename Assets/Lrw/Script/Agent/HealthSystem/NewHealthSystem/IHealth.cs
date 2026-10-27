namespace Lrw.Script.Agent.HealthSystem.NewHealthSystem
{
    /// <summary>
    /// 체력 하나의 읽기 전용 조회용 인터페이스
    /// </summary>
    public interface IHealth
    {
        AbstractHealthSo Data { get; }
        float Current { get; }
        float Max { get; }
        float Normalized { get; }
        bool IsEmpty { get; }
    }
}
