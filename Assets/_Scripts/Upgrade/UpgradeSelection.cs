using System;
using System.Collections.Generic;
using _Scripts._Core._Debug;
using _Scripts.Currency;
using _Scripts.Upgrade.SO;

namespace _Scripts.Upgrade
{
    // 확정 전의 임시 선택. 고른 노드는 구매한 것처럼 다음 노드를 열지만, 재화 차감과 저장 대상 반영은 Confirm에서만 일어난다.
    public class UpgradeSelection
    {
        private readonly UpgradeTree _tree;
        // 고른 순서를 유지해야 Confirm에서 선행 노드부터 구매된다.
        private readonly List<UpgradeNodeSO> _pending = new();
        private readonly Predicate<UpgradeNodeSO> _isOwned;

        public event Action OnSelectionChanged;

        public bool HasPending => _pending.Count > 0;

        public UpgradeSelection(UpgradeTree tree)
        {
            _tree = tree;
            _isOwned = node => _tree.IsUpgrade(node) || _pending.Contains(node);
        }

        public bool IsPending(UpgradeNodeSO node) => _pending.Contains(node);

        // 선행 노드와 3택1 조건만 본다. 재화는 Toggle에서 확인한다.
        // 같은 3택1 그룹을 임시 선택해 둔 경우는 교체할 수 있으므로 고를 수 있다(구매한 것은 교체할 수 없다).
        public bool CanSelect(UpgradeNodeSO node)
        {
            if (IsPending(node)) return false;

            UpgradeNodeSO rival = FindPendingRival(node);
            if (rival == null) return _tree.CanUpgrade(node, _isOwned);

            return _tree.CanUpgrade(node, other => other != rival && _isOwned(other));
        }

        // 임시 선택분의 비용 합.
        public int GetPendingCost(CurrencySO type)
        {
            int total = 0;
            foreach (UpgradeNodeSO node in _pending)
                total += GetCost(node, type);
            return total;
        }

        private static int GetCost(UpgradeNodeSO node, CurrencySO type)
        {
            if (node == null) return 0;

            int total = 0;
            foreach (Cost cost in node.UpgradeCost)
            {
                if (cost.Type == type) total += cost.Amount;
            }
            return total;
        }

        // 임시 선택한 노드면 취소하고, 아니면 고른다. 같은 3택1 그룹의 임시 선택이 있으면 그것과 바꾼다.
        public void Toggle(UpgradeNodeSO node)
        {
            if (!_pending.Remove(node))
            {
                if (!CanSelect(node)) return;

                UpgradeNodeSO rival = FindPendingRival(node);
                if (!CanAfford(node, rival)) return;

                // 교체할 때는 같은 자리에 넣어야 뒤에 고른 노드보다 먼저 구매된다.
                if (rival != null) _pending[_pending.IndexOf(rival)] = node;
                else _pending.Add(node);
            }

            RemoveOrphans();
            OnSelectionChanged?.Invoke();
        }

        // 임시 선택을 고른 순서대로 실제 구매한다.
        public void Confirm()
        {
            if (!HasPending) return;

            // 구매 알림을 받은 쪽이 재화를 두 번 빼서 보지 않게 먼저 비운다.
            List<UpgradeNodeSO> nodes = new(_pending);
            _pending.Clear();

            foreach (UpgradeNodeSO node in nodes)
                _tree.TryUpgrade(node);

            OnSelectionChanged?.Invoke();
        }

        public void Clear()
        {
            if (!HasPending) return;

            _pending.Clear();
            OnSelectionChanged?.Invoke();
        }

        // 선행 노드가 취소되어 끊긴 임시 노드를 함께 취소한다.
        private void RemoveOrphans()
        {
            bool removed;
            do
            {
                removed = false;
                for (int i = _pending.Count - 1; i >= 0; i--)
                {
                    if (_tree.HasPrevNode(_pending[i], _isOwned)) continue;
                    _pending.RemoveAt(i);
                    removed = true;
                }
            } while (removed);
        }

        // 같은 3택1 그룹에서 임시 선택해 둔 노드. 없으면 null.
        private UpgradeNodeSO FindPendingRival(UpgradeNodeSO node)
        {
            if (node == null || string.IsNullOrEmpty(node.ExclusiveGroup)) return null;

            foreach (UpgradeNodeSO other in _pending)
            {
                if (other != node && other.ExclusiveGroup == node.ExclusiveGroup) return other;
            }
            return null;
        }

        // replaced: 이 노드로 바뀌며 빠지는 임시 노드. 그 비용은 다시 쓸 수 있다.
        private bool CanAfford(UpgradeNodeSO node, UpgradeNodeSO replaced)
        {
            foreach (Cost cost in node.UpgradeCost)
            {
                int required = GetPendingCost(cost.Type) - GetCost(replaced, cost.Type) + cost.Amount;
                if (CurrencySystem.GetCurrencyData(cost.Type).Has(required)) continue;

                FDebug.Log($"[UpgradeSelection] {node.NodeName} 선택 불가 (재화 부족: {cost.Type} {cost.Amount})");
                return false;
            }
            return true;
        }
    }
}
