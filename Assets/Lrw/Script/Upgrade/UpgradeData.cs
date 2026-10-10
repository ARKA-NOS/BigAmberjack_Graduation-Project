namespace Lrw.Script.Upgrade
{
    public struct UpgradeData
    {
        public string Name;
        public bool IsUpgrade;

        public UpgradeData(string name, bool isUpgrade)
        {
            Name = name;
            IsUpgrade = isUpgrade;
        }
        
    }
}