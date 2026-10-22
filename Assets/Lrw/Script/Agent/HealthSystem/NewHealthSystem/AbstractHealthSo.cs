using DevLib.ModuleSystem;
using UnityEngine;

namespace Lrw.Script.Agent.HealthSystem.NewHealthSystem
{
    //[CreateAssetMenu(fileName = "??? Health SO", menuName = "Health/??? Health SO")]
    public abstract class AbstractHealthSo : ScriptableObject
    {
        [field: SerializeField] public Color HealthColor { get; private set; } = Color.white;
        [field: Header("클수록 데미지를 먼저 받는다")]
        [field: SerializeField] public int Priority { get; private set; }
        [field: SerializeField] public HealthType Type { get; private set; }
        
        public abstract AbstractHealth CreateHealth(ModuleOwner owner);
    }
}
