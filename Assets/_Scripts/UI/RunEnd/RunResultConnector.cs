using _Scripts._Core.SceneSystem;
using _Scripts.Currency;
using _Scripts.UI.MVVM.Connectors;
using UnityEngine;

namespace _Scripts.UI.RunEnd
{
    // 결과창 캔버스 프리팹에 붙어, 게임 쪽 기능(RunResultSource)을 RunResultViewModel에 연결한다.
    // 어떤 재화를 보여줄지, "다음"을 누르면 어느 씬으로 갈지는 여기서 정한다.
    public class RunResultConnector : ConnectorBase
    {
        [SerializeField] private RunResultViewModel viewModel;
        [Tooltip("소지금으로 표시할 재화")]
        [SerializeField] private CurrencySO gold;
        [Tooltip("\"다음\"을 누르면 이동할 씬. 비워두면 현재 씬을 다시 시작한다.")]
        [SerializeField] private SceneSo nextScene;

        protected override void Connect()
        {
            if (viewModel == null)
            {
                Debug.LogWarning($"[{name}] RunResultViewModel이 지정되지 않았습니다.", this);
                return;
            }

            viewModel.Connect(new RunResultSource(gold, nextScene));
        }

        protected override void Disconnect()
        {
            if (viewModel != null)
                viewModel.Disconnect();
        }
    }
}
