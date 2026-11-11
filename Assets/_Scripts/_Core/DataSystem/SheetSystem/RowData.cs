using System;
using System.Collections;
using System.Collections.Generic;

namespace _Scripts._Core.DataSystem.SheetSystem
{
    [Serializable]
    public class RowData : IEnumerable<KeyValuePair<string, string>>
    {
        private readonly Dictionary<string, string> dataList = new();
        
        public string this[string key]
        {
            get => dataList.GetValueOrDefault(key, string.Empty);
            set => dataList[key] = value;
        }
        
        public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => dataList.GetEnumerator();
        
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        
    }
}