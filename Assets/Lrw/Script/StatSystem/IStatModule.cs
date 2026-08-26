namespace Lrw.Script.StatSystem
{
    public interface IStatModule
    {
        Stat GetStat(StatData statData, float baseValue = 0f);
    }
}