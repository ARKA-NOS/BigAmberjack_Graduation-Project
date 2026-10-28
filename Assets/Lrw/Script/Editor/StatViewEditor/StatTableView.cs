using System;
using System.Collections.Generic;
using Lrw.Script.Agent.StatSystem;
using UnityEngine;
using UnityEngine.UIElements;

namespace Lrw.Script.Editor.StatViewEditor
{
    public class StatTableView : VisualElement
    {
        private const float IconSize = 16f;
        private static readonly Color IncreaseColor = new(0.4f, 0.8f, 0.4f);
        private static readonly Color DecreaseColor = new(0.9f, 0.4f, 0.4f);

        private readonly MultiColumnListView _listView;
        private readonly List<KeyValuePair<StatData, Stat>> _items = new();
        private IReadOnlyDictionary<StatData, Stat> _source;

        public StatTableView()
        {
            style.flexGrow = 1;

            _listView = new MultiColumnListView
            {
                itemsSource = _items,
                selectionType = SelectionType.None,
                reorderable = false,
                fixedItemHeight = 20,
                showAlternatingRowBackgrounds = AlternatingRowBackground.ContentOnly
            };
            _listView.style.flexGrow = 1;

            AddNameColumn();
            AddTextColumn("Base", 70, (label, _, stat) => label.text = stat.BaseValue.ToString("0.00"));
            AddTextColumn("Modify", 120, BindModify);
            AddTextColumn("Value", 70, (label, _, stat) => label.text = stat.Value.ToString("0.00"));
            AddTextColumn("Range", 90, (label, data, _) => label.text = data.UseValueRange
                ? $"{data.ValueRange.x:0.##} ~ {data.ValueRange.y:0.##}"
                : "-");

            Add(_listView);
        }

        public void SetStats(IReadOnlyDictionary<StatData, Stat> stats)
        {
            // GetStat()으로 스탯이 나중에 추가될 수 있어서 개수가 바뀌면 목록을 다시 만든다
            if (!ReferenceEquals(stats, _source) || (stats?.Count ?? 0) != _items.Count)
            {
                _source = stats;
                _items.Clear();
                if (stats != null) _items.AddRange(stats);
                _listView.Rebuild();
                return;
            }

            _listView.RefreshItems();
        }

        private void AddNameColumn()
        {
            _listView.columns.Add(new Column
            {
                title = "Stat",
                width = 160,
                stretchable = true,
                makeCell = CreateNameCell,
                bindCell = (element, index) => BindName(element, _items[index].Key)
            });
        }

        private void AddTextColumn(string title, float width, Action<Label, StatData, Stat> bind)
        {
            _listView.columns.Add(new Column
            {
                title = title,
                width = width,
                stretchable = true,
                makeCell = CreateValueCell,
                bindCell = (element, index) => bind((Label)element, _items[index].Key, _items[index].Value)
            });
        }

        private static VisualElement CreateNameCell()
        {
            VisualElement cell = new VisualElement();
            cell.style.flexDirection = FlexDirection.Row;
            cell.style.alignItems = Align.Center;

            Image icon = new Image();
            icon.style.width = IconSize;
            icon.style.height = IconSize;
            icon.style.marginRight = 4;

            cell.Add(icon);
            cell.Add(new Label());
            return cell;
        }

        private static Label CreateValueCell()
        {
            Label label = new Label();
            label.style.unityTextAlign = TextAnchor.MiddleRight;
            label.style.paddingRight = 4;
            return label;
        }

        private static void BindName(VisualElement cell, StatData data)
        {
            Image icon = cell.Q<Image>();
            icon.sprite = data.StatIcon;
            icon.style.display = data.StatIcon != null ? DisplayStyle.Flex : DisplayStyle.None;

            cell.Q<Label>().text = string.IsNullOrEmpty(data.StatName) ? data.name : data.StatName;
            cell.tooltip = data.StatDescription;
        }

        private static void BindModify(Label label, StatData data, Stat stat)
        {
            float modify = stat.ModifyValue;

            // Stat.PercentChange는 BaseValue가 0이면 에러 로그를 남기므로 여기서 직접 계산한다
            string percent = Mathf.Approximately(stat.BaseValue, 0f)
                ? "-"
                : $"{modify / stat.BaseValue * 100f:+0.#;-0.#;0}%";

            label.text = $"{modify:+0.00;-0.00;0.00} ({percent})";

            if (Mathf.Approximately(modify, 0f)) label.style.color = StyleKeyword.Null;
            else label.style.color = modify > 0f ? IncreaseColor : DecreaseColor;
        }
    }
}
