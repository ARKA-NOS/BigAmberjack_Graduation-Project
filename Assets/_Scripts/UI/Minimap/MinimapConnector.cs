using _Scripts.Maps;
using _Scripts.UI.MVVM.Connectors;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _Scripts.UI.Minimap
{
    // 씬의 스테이지 생성기를 MinimapViewModel에 연결한다. 스테이지 생성기와 플레이어는 미니맵을 모른다.
    // 미니맵 캔버스 프리팹에 붙어 있다.
    // 이 프리팹은 DontDestroyOnLoad인 PlayerPack 아래에 있어 씬을 옮겨도 살아남지만 스테이지 생성기는 씬과 함께 사라지므로,
    // 씬이 바뀔 때마다 새 씬에서 스테이지 생성기를 다시 찾아 연결한다.
    public class MinimapConnector : ConnectorBase
    {
        [Tooltip("비워두면 씬에서 찾는다. (프리팹으로 배치할 때는 씬 참조를 걸 수 없으므로)")]
        [SerializeField] private LinearStageGenerator stageGenerator;
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

        // sceneLoaded는 새 씬의 Awake/OnEnable 뒤, Start 앞에 불린다. 스테이지 생성기는 Start에서 방을 만들고 첫 방에 들어가므로
        // 지금 연결하면 RoomEntered 이벤트를 놓치지 않는다. (Additive 로드는 씬 이동이 아니므로 무시한다)
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

            // 직렬화 필드에 캐시하지 않는다. 씬이 바뀌면 이전 씬의 생성기를 가리키게 되므로 연결할 때마다 현재 씬에서 찾는다.
            LinearStageGenerator generator = stageGenerator != null
                ? stageGenerator
                : FindAnyObjectByType<LinearStageGenerator>();

            if (generator == null)
            {
                Debug.LogWarning($"{name} : LinearStageGenerator가 현재 씬에 없어 미니맵을 갱신하지 않습니다.");
                return;
            }

            viewModel.Connect(new StageMinimapSource(generator));
        }

        protected override void Disconnect()
        {
            if (viewModel != null)
                viewModel.Disconnect();
        }
    }
}
