using System.Collections.Generic;

namespace _Scripts._Core.DataSystem.SheetSystem
{
    public class RowData
    {
        private readonly Dictionary<string, string> dataList = new();
        
        public string this[string key]
        {
            get => dataList.GetValueOrDefault(key, string.Empty);
            set => dataList[key] = value;
        }
    }
}