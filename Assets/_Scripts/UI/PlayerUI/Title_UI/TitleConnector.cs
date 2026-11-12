using _Scripts.Agent.Players.TitleSystem;
using _Scripts.DevLib.ModuleSystem;
using _Scripts.UI.MVVM.Connectors;
using UnityEngine;

namespace _Scripts.UI.PlayerUI.Title_UI
{
    // 플레이어 프리팹에 붙어 칭호 모듈을 칭호 창 ViewModel에 연결한다.
    // 칭호 모듈은 칭호 창을 모르고, 이 컴포넌트만 둘을 함께 안다. 칭호 모듈이 없는 플레이어면 연결하지 않는다.
    public class TitleConnector : ConnectorBase
    {
        [SerializeField] private TitleListViewModel viewModel;

        protected override void Connect()
        {
            if (viewModel == null) return;

            ModuleOwner owner = GetComponentInParent<ModuleOwner>();
            if (owner != null && owner.TryGetModule(out ITitleModule titles))
                viewModel.Connect(titles);
        }

        protected override void Disconnect()
        {
            if (viewModel != null)
                viewModel.Disconnect();
        }
    }
}
