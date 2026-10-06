using System;

namespace Lrw.Script.Agent.StatSystem
{
    [Serializable]
    public readonly struct StatModifyData
    {
        public readonly int Priority;
        public readonly float Value;
        public readonly ModifyMathType ModifyType;

        public StatModifyData(int priority, float value, ModifyMathType modifyType = ModifyMathType.Add)
        {
            Priority = priority;
            Value = value;
            ModifyType = modifyType;
        }
        
    }
}