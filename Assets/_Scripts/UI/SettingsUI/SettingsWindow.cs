using _Scripts.UI.UI_System;
using UnityEngine;
using UnityEngine.UI;

namespace _Scripts.UI.SettingsUI
{
    // 설정 창. 시간 정지·스킬 입력 차단·페이드는 PausingWindow가 처리하고, 여기서는 화면 전환과 저장만 한다.
    // 열 때는 항상 메인 화면부터 보여 주고, 닫을 때 설정을 저장한다.
    public class SettingsWindow : PausingWindow
    {
        [SerializeField] private SettingsViewModel viewModel;
        [SerializeField] private SettingsConnector connector;

        [Header("Pages")]
        [SerializeField] private GameObject mainPage;
        [SerializeField] private GameObject keysPage;

        [Header("Buttons")]
        [SerializeField] private Button backButton;
        [SerializeField] private Button openKeysButton;
        [SerializeField] private Button closeKeysButton;

        public bool IsKeysPageOpen => keysPage.activeSelf;

        protected override void Awake()
        {
            base.Awake();
            backButton.onClick.AddListener(RequestClose);
            openKeysButton.onClick.AddListener(ShowKeysPage);
            closeKeysButton.onClick.AddListener(ShowMainPage);
            ShowMainPage();
        }

        private void OnDestroy()
        {
            backButton.onClick.RemoveListener(RequestClose);
            openKeysButton.onClick.RemoveListener(ShowKeysPage);
            closeKeysButton.onClick.RemoveListener(ShowMainPage);
        }

        public void ShowMainPage() => SetPage(false);

        public void ShowKeysPage() => SetPage(true);

        // UIManager는 같은 창을 다시 보내면 닫으므로, 열려 있을 때만 보낸다.
        public void RequestClose()
        {
            if (IsOpen) UIManagerEvents.OpenCloseWindow(this);
        }

        protected override void OnOpened(object data)
        {
            if (connector != null) connector.RefreshModel();
            ShowMainPage();
        }

        protected override void OnClosed()
        {
            if (viewModel != null) viewModel.Save();
        }

        private void SetPage(bool keys)
        {
            mainPage.SetActive(!keys);
            keysPage.SetActive(keys);
        }
    }
}
