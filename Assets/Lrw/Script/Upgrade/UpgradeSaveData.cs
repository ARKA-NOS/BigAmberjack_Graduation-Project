using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lrw.Script.Upgrade
{
    [Serializable]
    public class UpgradeSaveData
    {
        [field:SerializeField] public List<string> Names { get; private set; }
        [field:SerializeField] public List<List<UpgradeData>> Upgrades { get; private set; }
        
        public UpgradeSaveData()
        {
            Names = new();
            Upgrades = new();
        }

        public void Add(string name, List<UpgradeData> upgrades)
        {
            Names.Add(name);
            Upgrades.Add(upgrades);
        }
        
        
        
    }
}