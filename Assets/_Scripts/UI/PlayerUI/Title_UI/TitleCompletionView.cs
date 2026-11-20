using _Scripts.UI.MVVM.Disposables;
using _Scripts.UI.MVVM.Views;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Scripts.UI.PlayerUI.Title_UI
{
    // 도감 달성 이력: "도감 달성 이력 n / N", 퍼센트, 진행 바.
    public class TitleCompletionView : ViewBase<TitleListViewModel>
    {
        [SerializeField] private TMP_Text countText;
        [SerializeField] private TMP_Text percentText;
        [SerializeField] private Image fillImage;

        protected override void OnBind(TitleListViewModel vm)
        {
            vm.Completion.Subscribe(Show).AddTo(Disposables);
        }

        private void Show(TitleCompletionData data)
        {
            countText.text = $"도감 달성 이력 {data.Earned} / {data.Total}";
            percentText.text = $"{Mathf.RoundToInt(data.Ratio * 100f)}%";
            fillImage.fillAmount = data.Ratio;
        }
    }
}
