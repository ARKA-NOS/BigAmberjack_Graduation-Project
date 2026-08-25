using Lrw.Script._Core;
using Lrw.Script.StatSystem;
using UnityEngine;

namespace Lrw.Script.Test
{
    public class StatTester : MonoBehaviour
    {
        [SerializeField] private StatData statData;
        
        [SerializeField] private StatModule statModule;

        [ContextMenu("StatLog")]
        private void StatLog()
        {
            if(statModule == null || statData == null) return;
            
            FDebug.Log(statModule.GetStat(statData).Value);
        }
    }
}