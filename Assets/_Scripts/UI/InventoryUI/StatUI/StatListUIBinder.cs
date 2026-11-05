using System.Collections.Generic;
using Lrw.Script.Agent.StatSystem;
using Lrw.Script.UI.BindSystem;
using UnityEngine;

namespace KHO.Scripts.UI.StatUI
{
    // display에 적힌 스탯만, 적힌 순서대로 "현재값 (+변화량)" 형식으로 표시한다.
    public class StatListUIBinder : AbstractUIBinder<IReadOnlyDictionary<StatData, Stat>>
    {
        private const string IncreaseColor = "#7CE07C";
        private const string DecreaseColor = "#E07C7C";

        [SerializeField] private StatDisplaySO display;
        [SerializeField] private Transform rowParent;
        [SerializeField] private StatRowUI template;

        private readonly List<StatRowUI> _rows = new();

        protected override void Awake()
        {
            base.Awake();
            rowParent.GetComponentsInChildren(true, _rows);
        }

        private void Start()
        {
            RefreshRows();
        }

        // 스탯은 값이 바뀔 때만 다시 그리면 되므로 매 프레임 할 일은 없다.
        protected override void DataUpdate()
        {
        }

        protected override void OnDataChanged()
        {
            RefreshRows();
        }

        private void RefreshRows()
        {
            if (display == null) return;

            StatDisplaySO.Entry[] entries = display.Entries;
            while (_rows.Count < entries.Length && template != null)
                _rows.Add(Instantiate(template, rowParent));

            IReadOnlyDictionary<StatData, Stat> stats = CurrentValue;
            for (int i = 0; i < _rows.Count; i++)
            {
                bool active = i < entries.Length;
                _rows[i].gameObject.SetActive(active);
                if (active) _rows[i].Set(entries[i].DisplayName, FormatEntry(entries[i], stats));
            }
        }

        private static string FormatEntry(StatDisplaySO.Entry entry, IReadOnlyDictionary<StatData, Stat> stats)
        {
            if (entry.Stat == null || stats == null || !stats.TryGetValue(entry.Stat, out Stat stat))
                return "-";

            string value = StatDisplaySO.FormatValue(stat.Value, entry.Format);
            float delta = stat.ModifyValue;
            if (Mathf.Approximately(delta, 0f)) return value;

            string sign = delta > 0f ? "+" : "-";
            string color = delta > 0f ? IncreaseColor : DecreaseColor;
            string amount = StatDisplaySO.FormatValue(Mathf.Abs(delta), entry.Format == StatValueFormat.Multiplier ? StatValueFormat.Decimal : entry.Format);
            return $"{value} <color={color}>({sign}{amount})</color>";
        }
    }
}
