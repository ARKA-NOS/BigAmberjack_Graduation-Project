namespace Lrw.Script.Agent.StatSystem
{
    public interface IStatModule
    {
        Stat GetStat(StatData statData, float baseValue = 0f);
    }
}