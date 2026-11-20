using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace _Scripts._Core.DataSystem.SheetSystem
{
    [Serializable]
    public class RowData : IReadOnlyDictionary<string,string>
    {
        private readonly Dictionary<string, string> dataList = new();

        public string this[string key]
        {
            get => dataList.GetValueOrDefault(key, string.Empty);
            set => dataList[key] = value;
        }
        
        public int Count => dataList.Count;
        public IEnumerable<string> Keys => dataList.Keys;
        public IEnumerable<string> Values => dataList.Values;
        public bool ContainsKey(string key) => dataList.ContainsKey(key);
        public bool TryGetValue(string key, out string value) =>  dataList.TryGetValue(key, out value);
        public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => dataList.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}