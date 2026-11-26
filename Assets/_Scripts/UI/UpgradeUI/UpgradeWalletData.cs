using UnityEngine;

namespace _Scripts.UI.UpgradeUI
{
    // 테크트리 창의 재화 칸에 보여줄 값. 아이콘과 수량이 함께 바뀌므로 한 묶음으로 전달한다.
    public readonly struct UpgradeWalletData
    {
        public readonly Sprite Icon;
        public readonly int Amount;

        public UpgradeWalletData(Sprite icon, int amount)
        {
            Icon = icon;
            Amount = amount;
        }
    }
}
