namespace Lrw.Script.Agent.StatSystem
{
    public interface IStatModule
    {
        Stat GetStat(StatData statData);
        // 없어도 되는 스탯을 조회할 때 쓴다. 없으면 에러 로그 없이 false.
        bool TryGetStat(StatData statData, out Stat stat);
    }
}