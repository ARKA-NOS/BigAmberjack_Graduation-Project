using _Scripts.UI.MVVM.Observables;
using _Scripts.UI.MVVM.ViewModels;
using UnityEngine;

namespace _Scripts.UI.AlarmUI
{
    // 알람(퀘스트 완료 등) ViewModel. 알람이 오면 목록 끝에 넣고, View가 표시를 마치고 Dismiss하면 뺀다.
    // 얼마나 보여주고 언제 사라질지는 View(AlarmListView)가 정한다.
    [CreateAssetMenu(fileName = "Alarm View Model", menuName = "UI/MVVM/Alarm View Model")]
    public class AlarmViewModel : ViewModelBase<IAlarmSource>
    {
        private readonly ObservableList<AlarmItem> _alarms = new();
        private IAlarmSource _source;

        // 지금 화면에 떠 있는 알람. 먼저 온 것이 앞.
        public IReadOnlyObservableList<AlarmItem> Alarms => _alarms;

        protected override void OnConnect(IAlarmSource source)
        {
            _source = source;
            _source.AlarmReceived += HandleAlarmReceived;
        }

        protected override void OnDisconnect()
        {
            _source.AlarmReceived -= HandleAlarmReceived;
            _source = null;
        }

        protected override void ResetState()
        {
            _alarms.Clear();
        }

        private void HandleAlarmReceived(AlarmData data)
        {
            _alarms.Add(new AlarmItem(data, Dismiss));
        }

        // 이미 빠진 알람이면 아무 일도 하지 않는다.
        private void Dismiss(AlarmItem item)
        {
            _alarms.Remove(item);
        }
    }
}
