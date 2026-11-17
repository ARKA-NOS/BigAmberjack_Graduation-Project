using System.Collections.Generic;
using _Scripts.Agent.StatSystem;
using _Scripts.UI.MVVM.Observables;
using _Scripts.UI.MVVM.ViewModels;
using UnityEngine;

namespace _Scripts.UI.InventoryUI.StatUI
{
    // 플레이어 스탯 ViewModel. View가 GetStat으로 보여줄 스탯을 요청하면 그 스탯의 값 변경을 구독해 알려 준다.
    // 어떤 스탯을 어떤 순서·형식으로 보여줄지는 View(StatDisplaySO)가 정하므로, 표시 목록이 다른 View들이 에셋 하나를 같이 쓴다.
    [CreateAssetMenu(fileName = "Stat List View Model", menuName = "UI/MVVM/Stat List View Model")]
    public class StatListViewModel : ViewModelBase<IStatModule>
    {
        private readonly Dictionary<StatData, StatEntry> _entries = new();
        private readonly List<KeyValuePair<StatData, StatEntry>> _buffer = new();
        private IStatModule _model;

        private sealed class StatEntry
        {
            public readonly Observable<StatValueData> Value = new(StatValueData.None);
            public Stat Stat;
            public Stat.StatValueChanged Handler;
        }

        // 스탯 하나의 표시 값. 연결 전에도 받을 수 있고(None), 연결되면 값이 채워진다.
        public IReadOnlyObservable<StatValueData> GetStat(StatData data)
        {
            if (data == null) return null;

            if (!_entries.TryGetValue(data, out StatEntry entry))
            {
                entry = new StatEntry();
                _entries.Add(data, entry);
                if (_model != null) Attach(data, entry);
            }

            return entry.Value;
        }

        protected override void OnConnect(IStatModule model)
        {
            _model = model;

            // 값 알림을 받은 View가 GetStat으로 새 스탯을 요청해도 순회가 깨지지 않게 복사본을 돈다.
            _buffer.Clear();
            _buffer.AddRange(_entries);
            foreach (KeyValuePair<StatData, StatEntry> pair in _buffer)
                Attach(pair.Key, pair.Value);
        }

        protected override void OnDisconnect()
        {
            foreach (StatEntry entry in _entries.Values)
                Detach(entry);

            _model = null;
        }

        protected override void ResetState()
        {
            foreach (StatEntry entry in _entries.Values)
                entry.Value.Value = StatValueData.None;
        }

        private void Attach(StatData data, StatEntry entry)
        {
            if (!_model.TryGetStat(data, out Stat stat))
            {
                entry.Value.Value = StatValueData.None;
                return;
            }

            entry.Stat = stat;
            entry.Handler = (newValue, delta) => entry.Value.Value = StatValueData.From(stat);
            stat.OnValueChanged += entry.Handler;
            entry.Value.Value = StatValueData.From(stat);
        }

        private static void Detach(StatEntry entry)
        {
            if (entry.Stat != null)
                entry.Stat.OnValueChanged -= entry.Handler;

            entry.Stat = null;
            entry.Handler = null;
        }
    }
}
