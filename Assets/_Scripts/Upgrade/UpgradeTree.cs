using System;
using System.Collections.Generic;
using _Scripts._Core._Debug;
using _Scripts.Currency;
using _Scripts.DevLib.ModuleSystem;
using _Scripts.Upgrade.SO;

namespace _Scripts.Upgrade
{
    public class UpgradeTree
    {
        public readonly UpgradeTreeSO TreeSo;
        
        public Dictionary<UpgradeNodeSO,bool> Data { get; private set; }

        // 노드의 구매 상태가 바뀌었을 때. 인자는 바뀐 노드.
        public event Action<UpgradeNodeSO> OnUpgradeChanged;

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
            if(!Data.TryGetValue(nodeSo, out bool prev)) return;
            if(prev == upgrade) return;
            Data[nodeSo] = upgrade;
            OnUpgradeChanged?.Invoke(nodeSo);
        }

        // 구매 조건(선행 노드, 3택1)과 재화를 확인하고, 되면 재화를 쓰고 구매한다.
        public bool TryUpgrade(UpgradeNodeSO nodeSo)
        {
            if (nodeSo == null) return false;

            if (!CanUpgrade(nodeSo))
            {
                FDebug.Log($"[UpgradeTree] {nodeSo.NodeName} 구매 불가 (이미 구매했거나 선행 노드 미충족 / 3택1 선택됨)");
                return false;
            }

            foreach (Cost cost in nodeSo.UpgradeCost)
            {
                if (!CurrencySystem.GetCurrencyData(cost.Type).Has(cost.Amount))
                {
                    FDebug.Log($"[UpgradeTree] {nodeSo.NodeName} 구매 불가 (재화 부족: {cost.Type} {cost.Amount})");
                    return false;
                }
            }

            foreach (Cost cost in nodeSo.UpgradeCost)
            {
                CurrencySystem.GetCurrencyData(cost.Type).Add(-cost.Amount);
            }

            SetUpgrade(nodeSo, true);
            FDebug.Log($"[UpgradeTree] {nodeSo.NodeName} 구매 완료");
            return true;
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