using System;
using System.Collections.Generic;
using System.Linq;
using Lrw.Script._Core._Debug;
using UnityEngine;

namespace Lrw.Script.Agent.StatSystem
{
    public class Stat
    {
        public float BaseValue { get; private set; }
        
        private readonly Dictionary<object,StatModifyData> _modifyDict;
        
        private readonly StatData _statData;

        public float Value { get; private set; }
        public float ModifyValue => Value - BaseValue;
        public float PercentChange => ChangePercent(BaseValue,Value);
        public event StatValueChanged OnValueChanged;

        public delegate void StatValueChanged(float newValue, float delta);
        
        public Stat(StatData data,float baseValue)
        {
            if(data == null) throw new Exception("Stat data cannot be null");
            _statData = data;
            BaseValue = baseValue;
            _modifyDict = new();
            UpdateValue();
        }
        
        /// <summary>
        /// 최대한 사용하지 않기
        /// </summary>
        public void SetBaseValue(float value)
        {
            BaseValue = value;
            UpdateValue();
        }
        
        
        public void SetModify(object key, StatModifyData modifyData)
        {
            _modifyDict[key] = modifyData;
            UpdateValue();
        }
        
        public void RemoveModify(object key)
        {
            _modifyDict.Remove(key);
            UpdateValue();
        }
        
        private void UpdateValue()
        {
            float prevValue = Value;
            
            float value = ModifyCalculator.Calculate(BaseValue, _modifyDict.Values.ToArray());
            
            Value = _statData.UseValueRange ?
                Mathf.Clamp(value,_statData.ValueRange.x,_statData.ValueRange.y)
                : value;
            
            if (!Mathf.Approximately(prevValue, Value))
            {
                OnValueChanged?.Invoke(Value,Value - prevValue);
            }
        }
        
        private float ChangePercent(float before, float after)
        {
            if (Mathf.Approximately(before, 0f))
            {
                FDebug.LogError($"[{_statData.StatName} Stat] Cannot calculate percent change: the initial value is zero.");
                return 0f;
            }
            
            return (after - before) / before * 100f;
        }
        
        
    }
}
