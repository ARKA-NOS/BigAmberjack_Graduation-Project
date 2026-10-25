using DevLib.ModuleSystem;
using Lrw.Script.Agent.HealthSystem.NewHealthSystem.Healths;
using UnityEngine;

namespace Lrw.Script.Agent.HealthSystem.NewHealthSystem.HealthSos
{
    [CreateAssetMenu(fileName = "Default Health SO", menuName = "Health/Default Health SO")]
    public class DefaultHealthSo : AbstractHealthSo
    {
        [SerializeField] private Color healthColor;

        public override IHealth CreateHealth(ModuleOwner owner)
            => new DefaultHealth(Priority, owner, healthColor);

    }
}