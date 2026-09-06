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
            if (type == ModifyMathType.Add)
            {
                _addValue += value;
            }
            else if (type == ModifyMathType.Multiply)
            {
                _multiplyValue += value;
            }
        }

        public float GetValue(float baseValue)
            => _addValue + (baseValue * _multiplyValue);
        
    }
}