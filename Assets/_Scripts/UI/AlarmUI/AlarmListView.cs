using System.Collections.Generic;
using _Scripts.UI.MVVM.Disposables;
using _Scripts.UI.MVVM.Observables;
using _Scripts.UI.MVVM.Views;
using UnityEngine;

namespace _Scripts.UI.AlarmUI
{
    // 알람 목록을 화면에 띄운다. 알람마다 AlarmUI를 이 오브젝트 아래에 만들고, 목록에서 빠지면 지운다.
    // 알람은 showTime 동안 보였다가 fadeDuration 동안 흐려지고, 다 사라지면 Dismiss로 ViewModel에 알린다.
    public class AlarmListView : ViewBase<AlarmViewModel>
    {
        [SerializeField] private AlarmUI alarmUI;

        [Header("Fade Setting")]
        [SerializeField] private float showTime = 3f;
        [SerializeField] private float fadeDuration = 2f;

        // ViewModel의 Alarms와 같은 순서.
        private readonly List<AlarmUI> _toasts = new();

        protected override void OnBind(AlarmViewModel vm)
        {
            vm.Alarms.Subscribe(change => HandleAlarmsChanged(vm.Alarms, change)).AddTo(Disposables);
        }

        // 꺼질 때 떠 있던 알람은 지운다. 다시 켜지면 아직 남은 알람을 처음부터 다시 보여준다.
        protected override void OnUnbind()
        {
            ClearToasts();
        }

        private void HandleAlarmsChanged(IReadOnlyList<AlarmItem> alarms, ListChange<AlarmItem> change)
        {
            switch (change.Kind)
            {
                case ListChangeKind.Add:
                    _toasts.Insert(change.Index, CreateToast(change.NewItem));
                    break;

                case ListChangeKind.Remove:
                    RemoveToast(change.Index);
                    break;

                default:
                    ClearToasts();
                    foreach (AlarmItem alarm in alarms)
                        _toasts.Add(CreateToast(alarm));
                    break;
            }
        }

        private AlarmUI CreateToast(AlarmItem alarm)
        {
            AlarmUI toast = Instantiate(alarmUI, transform);
            toast.Init(alarm, showTime, fadeDuration);
            return toast;
        }

        private void RemoveToast(int index)
        {
            if (index < 0 || index >= _toasts.Count) return;

            if (_toasts[index] != null)
                Destroy(_toasts[index].gameObject);

            _toasts.RemoveAt(index);
        }

        private void ClearToasts()
        {
            foreach (AlarmUI toast in _toasts)
            {
                if (toast != null)
                    Destroy(toast.gameObject);
            }

            _toasts.Clear();
        }
    }
}
