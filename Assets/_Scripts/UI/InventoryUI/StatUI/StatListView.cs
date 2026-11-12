using System.Collections.Generic;
using _Scripts.UI.MVVM.Disposables;
using _Scripts.UI.MVVM.Views;
using UnityEngine;

namespace _Scripts.UI.InventoryUI.StatUI
{
    // display에 적힌 스탯만, 적힌 순서대로 "현재값 (+변화량)" 형식으로 표시한다.
    // 줄마다 그 스탯의 값만 구독해서, 스탯 하나가 바뀌면 그 줄만 다시 그린다.
    public class StatListView : ViewBase<StatListViewModel>
    {
        private const string IncreaseColor = "#7CE07C";
        private const string DecreaseColor = "#E07C7C";

        [SerializeField] private StatDisplaySO display;
        [SerializeField] private Transform rowParent;
        [SerializeField] private StatRowUI template;

        private readonly List<StatRowUI> _rows = new();

        private void Awake()
        {
            rowParent.GetComponentsInChildren(true, _rows);
        }

        protected override void OnBind(StatListViewModel vm)
        {
            if (display == null) return;

            StatDisplaySO.Entry[] entries = display.Entries;
            while (_rows.Count < entries.Length && template != null)
                _rows.Add(Instantiate(template, rowParent));

            for (int i = 0; i < _rows.Count; i++)
            {
                bool active = i < entries.Length;
                _rows[i].gameObject.SetActive(active);
                if (!active) continue;

                StatRowUI row = _rows[i];
                StatDisplaySO.Entry entry = entries[i];

                // 아직 없는 스탯용으로 비워 둔 줄은 값 자리에 '-'를 표시한다.
                if (entry.Stat == null)
                {
                    row.Set(entry.DisplayName, "-");
                    continue;
                }

                vm.GetStat(entry.Stat)
                    .Subscribe(value => row.Set(entry.DisplayName, FormatEntry(entry, value)))
                    .AddTo(Disposables);
            }
        }

        private static string FormatEntry(StatDisplaySO.Entry entry, StatValueData stat)
        {
            if (!stat.HasValue) return "-";

            string value = StatDisplaySO.FormatValue(stat.Value, entry.Format);
            float delta = stat.Modify;
            if (Mathf.Approximately(delta, 0f)) return value;

            string sign = delta > 0f ? "+" : "-";
            string color = delta > 0f ? IncreaseColor : DecreaseColor;
            string amount = StatDisplaySO.FormatValue(Mathf.Abs(delta), entry.Format == StatValueFormat.Multiplier ? StatValueFormat.Decimal : entry.Format);
            return $"{value} <color={color}>({sign}{amount})</color>";
        }
    }
}
