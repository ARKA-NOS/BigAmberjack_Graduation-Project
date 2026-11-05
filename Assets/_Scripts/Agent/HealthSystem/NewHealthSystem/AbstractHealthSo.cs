using _Scripts.DevLib.ModuleSystem;
using UnityEngine;

namespace _Scripts.Agent.HealthSystem.NewHealthSystem
{
    //[CreateAssetMenu(fileName = "??? Health SO", menuName = "Health/??? Health SO")]
    public abstract class AbstractHealthSo : ScriptableObject
    {
        [field:SerializeField] public int Priority { get; private set; }
        [field:SerializeField] public HealthType HealthType { get; protected set; }
        [field: SerializeField] public Color HealthTextColor { get; protected set; } = Color.white;
        
        public abstract IHealth CreateHealth(ModuleOwner owner);
    }
}