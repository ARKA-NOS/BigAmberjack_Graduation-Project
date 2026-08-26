using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Lrw.Script.StatSystem
{
    public class Stat
    {
        private readonly float _baseValue;
        
        private readonly Dictionary<object,StatModifyData> _modifyDict;
        
        private readonly StatData _statData;
        public float Value { get; private set; }
        public float ModifyValue => Value - _baseValue;
        
        public event StatValueChanged OnValueChanged;

        public delegate void StatValueChanged(float newValue, float delta);
        
        public Stat(StatData data,float baseValue)
        {
            if(data == null) throw new Exception("Stat data cannot be null");
            _statData = data;
            _baseValue = baseValue;
            _modifyDict = new();
            UpdateValue();
        }
        
        public void AddModify(object key, StatModifyData modifyData)
        {
            _modifyDict[key] = modifyData;
        }
        
        public void RemoveModify(object key)
        {
            _modifyDict.Remove(key);
        }
        
        public void UpdateValue()
        {
            float prevValue = Value;
            
            float value = _baseValue;
            
            foreach (StatModifyData modifyData in _modifyDict.Values.OrderBy(data => data.Priority))
            {
                value = modifyData.GetValue(value);
            }
            
            Value = Mathf.Clamp(value,_statData.ValueRange.x,_statData.ValueRange.y);
            
            if (!Mathf.Approximately(prevValue, Value))
            {
                OnValueChanged?.Invoke(Value,Value - prevValue);
            }
        }
        
        public float UpdateAndGetValue()
        {
            UpdateValue();
            return Value;
        }
        
        

    }
}
