using UnityEngine;

namespace Lrw.Script.MapCreateSystem
{
    [CreateAssetMenu(fileName = "MapModule", menuName = "Map/MapModule", order = 0)]
    public class MapModuleSO : ScriptableObject
    {
        [field:SerializeField] public GameObject Module { get; private set; }
        
        public Vector2 ModuleSize => Module?.transform.lossyScale ?? Vector2.one;
        
    }
}