using _Scripts.UI.MVVM.Observables;
using _Scripts.UI.MVVM.ViewModels;
using UnityEngine;

namespace _Scripts.UI.RunEnd
{
    // 탐험 종료 결과창의 ViewModel. 탐험이 끝나면 플레이어 입력을 끄고 결과(RunResultData)를 Result에 넣는다.
    // "다음" 버튼은 RunResultData.OnNext(Action)로 받아 결과창을 닫고 다음 씬으로 넘어간다.
    [CreateAssetMenu(fileName = "Run Result View Model", menuName = "UI/MVVM/Run Result View Model")]
    public class RunResultViewModel : ViewModelBase<IRunResultSource>
    {
        private readonly Observable<RunResultData> _result = new();
        private IRunResultSource _source;
        private bool _isEnded;
        private bool _isLeaving;

        // 보여줄 결과. null이면 결과창을 닫는다.
        public IReadOnlyObservable<RunResultData> Result => _result;

        protected override void OnConnect(IRunResultSource source)
        {
            _source = source;
            _source.RunEnded += HandleRunEnded;
        }

        protected override void OnDisconnect()
        {
            _source.RunEnded -= HandleRunEnded;
            _source = null;
        }

        protected override void ResetState()
        {
            _result.Value = null;
            _isEnded = false;
            _isLeaving = false;
        }

        // 보스 클리어와 사망이 겹쳐도 결과창은 한 번만 연다.
        private void HandleRunEnded(RunEndReason reason)
        {
            if (_isEnded) return;

            _isEnded = true;
            _source.LockPlayerInput();

            _result.Value = new RunResultData(
                reason,
                _source.Gold,
                _source.GoldIcon,
                _source.GetItemIcons(),
                _source.KillCount,
                () => Next(reason));
        }

        // 여러 번 눌러도 한 번만 넘어간다.
        private void Next(RunEndReason reason)
        {
            if (_isLeaving || _source == null) return;

            _isLeaving = true;
            IRunResultSource source = _source;
            _result.Value = null;
            source.GoNext(reason);
        }
    }
}
