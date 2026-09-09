using System;
using System.Collections.Generic;
using System.Linq;

namespace Lrw.Script.Agent.StatSystem
{
    public static class ModifyCalculator
    {
        public static float Calculate(float baseValue,StatModifyData[] modifyArr)
        {
            if(modifyArr == null) throw new Exception("modifyArr is null");
            
            Dictionary<int, ModifyGroup> modifyGroups = new();

            foreach (StatModifyData modifyData in modifyArr)
            {
                if (!modifyGroups.TryGetValue(modifyData.Priority, out ModifyGroup modifyGroup))
                {
                    modifyGroup = new ModifyGroup(modifyData.Priority);
                    modifyGroups.Add(modifyData.Priority,modifyGroup);
                }
                modifyGroup.AddModifyData(modifyData.Value,modifyData.Type);
            }
            
            ModifyGroup[] groups = modifyGroups.Values.OrderBy(x => x.Priority).ToArray();
            
            float value = baseValue;

            foreach (ModifyGroup group in groups)
            {
                value = group.GetValue(value);
            }
            
            return value;
        }
    }
}