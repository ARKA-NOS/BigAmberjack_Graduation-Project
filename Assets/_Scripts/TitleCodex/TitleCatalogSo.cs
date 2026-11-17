using System;
using System.Collections.Generic;
using _Scripts.Agent.Players.TitleSystem;
using UnityEngine;

namespace _Scripts.TitleCodex
{
    // 도감에 싣는 칭호의 순서와 맵 이름. 그룹을 적은 순서대로 번호(No. 001…)를 매긴다.
    [CreateAssetMenu(fileName = "Title Catalog", menuName = "Title/Title Catalog", order = 1)]
    public class TitleCatalogSo : ScriptableObject
    {
        [Serializable]
        public class Group
        {
            [field: SerializeField] public TitleGroupSo Titles { get; private set; }
            [field: SerializeField, Tooltip("도감에 표시할 맵 이름(예: 1번 맵)")] public string MapLabel { get; private set; }
        }

        [field: SerializeField] public Group[] Groups { get; private set; } = Array.Empty<Group>();

        private List<TitleCatalogEntry> _entries;
        private Dictionary<AbstractTitleSo, TitleCatalogEntry> _lookup;
        private List<string> _mapLabels;

        public IReadOnlyList<TitleCatalogEntry> Entries { get { Build(); return _entries; } }

        // 맵 이름 목록(카탈로그 순서, 중복 없음).
        public IReadOnlyList<string> MapLabels { get { Build(); return _mapLabels; } }

        public bool TryGetEntry(AbstractTitleSo title, out TitleCatalogEntry entry)
        {
            Build();
            if (title != null && _lookup.TryGetValue(title, out entry)) return true;

            entry = default;
            return false;
        }

        private void OnValidate() => _entries = null;

        private void Build()
        {
            if (_entries != null) return;

            _entries = new List<TitleCatalogEntry>();
            _lookup = new Dictionary<AbstractTitleSo, TitleCatalogEntry>();
            _mapLabels = new List<string>();

            foreach (Group group in Groups)
            {
                if (group?.Titles == null || group.Titles.TitleList == null) continue;
                if (!_mapLabels.Contains(group.MapLabel)) _mapLabels.Add(group.MapLabel);

                foreach (AbstractTitleSo title in group.Titles.TitleList)
                {
                    if (title == null || _lookup.ContainsKey(title)) continue;

                    var entry = new TitleCatalogEntry(title, _entries.Count + 1, group.MapLabel);
                    _entries.Add(entry);
                    _lookup.Add(title, entry);
                }
            }
        }
    }

    public readonly struct TitleCatalogEntry
    {
        public readonly AbstractTitleSo Title;
        public readonly int Number;
        public readonly string MapLabel;

        public TitleCatalogEntry(AbstractTitleSo title, int number, string mapLabel)
        {
            Title = title;
            Number = number;
            MapLabel = mapLabel;
        }
    }
}
