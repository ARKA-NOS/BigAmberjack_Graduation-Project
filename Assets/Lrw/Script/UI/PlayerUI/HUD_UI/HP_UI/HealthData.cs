using System.Collections.Generic;

namespace Lrw.Script.UI.PlayerUI.HUD_UI.HP_UI
{
    public class HealthData
    {
        public readonly int HealthCount;
        public HealthData(int healthCount)
        {
            HealthCount = healthCount;
            Data = new(healthCount);
        }
        
        public readonly List<HealthViewData> Data;
        
        public void Clear() =>  Data.Clear();
        public void Add(HealthViewData data) => Data.Add(data);
    }
}