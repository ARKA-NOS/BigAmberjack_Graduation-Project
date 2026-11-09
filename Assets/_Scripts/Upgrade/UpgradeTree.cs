using System.Collections.Generic;
using _Scripts.DevLib.ModuleSystem;
using _Scripts.Upgrade.SO;

namespace _Scripts.Upgrade
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

        public bool IsUpgrade(UpgradeNodeSO nodeSo)
            => Data.GetValueOrDefault(nodeSo, false);

        public bool CanUpgrade(UpgradeNodeSO nodeSo)
        {
            if (nodeSo == null || !Data.ContainsKey(nodeSo)) return false;
            if (Data[nodeSo]) return false;

            UpgradeNodeSO[] prevNodes = nodeSo.PrevNodes;
            if (prevNodes != null && prevNodes.Length > 0)
            {
                bool prevUpgraded = false;
                foreach (UpgradeNodeSO prev in prevNodes)
                {
                    if (!IsUpgrade(prev)) continue;
                    prevUpgraded = true;
                    break;
                }
                if (!prevUpgraded) return false;
            }

            if (string.IsNullOrEmpty(nodeSo.ExclusiveGroup)) return true;

            foreach (KeyValuePair<UpgradeNodeSO, bool> pair in Data)
            {
                if (!pair.Value || pair.Key == nodeSo) continue;
                if (pair.Key.ExclusiveGroup == nodeSo.ExclusiveGroup) return false;
            }

            return true;
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