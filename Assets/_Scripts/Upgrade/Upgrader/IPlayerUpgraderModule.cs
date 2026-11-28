namespace _Scripts.Upgrade.Upgrader
{
    public interface IPlayerUpgraderModule
    {
        IUpgraderSystem GetUpgrader(UpgraderSO upgraderSo);
        void SetUpgraderActive(UpgraderSO upgraderSo, bool active);
    }
}
