using _Scripts._Core._EventSystem;
using _Scripts._Core._ServiceLocator;
using _Scripts.Agent.Players;
using _Scripts.Agent.Players.TitleSystem;
using _Scripts.DevLib.ModuleSystem;
using _Scripts.TitleCodex;
using _Scripts.UI.MVVM.ViewModels;
using _Scripts.UI.MVVM.Views;
using _Scripts.UI.UI_System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace _Scripts.UI.PlayerUI.Title_UI.Equip
{
    // 칭호 장착 창. 칭호 NPC와 상호작용하면 열린다(TitleEquipEvents.RequestOpen).
    // 창이 열릴 때 플레이어의 칭호 모듈에 ViewModel을 연결하고, 닫을 때 끊는다(MVVM README 예시 6).
    // 열려 있는 동안 시간을 멈추는 PausingWindow다.
    public class TitleEquipWindow : PausingWindow
    {
        [SerializeField] private Button closeButton;

        private TitleEquipViewModel _viewModel;

        protected override void Awake()
        {
            base.Awake();

            _viewModel = ViewModelBase.CreateRuntime<TitleEquipViewModel>();
            foreach (ViewBase<TitleEquipViewModel> view in GetComponentsInChildren<ViewBase<TitleEquipViewModel>>(true))
                view.SetViewModel(_viewModel);

            EventBus<TitleEquipOpenRequestEvent>.Subscribe(HandleOpenRequest);
            if (closeButton != null) closeButton.onClick.AddListener(HandleCloseClick);
        }

        protected virtual void OnDestroy()
        {
            EventBus<TitleEquipOpenRequestEvent>.UnSubscribe(HandleOpenRequest);
            if (closeButton != null) closeButton.onClick.RemoveListener(HandleCloseClick);
            if (_viewModel != null) _viewModel.ReleaseRuntime();
        }

        protected override void OnOpened(object data)
        {
            if (ServiceLocator.Get<IPlayerController>() is not ModuleOwner player) return;
            if (!player.TryGetModule(out ITitleModule run) || !player.TryGetModule(out ITitleEquipModule equip)) return;

            _viewModel.Connect(new TitleEquipModel(run, equip, ServiceLocator.Get<ITitleRecordManager>()));
        }

        protected override void OnClosed() => _viewModel.Disconnect();

        private void HandleOpenRequest(TitleEquipOpenRequestEvent evt)
        {
            // UIManager는 같은 창을 다시 보내면 닫으므로, 닫혀 있을 때만 보낸다.
            if (!IsOpen) UIManagerEvents.OpenCloseWindow(this);
        }

        // null = 현재 열린 창 닫기.
        private void HandleCloseClick() => UIManagerEvents.OpenCloseWindow(null);

        // 열려 있을 때 ESC로 닫는다.
        private void Update()
        {
            if (IsOpen && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                UIManagerEvents.OpenCloseWindow(this);
        }
    }
}
