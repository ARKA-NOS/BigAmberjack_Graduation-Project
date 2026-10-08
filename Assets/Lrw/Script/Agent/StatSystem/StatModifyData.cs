using System;
using UnityEngine;

namespace Lrw.Script.Agent.StatSystem
{
    [Serializable]
    public struct StatModifyData
    {
        [field:SerializeField] public int Priority { get; private set; }
        [field:SerializeField] public float Value { get; private set; }
        [field:SerializeField] public ModifyMathType ModifyType { get; private set; }
        
        public StatModifyData(int priority, float value, ModifyMathType modifyType = ModifyMathType.Add)
        {
            Priority = priority;
            Value = value;
            ModifyType = modifyType;
        }
        
    }
}