using DevLib.ModuleSystem;
using Lrw.Script.Agent.HealthSystem.NewHealthSystem.Healths.HealthClass;
using UnityEngine;

namespace Lrw.Script.Agent.HealthSystem.NewHealthSystem.Healths.HealthSO
{
    [CreateAssetMenu(fileName = "Default Health SO", menuName = "Health/Default Health SO")]
    public class DefaultHealthSo : AbstractHealthSo
    {
        [SerializeField] private float baseValue = 0f;
        
        public override AbstractHealth CreateHealth(ModuleOwner owner)
            => new DefaultHealth(this, owner, baseValue);
    }
}