using System;
using System.Collections.Generic;
using _Scripts.Agent.StatSystem;
using UnityEngine;

namespace _Scripts.UI.InventoryUI.StatUI
{
    public enum StatValueFormat
    {
        Integer = 0,    // 12
        Decimal = 1,    // 1.25
        Percent = 2,    // 0.2 → 20%
        Multiplier = 3, // 2 → ×2
    }

    // 화면에 보여줄 스탯 목록(순서, 한글 이름, 값 형식). Lrw의 StatData는 건드리지 않고 표시만 여기서 정한다.
    [CreateAssetMenu(fileName = "Stat Display", menuName = "KHO/UI/Stat Display")]
    public class StatDisplaySO : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            [field: SerializeField, Tooltip("비워 두면 값 자리에 '-'를 표시한다(아직 없는 스탯용).")]
            public StatData Stat { get; private set; }
            [field: SerializeField] public string DisplayName { get; private set; }
            [field: SerializeField] public StatValueFormat Format { get; private set; }
        }

        [field: SerializeField] public Entry[] Entries { get; private set; } = new Entry[0];

        public static string FormatValue(float value, StatValueFormat format) => format switch
        {
            StatValueFormat.Integer => Mathf.RoundToInt(value).ToString(),
            StatValueFormat.Decimal => value.ToString("0.##"),
            StatValueFormat.Percent => $"{value * 100f:0.#}%",
            StatValueFormat.Multiplier => $"×{value:0.##}",
            _ => value.ToString(),
        };

        public IEnumerable<StatData> Stats()
        {
            foreach (Entry entry in Entries)
            {
                if (entry.Stat != null) yield return entry.Stat;
            }
        }
    }
}
