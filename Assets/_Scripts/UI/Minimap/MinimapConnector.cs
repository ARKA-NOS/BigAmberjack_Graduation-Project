using _Scripts.Maps;
using _Scripts.UI.MVVM.Connectors;
using UnityEngine;

namespace _Scripts.UI.Minimap
{
    // 씬의 스테이지 생성기를 MinimapViewModel에 연결한다. 스테이지 생성기와 플레이어는 미니맵을 모른다.
    // 미니맵 캔버스 프리팹에 붙어 있다.
    public class MinimapConnector : ConnectorBase
    {
        [Tooltip("비워두면 씬에서 찾는다. (프리팹으로 배치할 때는 씬 참조를 걸 수 없으므로)")]
        [SerializeField] private LinearStageGenerator stageGenerator;
        [SerializeField] private MinimapViewModel viewModel;

        protected override void Connect()
        {
            if (viewModel == null)
            {
                Debug.LogWarning($"{name} : MinimapViewModel이 지정되지 않았습니다.");
                return;
            }

            if (stageGenerator == null)
                stageGenerator = FindAnyObjectByType<LinearStageGenerator>();

            if (stageGenerator == null)
            {
                Debug.LogWarning($"{name} : LinearStageGenerator가 지정되지 않아 미니맵을 갱신하지 않습니다.");
                return;
            }

            viewModel.Connect(new StageMinimapSource(stageGenerator));
        }

        protected override void Disconnect()
        {
            if (viewModel != null)
                viewModel.Disconnect();
        }
    }
}
