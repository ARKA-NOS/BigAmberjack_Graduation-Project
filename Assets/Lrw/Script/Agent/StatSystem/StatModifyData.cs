using System;

namespace Lrw.Script.Agent.StatSystem
{
    [Serializable]
    public readonly struct StatModifyData
    {
        public readonly int Priority;
        public readonly float Value;
        public readonly ModifyMathType Type;

        public StatModifyData(int priority, float value, ModifyMathType type = ModifyMathType.Add)
        {
            Priority = priority;
            Value = value;
            Type = type;
        }
        
    }
}