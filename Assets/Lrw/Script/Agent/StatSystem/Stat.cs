using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Lrw.Script.Agent.StatSystem
{
    public class Stat
    {
        private readonly float _baseValue;
        
        private readonly Dictionary<object,StatModifyData> _modifyDict;
        
        private readonly StatData _statData;
        public float Value { get; private set; }
        //private float _value;
        public float ModifyValue => Value - _baseValue;
        
        public event StatValueChanged OnValueChanged;

        public delegate void StatValueChanged(float newValue, float delta);

        private StatModifyData[] _sortingStatModifyDatas;
        
        public Stat(StatData data,float baseValue)
        {
            if(data == null) throw new Exception("Stat data cannot be null");
            _statData = data;
            _baseValue = baseValue;
            _modifyDict = new();
            _sortingStatModifyDatas = new StatModifyData[]{};
            UpdateValue();
        }
        
        public void AddModify(object key, StatModifyData modifyData)
        {
            _modifyDict[key] = modifyData;
            SetSortingStatModifyData();
        }
        
        public void RemoveModify(object key)
        {
            _modifyDict.Remove(key);
            SetSortingStatModifyData();
        }
        
        private void SetSortingStatModifyData()
            => _sortingStatModifyDatas = _modifyDict.Values.OrderBy(data => data.Priority).ToArray();
        
        public void UpdateValue()
        {
            float prevValue = Value;
            
            float value = _baseValue;
            
            foreach (StatModifyData modifyData in _sortingStatModifyDatas)
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
