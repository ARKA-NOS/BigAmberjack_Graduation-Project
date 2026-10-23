using DevLib.ModuleSystem;
using UnityEngine;

namespace Lrw.Script.Agent.HealthSystem.NewHealthSystem
{
    //[CreateAssetMenu(fileName = "??? Health SO", menuName = "Health/??? Health SO", order = 0)]
    public abstract class AbstractHealthSo : ScriptableObject
    {
        [field:SerializeField] public int Priority { get; private set; }
        [field:SerializeField] public HealthType HealthType { get; protected set; }
        [field: SerializeField] public Color HealthColor { get; protected set; } = Color.red;
        
        public abstract IHealth CreateHealth(ModuleOwner owner);
    }
}