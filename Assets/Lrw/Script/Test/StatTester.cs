using Lrw.Script._Core;
using Lrw.Script.Agent.StatSystem;
using UnityEngine;

namespace Lrw.Script.Test
{
    public class StatTester : MonoBehaviour
    {
        [SerializeField] private StatData statData;
        
        [SerializeField] private StatModule statModule;

        [SerializeField] private float changeValue;

        [ContextMenu("StatLog")]
        private void StatLog()
        {
            if(statModule == null || statData == null) return;
            
            FDebug.Log(statModule.GetStat(statData).Value);
        }

        [ContextMenu("ChangeValue")]
        private void ChangeValue()
        {
            if(statModule == null || statData == null) return;
            
            statModule.GetStat(statData).AddModify(this,new StatModifyData(1,changeValue));
        }
        
        
    }
}