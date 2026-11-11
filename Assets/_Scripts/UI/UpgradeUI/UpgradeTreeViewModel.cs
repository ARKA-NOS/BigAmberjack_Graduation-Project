using System.Collections.Generic;
using _Scripts.UI.MVVM.Observables;
using _Scripts.UI.MVVM.ViewModels;
using _Scripts.Upgrade;
using _Scripts.Upgrade.SO;
using UnityEngine;

namespace _Scripts.UI.UpgradeUI
{
    // 테크트리 창의 ViewModel. 노드마다 표시 상태를 만들고, 트리가 바뀌면(구매) 모든 노드 상태를 다시 계산한다.
    // 3택1처럼 노드 하나를 사면 다른 노드가 잠기므로 바뀐 노드만이 아니라 전체를 다시 계산한다.
    // 창마다 런타임 인스턴스(CreateRuntime)로 쓰므로 에셋을 만들지 않아도 된다.
    [CreateAssetMenu(fileName = "Upgrade Tree View Model", menuName = "UI/MVVM/Upgrade Tree View Model")]
    public class UpgradeTreeViewModel : ViewModelBase<UpgradeTree>
    {
        private readonly Dictionary<UpgradeNodeSO, NodeEntry> _nodes = new();
        private readonly List<NodeEntry> _refreshBuffer = new();
        private readonly Observable<UpgradeNodeSO> _selectedNode = new();
        private UpgradeTree _tree;

        // 정보 패널에 보여줄 노드. null이면 비운다.
        public IReadOnlyObservable<UpgradeNodeSO> SelectedNode => _selectedNode;

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
                    Item = new UpgradeNodeItem(node, state, () => Select(node), () => Purchase(node)),
                };
                _nodes.Add(node, entry);
            }

            return entry.Item;
        }

        protected override void OnConnect(UpgradeTree tree)
        {
            _tree = tree;
            _tree.OnUpgradeChanged += HandleUpgradeChanged;
            RefreshAll();
        }

        protected override void OnDisconnect()
        {
            _tree.OnUpgradeChanged -= HandleUpgradeChanged;
            _tree = null;
        }

        // OnEnable에서도 불린다. _tree는 비어 있으므로 모든 노드가 Locked가 된다.
        protected override void ResetState()
        {
            _selectedNode.Value = null;
            RefreshAll();
        }

        private void HandleUpgradeChanged(UpgradeNodeSO node) => RefreshAll();

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
            return _tree.CanUpgrade(node) ? UpgradeNodeState.Available : UpgradeNodeState.Locked;
        }

        private void Select(UpgradeNodeSO node) => _selectedNode.Value = node;

        // 결과는 트리의 OnUpgradeChanged로 돌아와서 노드 상태에 반영된다.
        private void Purchase(UpgradeNodeSO node)
        {
            Select(node);
            if (_tree != null) _tree.TryUpgrade(node);
        }
    }
}
