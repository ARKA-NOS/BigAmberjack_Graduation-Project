using _Scripts._Core;
using _Scripts._Core._Debug;
using _Scripts._Core.SceneSystem;
using _Scripts.UI.UI_System;
using UnityEngine;
using UnityEngine.UI;

namespace _Scripts.UI.MainMenu
{
    // 타이틀 화면의 버튼(시작·설정·나가기)을 처리한다. 보여 줄 게임 데이터가 없어 ViewModel은 두지 않는다.
    // 코드에서 Title은 칭호를 뜻하므로 타이틀 화면은 MainMenu로 부른다.
    public class MainMenuView : MonoBehaviour
    {
        [SerializeField] private SceneSo homeScene;
        [SerializeField] private AbstractWindow settingsWindow;

        [Header("Buttons")]
        [SerializeField] private Button startButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button quitButton;

        private bool _isStarting;

        private void OnEnable()
        {
            startButton.onClick.AddListener(StartGame);
            settingsButton.onClick.AddListener(OpenSettings);
            quitButton.onClick.AddListener(Game.Exit);
        }

        private void OnDisable()
        {
            startButton.onClick.RemoveListener(StartGame);
            settingsButton.onClick.RemoveListener(OpenSettings);
            quitButton.onClick.RemoveListener(Game.Exit);
        }

        private void StartGame()
        {
            if (_isStarting) return;

            if (homeScene == null)
            {
                FDebug.LogError($"[{name}] homeScene이 지정되지 않았습니다.", this);
                return;
            }

            _isStarting = true;
            // 타이틀의 GameManager는 홈까지 남아 로딩창과 소리가 이어진다. 홈의 중복 GameManager는 스스로 파괴된다.
            SceneLoader.LoadScene(homeScene);
        }

        private void OpenSettings() => UIManagerEvents.OpenCloseWindow(settingsWindow);
    }
}
