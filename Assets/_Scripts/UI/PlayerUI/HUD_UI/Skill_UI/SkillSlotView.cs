using _Scripts.UI.MVVM.Bindings;
using _Scripts.UI.MVVM.Disposables;
using _Scripts.UI.MVVM.Views;
using UnityEngine;
using UnityEngine.UI;

namespace _Scripts.UI.PlayerUI.HUD_UI.Skill_UI
{
    // HUD의 스킬 슬롯 하나. 아이콘, 쿨타임 덮개, 사용 불가 표시를 그린다. 비워 둔 이미지는 건너뛴다.
    public class SkillSlotView : ViewBase<SkillSlotViewModel>
    {
        [SerializeField] private Image skillImage;
        [SerializeField] private Image coolTimeImage;
        [SerializeField] private Image blockImage;

        protected override void OnBind(SkillSlotViewModel vm)
        {
            if (skillImage != null)
                vm.Icon.Subscribe(ShowIcon).AddTo(Disposables);

            if (coolTimeImage != null)
            {
                vm.HasCoolTime.BindActive(coolTimeImage.gameObject).AddTo(Disposables);
                vm.CoolTimeRatio.BindFill(coolTimeImage).AddTo(Disposables);
            }

            if (blockImage != null)
                vm.Blocked.BindActive(blockImage.gameObject).AddTo(Disposables);
        }

        private void ShowIcon(Sprite sprite)
        {
            skillImage.gameObject.SetActive(sprite != null);
            skillImage.sprite = sprite;
        }
    }
}
