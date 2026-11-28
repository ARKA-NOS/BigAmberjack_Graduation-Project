using _Scripts.DevLib.ModuleSystem;

namespace _Scripts.Upgrade.Upgrader
{
    public interface IUpgraderSystem
    {
        UpgraderSO UpgraderSo { get; }
        bool IsActive { get; }
        void InitUpgrader(ModuleOwner owner);
        void SetActive(bool active);
    }
}
