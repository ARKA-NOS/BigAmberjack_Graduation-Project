using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Lrw.Script._Core._SaveSystem.Data
{
    [Serializable]
    public struct JsonDict<T, K>
    {
        [SerializeField] private T[] keys;
        [SerializeField] private K[] values;
        
        public JsonDict(Dictionary<T, K> dict)
        {
            keys = dict.Keys.ToArray();
            values = dict.Values.ToArray();
        }

        public Dictionary<T, K> ChangeDict()
        {
            Dictionary<T, K> dict = new();
            
            AddTo(dict);
            
            return dict;
        }
        
        public void AddTo(Dictionary<T,K> dict)
        {
            if (keys.Length != values.Length) throw new Exception("JsonDict Error");
            
            int count = keys.Length;

            for (int i = 0; i < count; i++) 
                dict.Add(keys[i], values[i]);
        }
    }
}