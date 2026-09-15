using System.Collections.Generic;
using UnityEngine;

namespace Lrw.Script.Agent.StatSystem
{
    [CreateAssetMenu(fileName = "Stat Group", menuName = "Stat/Stat Group", order = 0)]
    public class StatGroup : ScriptableObject
    {
        [field:SerializeField] public StatOverride[] Stats { get;private set; }


        #if UNITY_EDITOR
        private void OnValidate()
        {
            HashSet<StatData> stats = new HashSet<StatData>();
            foreach (StatOverride stat in Stats)
            {
                if (!stats.Add(stat.StatData))
                {
                    FDebug.LogError("Stat group stat overrides error");
                }
            }
        }
        #endif
        
    }
}