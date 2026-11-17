using System;
using _Scripts.UI.MVVM.Observables;
using _Scripts.Upgrade.SO;

namespace _Scripts.UI.UpgradeUI
{
    // UpgradeTreeViewModel이 노드마다 하나씩 만드는 표시용 데이터. 노드 View는 State를 구독하고 Action을 호출만 한다.
    public class UpgradeNodeItem
    {
        public UpgradeNodeSO Node { get; }
        public IReadOnlyObservable<UpgradeNodeState> State { get; }

        // 정보 패널에 이 노드를 보여준다(마우스를 올렸을 때).
        public Action Select { get; }

        // 구매를 시도한다(클릭했을 때). 정보 패널에도 이 노드가 표시된다.
        public Action Purchase { get; }

        public UpgradeNodeItem(UpgradeNodeSO node, IReadOnlyObservable<UpgradeNodeState> state, Action select, Action purchase)
        {
            Node = node;
            State = state;
            Select = select;
            Purchase = purchase;
        }
    }
}
