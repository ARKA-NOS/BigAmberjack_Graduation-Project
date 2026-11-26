using _Scripts.UI.MVVM.Disposables;
using _Scripts.UI.MVVM.Views;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Scripts.UI.UpgradeUI
{
    // 임시 선택분을 뺀 남은 재화를 보여준다.
    // ViewModel은 UpgradeTreeWindow가 SetViewModel로 넣는다.
    public class UpgradeWalletView : ViewBase<UpgradeTreeViewModel>
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text amountText;

        protected override void OnBind(UpgradeTreeViewModel vm)
        {
            vm.Wallet.Subscribe(Show).AddTo(Disposables);
        }

        private void Show(UpgradeWalletData data)
        {
            iconImage.sprite = data.Icon;
            iconImage.enabled = data.Icon != null;
            amountText.text = data.Amount.ToString();
        }
    }
}
