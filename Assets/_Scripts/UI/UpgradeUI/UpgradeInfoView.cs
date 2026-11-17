using _Scripts.UI.MVVM.Disposables;
using _Scripts.UI.MVVM.Views;
using _Scripts.Upgrade.SO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Scripts.UI.UpgradeUI
{
    // 선택한(마우스를 올리거나 클릭한) 노드의 아이콘과 설명을 보여준다.
    // ViewModel은 UpgradeTreeWindow가 SetViewModel로 넣는다.
    public class UpgradeInfoView : ViewBase<UpgradeTreeViewModel>
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text descriptionText;

        protected override void OnBind(UpgradeTreeViewModel vm)
        {
            vm.SelectedNode.Subscribe(Show).AddTo(Disposables);
        }

        private void Show(UpgradeNodeSO node)
        {
            iconImage.sprite = node != null ? node.NodeIcon : null;
            descriptionText.text = node != null ? node.NodeDescription : string.Empty;
        }
    }
}
