using UnityEngine;

namespace Lrw.Script.StatSystem
{
    [CreateAssetMenu(fileName = "Stat Data", menuName = "Stat/Stat Data", order = 0)]
    public class StatData : ScriptableObject
    {
        [field:SerializeField] public string StatName { get; private set; }
        [field:SerializeField] public string StatDescription { get; private set; }
        [field:SerializeField] public Sprite StatIcon { get; private set; }
        [field: SerializeField] public Vector2 ValueRange { get; private set; }
        
        private void OnValidate()
        {
            if (ValueRange.x > ValueRange.y)
            {
                ValueRange = new(ValueRange.x, ValueRange.x);
            }
        }
        
    }
}