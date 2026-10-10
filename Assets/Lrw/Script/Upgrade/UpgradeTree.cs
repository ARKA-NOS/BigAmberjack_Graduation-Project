using System.Collections.Generic;
using DevLib.ModuleSystem;
using Lrw.Script.Upgrade.SO;

namespace Lrw.Script.Upgrade
{
    public class UpgradeTree
    {
        public readonly UpgradeTreeSO TreeSo;
        
        public Dictionary<UpgradeNodeSO,bool> Data { get; private set; }
        
        public UpgradeTree(UpgradeTreeSO treeSo,Dictionary<string,bool> data)
        {
            TreeSo = treeSo;
            Data = new();
            
            foreach (UpgradeNodeSO node in TreeSo.Nodes)
            {
                if (node == null) continue;
                bool upgradeValue = data?.GetValueOrDefault(node.NodeName, false) ?? false;
                Data.TryAdd(node, upgradeValue);
            }
        }

        public void SetUpgrade(UpgradeNodeSO nodeSo,bool upgrade)
        {
            if(!Data.ContainsKey(nodeSo)) return;
            Data[nodeSo] = upgrade;
        }

        public void UpgradeOwner(ModuleOwner owner)
        {
            if (owner == null) return;
            if(Data == null) return;
            
            foreach (UpgradeNodeSO node in Data.Keys)
            {
                if(node.UpgradeData == null) continue;
                if(!Data[node]) continue;
                node.UpgradeData.AgentUpgrade(owner);
            }
        }
        
        
    }
}