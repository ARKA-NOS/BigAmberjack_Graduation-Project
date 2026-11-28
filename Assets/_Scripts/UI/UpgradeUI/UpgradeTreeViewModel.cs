using System.Collections.Generic;
using _Scripts.Currency;
using _Scripts.UI.MVVM.Observables;
using _Scripts.UI.MVVM.ViewModels;
using _Scripts.Upgrade;
using _Scripts.Upgrade.SO;
using UnityEngine;

namespace _Scripts.UI.UpgradeUI
{
    // 테크트리 창의 ViewModel. 노드마다 표시 상태를 만들고, 임시 선택이나 트리가 바뀌면 모든 노드 상태를 다시 계산한다.
    // 3택1처럼 노드 하나를 고르면 다른 노드가 잠기므로 바뀐 노드만이 아니라 전체를 다시 계산한다.
    // 창마다 런타임 인스턴스(CreateRuntime)로 쓰므로 에셋을 만들지 않아도 된다.
    [CreateAssetMenu(fileName = "Upgrade Tree View Model", menuName = "UI/MVVM/Upgrade Tree View Model")]
    public class UpgradeTreeViewModel : ViewModelBase<UpgradeTree>
    {
        private readonly Dictionary<UpgradeNodeSO, NodeEntry> _nodes = new();
        private readonly List<NodeEntry> _refreshBuffer = new();
        private readonly Observable<UpgradeNodeSO> _selectedNode = new();
        private readonly Observable<UpgradeWalletData> _wallet = new();
        private readonly Observable<bool> _hasPending = new();
        private UpgradeTree _tree;
        private UpgradeSelection _selection;
        // 트리의 노드 비용에 처음 나오는 재화. 창의 재화 칸에 보여준다.
        private CurrencySO _currency;
        private CurrencyData _currencyData;

        // 정보 패널에 보여줄 노드. null이면 비운다.
        public IReadOnlyObservable<UpgradeNodeSO> SelectedNode => _selectedNode;

        // 임시 선택분을 뺀 남은 재화.
        public IReadOnlyObservable<UpgradeWalletData> Wallet => _wallet;

        // 확정할 임시 선택이 있는지.
        public IReadOnlyObservable<bool> HasPending => _hasPending;

        private sealed class NodeEntry
        {
            public Observable<UpgradeNodeState> State;
            public UpgradeNodeItem Item;
        }

        // 노드 하나의 표시 데이터. 연결 전에도 받을 수 있고(Locked), 연결되면 값이 채워진다.
        public UpgradeNodeItem GetNode(UpgradeNodeSO node)
        {
            if (node == null) return null;

            if (!_nodes.TryGetValue(node, out NodeEntry entry))
            {
                Observable<UpgradeNodeState> state = new(GetState(node));
                entry = new NodeEntry
                {
                    State = state,
                    Item = new UpgradeNodeItem(node, state, () => Select(node), () => Toggle(node)),
                };
                _nodes.Add(node, entry);
            }

            return entry.Item;
        }

        // 임시 선택한 노드를 실제로 구매한다.
        public void Confirm() => _selection?.Confirm();

        // 확정하지 않은 선택을 버린다.
        public void DiscardPending()
        {
            _selection?.Clear();
            _selectedNode.Value = null;
        }

        // 임시 선택을 지우고, 구매한 노드를 모두 되돌려 재화를 환불한다.
        public void ResetTree()
        {
            if (_tree == null) return;

            _selection.Clear();
            _tree.ResetAll();
            _selectedNode.Value = null;
        }

        protected override void OnConnect(UpgradeTree tree)
        {
            _tree = tree;
            _tree.OnUpgradeChanged += HandleUpgradeChanged;

            _selection = new UpgradeSelection(tree);
            _selection.OnSelectionChanged += HandleSelectionChanged;

            _currency = FindCurrency(tree);
            _currencyData = _currency != null ? CurrencySystem.GetCurrencyData(_currency) : null;
            if (_currencyData != null) _currencyData.OnCurrencyChanged += HandleCurrencyChanged;

            HandleSelectionChanged();
        }

        protected override void OnDisconnect()
        {
            _tree.OnUpgradeChanged -= HandleUpgradeChanged;
            _selection.OnSelectionChanged -= HandleSelectionChanged;
            if (_currencyData != null) _currencyData.OnCurrencyChanged -= HandleCurrencyChanged;

            _tree = null;
            _selection = null;
            _currency = null;
            _currencyData = null;
        }

        // OnEnable에서도 불린다. _tree는 비어 있으므로 모든 노드가 Locked가 된다.
        protected override void ResetState()
        {
            _selectedNode.Value = null;
            _wallet.Value = default;
            _hasPending.Value = false;
            RefreshAll();
        }

        private void HandleUpgradeChanged(UpgradeNodeSO node) => RefreshAll();

        private void HandleSelectionChanged()
        {
            _hasPending.Value = _selection.HasPending;
            RefreshWallet();
            RefreshAll();
        }

        private void HandleCurrencyChanged(int current, int prev) => RefreshWallet();

        private void RefreshWallet()
        {
            if (_currencyData == null)
            {
                _wallet.Value = default;
                return;
            }

            _wallet.Value = new UpgradeWalletData(_currency.CurrencyIcon,
                _currencyData.Count - _selection.GetPendingCost(_currency));
        }

        private static CurrencySO FindCurrency(UpgradeTree tree)
        {
            foreach (UpgradeNodeSO node in tree.TreeSo.Nodes)
            {
                if (node == null || node.UpgradeCost == null || node.UpgradeCost.Length == 0) continue;
                return node.UpgradeCost[0].Type;
            }
            return null;
        }

        // 상태 알림을 받은 View가 GetNode로 새 노드를 만들어도 순회가 깨지지 않게 복사본을 돈다.
        private void RefreshAll()
        {
            _refreshBuffer.Clear();
            _refreshBuffer.AddRange(_nodes.Values);

            foreach (NodeEntry entry in _refreshBuffer)
                entry.State.Value = GetState(entry.Item.Node);
        }

        private UpgradeNodeState GetState(UpgradeNodeSO node)
        {
            if (_tree == null) return UpgradeNodeState.Locked;
            if (_tree.IsUpgrade(node)) return UpgradeNodeState.Purchased;
            if (_selection.IsPending(node)) return UpgradeNodeState.Pending;
            return _selection.CanSelect(node) ? UpgradeNodeState.Available : UpgradeNodeState.Locked;
        }

        private void Select(UpgradeNodeSO node) => _selectedNode.Value = node;

        // 임시 선택을 켜고 끈다. 결과는 OnSelectionChanged로 돌아와서 노드 상태에 반영된다.
        private void Toggle(UpgradeNodeSO node)
        {
            Select(node);
            if (_tree != null && !_tree.IsUpgrade(node)) _selection.Toggle(node);
        }
    }
}
