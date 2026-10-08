using System;
using System.Collections.Generic;

namespace Lrw.Script.Agent.StatSystem
{
    public class ModifyGroup
    {
        private float _addValue;
        private float _multiplyValue;
        
        public readonly int Priority;
        public ModifyGroup(int priority)
        {
            Priority = priority;
            _addValue = 0f;
            _multiplyValue = 1f;
        }
        
        public void AddModifyData(float value,ModifyMathType type)
        {
            switch (type)
            {
                case ModifyMathType.Add:
                    _addValue += value;
                    break;
                case ModifyMathType.Multiply:
                    _multiplyValue += value;
                    break;
            }
        }

        public float GetValue(float baseValue)
            => (baseValue * _multiplyValue) + _addValue;
        
    }
}