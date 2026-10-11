namespace Lrw.Script.Upgrade
{
    public interface IUpgradeManager
    {
        UpgradeTree GetUpgradeTree(string treeName);
    }
}