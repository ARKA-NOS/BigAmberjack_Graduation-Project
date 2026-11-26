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

        // 구매한 노드를 모두 되돌리고 쓴 재화를 전부 돌려준다. 되돌린 노드가 하나도 없으면 false.
        public bool ResetAll()
        {
            List<UpgradeNodeSO> purchased = new();
            foreach (KeyValuePair<UpgradeNodeSO, bool> pair in Data)
            {
                if (pair.Value) purchased.Add(pair.Key);
            }

            if (purchased.Count == 0) return false;

            // 상태를 바꾸는 순간 알림을 받은 쪽이 새 상태를 보므로, 재화를 먼저 돌려준 뒤 노드를 되돌린다.
            foreach (UpgradeNodeSO node in purchased)
            {
                foreach (Cost cost in node.UpgradeCost)
                    CurrencySystem.GetCurrencyData(cost.Type).Add(cost.Amount);
            }

            foreach (UpgradeNodeSO node in purchased)
                SetUpgrade(node, false);

            FDebug.Log($"[UpgradeTree] {TreeSo.TreeName} 초기화 완료 ({purchased.Count}개 환불)");
            return true;
        }

        public bool IsUpgrade(UpgradeNodeSO nodeSo)
            => Data.GetValueOrDefault(nodeSo, false);

        public bool CanUpgrade(UpgradeNodeSO nodeSo)
            => CanUpgrade(nodeSo, IsUpgrade);

        // isOwned: 어떤 노드를 가진 것으로 볼지. 임시 선택까지 포함해 판정할 때 바꿔 넣는다.
        public bool CanUpgrade(UpgradeNodeSO nodeSo, Predicate<UpgradeNodeSO> isOwned)
        {
            if (nodeSo == null || !Data.ContainsKey(nodeSo)) return false;
            if (isOwned(nodeSo)) return false;
            if (!HasPrevNode(nodeSo, isOwned)) return false;

            if (string.IsNullOrEmpty(nodeSo.ExclusiveGroup)) return true;

            foreach (UpgradeNodeSO other in Data.Keys)
            {
                if (other == nodeSo || !isOwned(other)) continue;
                if (other.ExclusiveGroup == nodeSo.ExclusiveGroup) return false;
            }

            return true;
        }

        // 선행 노드가 없거나, 선행 노드 중 하나라도 가지고 있으면 true.
        public bool HasPrevNode(UpgradeNodeSO nodeSo, Predicate<UpgradeNodeSO> isOwned)
        {
            UpgradeNodeSO[] prevNodes = nodeSo.PrevNodes;
            if (prevNodes == null || prevNodes.Length == 0) return true;

            foreach (UpgradeNodeSO prev in prevNodes)
            {
                if (isOwned(prev)) return true;
            }

            return false;
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