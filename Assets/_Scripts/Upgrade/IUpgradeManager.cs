namespace _Scripts.Upgrade
{
    public interface IUpgradeManager
    {
        UpgradeTree GetUpgradeTree(string treeName);
    }
}