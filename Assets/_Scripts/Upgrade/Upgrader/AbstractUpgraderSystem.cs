using _Scripts.DevLib.ModuleSystem;
using UnityEngine;

namespace _Scripts.Upgrade.Upgrader
{
    public abstract class AbstractUpgraderSystem : MonoBehaviour, IUpgraderSystem
    {
        [field: SerializeField] public UpgraderSO UpgraderSo { get; private set; }

        public bool IsActive { get; private set; }

        protected ModuleOwner Owner { get; private set; }

        public virtual void InitUpgrader(ModuleOwner owner)
        {
            Owner = owner;
        }

        public void SetActive(bool active)
        {
            if (IsActive == active) return;
            IsActive = active;

            if (active)
                OnActivate();
            else
                OnDeactivate();
        }

        protected abstract void OnActivate();
        protected abstract void OnDeactivate();

        protected virtual void OnDestroy() => SetActive(false);
    }
}
