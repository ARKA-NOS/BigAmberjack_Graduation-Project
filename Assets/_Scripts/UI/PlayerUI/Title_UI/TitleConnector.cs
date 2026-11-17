using _Scripts._Core._ServiceLocator;
using _Scripts.Agent.Players.TitleSystem;
using _Scripts.DevLib.ModuleSystem;
using _Scripts.TitleCodex;
using _Scripts.UI.MVVM.Connectors;
using UnityEngine;

namespace _Scripts.UI.PlayerUI.Title_UI
{
    // 플레이어 프리팹에 붙어 칭호 모듈(이번 런)과 칭호 달성 이력(전역 매니저)을 칭호 도감 ViewModel에 연결한다.
    // 칭호 모듈·기록 매니저는 칭호 창을 모르고, 이 컴포넌트만 둘을 함께 안다. 칭호 모듈이 없는 플레이어면 연결하지 않는다.
    public class TitleConnector : ConnectorBase
    {
        [SerializeField] private TitleListViewModel viewModel;

        private ITitleRecordManager _record;

        // GameManager는 런이 끝날 때 다시 만들어지는데 플레이어는 남을 수 있어서, 기록 매니저가 바뀌면 다시 연결한다.
        private void Update()
        {
            if (_record != null && !ReferenceEquals(_record, ServiceLocator.Get<ITitleRecordManager>()))
                Reconnect();
        }

        protected override void Connect()
        {
            if (viewModel == null) return;

            ModuleOwner owner = GetComponentInParent<ModuleOwner>();
            if (owner == null || !owner.TryGetModule(out ITitleModule titles)) return;

            _record = ServiceLocator.Get<ITitleRecordManager>();
            viewModel.Connect(new TitleBookModel(titles, _record));
        }

        protected override void Disconnect()
        {
            _record = null;
            if (viewModel != null)
                viewModel.Disconnect();
        }
    }
}
