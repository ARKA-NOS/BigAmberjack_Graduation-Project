using UnityEngine;

namespace Lrw.Script.StatSystem
{
    [CreateAssetMenu(fileName = "Stat Group", menuName = "Stat/Stat Group", order = 0)]
    public class StatGroup : ScriptableObject
    {
        [field:SerializeField] public StatOverride[] Stats { get;private set; }
    }
}