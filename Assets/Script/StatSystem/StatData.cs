using UnityEngine;

namespace Script.StatSystem
{
    [CreateAssetMenu(fileName = "Stat Data", menuName = "Stat/Stat Data", order = 0)]
    public class StatData : ScriptableObject
    {
        [field:SerializeField] public string StatName { get; private set; }
        [field:SerializeField] public string StatDescription { get; private set; }
        [field:SerializeField] public Sprite StatIcon { get; private set; }
        
    }
}