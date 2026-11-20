namespace _Scripts.UI.UpgradeUI
{
    // 테크트리 노드 하나의 표시 상태.
    public enum UpgradeNodeState
    {
        // 선행 노드를 아직 사지 않았거나, 같은 3택1 그룹의 다른 노드를 이미 샀다.
        Locked,
        // 지금 살 수 있다(재화는 따로 확인한다).
        Available,
        Purchased,
    }
}
