using _Scripts.UI.MVVM.Bindings;
using _Scripts.UI.MVVM.Disposables;
using _Scripts.UI.MVVM.Views;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Scripts.UI.CurrencyUI
{
    // 재화 한 칸(아이콘 + 수량). 인스펙터에서 보여줄 재화의 CurrencyViewModel 에셋을 연결한다.
    // 비워 둔 칸(이름 글자 등)은 건너뛴다.
    public class CurrencyView : ViewBase<CurrencyViewModel>
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text countText;
        [Tooltip("재화 이름을 보여줄 글자. 없으면 비워 둔다.")]
        [SerializeField] private TMP_Text nameText;

        protected override void OnBind(CurrencyViewModel vm)
        {
            if (iconImage != null)
                vm.Icon.Subscribe(ShowIcon).AddTo(Disposables);

            if (countText != null)
                vm.Count.BindText(countText, count => count.ToString()).AddTo(Disposables);

            if (nameText != null)
                vm.CurrencyName.BindText(nameText).AddTo(Disposables);
        }

        private void ShowIcon(Sprite sprite)
        {
            iconImage.sprite = sprite;
            iconImage.enabled = sprite != null;
        }
    }
}
