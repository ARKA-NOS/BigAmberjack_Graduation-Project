using _Scripts.Agent.HealthSystem.NewHealthSystem.Healths;
using _Scripts.DevLib.ModuleSystem;
using UnityEngine;

namespace _Scripts.Agent.HealthSystem.NewHealthSystem.HealthSos
{
    [CreateAssetMenu(fileName = "Default Health SO", menuName = "Health/Default Health SO")]
    public class DefaultHealthSo : AbstractHealthSo
    {
        [SerializeField] private Color healthColor;

        public override IHealth CreateHealth(ModuleOwner owner)
            => new DefaultHealth(Priority, owner, HealthType, healthColor);

    }
}