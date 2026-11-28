using System.Collections.Generic;
using _Scripts.UI.MVVM.Disposables;
using _Scripts.UI.MVVM.Observables;
using _Scripts.UI.MVVM.Views;
using UnityEngine;

namespace _Scripts.UI.AlarmUI
{
    // 알람 목록을 화면에 하나씩 띄운다. AlarmUI를 이 오브젝트 아래에 만들고, 목록에서 빠지면 지우고 다음 알람을 띄운다.
    // 알람은 showTime 동안 보였다가 fadeDuration 동안 흐려지고, 다 사라지면 Dismiss로 ViewModel에 알린다.
    public class AlarmListView : ViewBase<AlarmViewModel>
    {
        [SerializeField] private AlarmUI alarmUI;

        [Header("Fade Setting")]
        [SerializeField] private float showTime = 3f;
        [SerializeField] private float fadeDuration = 2f;

        // 지금 떠 있는 알람. 나머지는 ViewModel의 Alarms에서 차례를 기다린다.
        private AlarmItem _shownAlarm;
        private AlarmUI _toast;

        protected override void OnBind(AlarmViewModel vm)
        {
            vm.Alarms.Subscribe(_ => ShowFirst(vm.Alarms)).AddTo(Disposables);
        }

        // 꺼질 때 떠 있던 알람은 지운다. 다시 켜지면 아직 남은 알람을 처음부터 다시 보여준다.
        protected override void OnUnbind()
        {
            ClearToast();
        }

        // 목록의 맨 앞 알람만 띄운다. 뒤에 추가되기만 했으면 떠 있는 알람은 그대로 둔다.
        // 알람은 띄울 때부터 시간을 재므로, 기다린 알람도 showTime을 온전히 보여준다.
        private void ShowFirst(IReadOnlyList<AlarmItem> alarms)
        {
            AlarmItem first = alarms.Count > 0 ? alarms[0] : null;
            if (first == _shownAlarm) return;

            ClearToast();
            if (first == null) return;

            _shownAlarm = first;
            _toast = Instantiate(alarmUI, transform);
            _toast.Init(first, showTime, fadeDuration);
        }

        private void ClearToast()
        {
            if (_toast != null)
                Destroy(_toast.gameObject);

            _toast = null;
            _shownAlarm = null;
        }
    }
}
