using _Scripts.UI.MVVM.Connectors;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _Scripts.UI.Minimap
{
    // 지금 씬의 지형을 MinimapViewModel에 연결한다. 맵 쪽과 플레이어는 미니맵을 모른다.
    // 미니맵 캔버스 프리팹에 붙어 있다.
    // 이 프리팹은 DontDestroyOnLoad인 PlayerPack 아래에 있어 씬을 옮겨도 살아남지만 지형은 씬과 함께 사라지므로,
    // 씬이 바뀔 때마다 다시 연결한다. 같은 씬 안에서 맵이 바뀌는 것은 IMinimapSource.AreaChanged로 받는다.
    public class MinimapConnector : ConnectorBase
    {
        [SerializeField] private MinimapViewModel viewModel;

        protected override void OnEnable()
        {
            base.OnEnable();
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        protected override void OnDisable()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            base.OnDisable();
        }

        // Additive 로드는 씬 이동이 아니므로 무시한다.
        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode != LoadSceneMode.Single) return;

            Reconnect();
        }

        protected override void Connect()
        {
            if (viewModel == null)
            {
                Debug.LogWarning($"{name} : MinimapViewModel이 지정되지 않았습니다.");
                return;
            }

            viewModel.Connect(new SceneMinimapSource());
        }

        protected override void Disconnect()
        {
            if (viewModel != null)
                viewModel.Disconnect();
        }
    }
}
