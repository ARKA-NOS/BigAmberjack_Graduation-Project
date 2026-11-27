using System;
using System.Collections.Generic;
using _Scripts.Agent.Players.TitleSystem;
using _Scripts.TitleCodex;
using _Scripts.UI.MVVM.Observables;
using _Scripts.UI.MVVM.ViewModels;

namespace _Scripts.UI.PlayerUI.Title_UI.Equip
{
    // 칭호 장착 창의 ViewModel. 도감의 전체 칭호를 이번 런 달성(장착 가능), 이전 런 기록만, 미획득 세 상태로 나눠
    // 필터·검색에 맞는 목록과, 선택한 칭호의 상세(장착/해제 동작 포함)를 만든다.
    // 인스펙터 설정이 없어서 에셋 없이 창이 CreateRuntime<TitleEquipViewModel>()로 만든다.
    public class TitleEquipViewModel : ViewModelBase<TitleEquipModel>
    {
        public const string LockedText = "???";
        private const string NotUnlockedLabel = "해금하지 않음";

        private readonly struct Entry
        {
            public readonly AbstractTitleSo Title;
            public readonly int Number;
            public readonly string MapLabel;
            public readonly TitleEquipState State;
            public readonly bool IsEquipped;

            public Entry(AbstractTitleSo title, int number, string mapLabel, TitleEquipState state, bool isEquipped)
            {
                Title = title;
                Number = number;
                MapLabel = mapLabel;
                State = state;
                IsEquipped = isEquipped;
            }

            public string Key => Title.name;
            public bool IsKnown => State != TitleEquipState.Locked;
            public string NumberText => Number > 0 ? Number.ToString("000") : string.Empty;
            public string DisplayName => IsKnown ? Title.TitleName : LockedText;
        }

        private readonly ObservableList<TitleEquipRowData> _rows = new();
        private readonly Observable<TitleEquipDetailData> _detail = new();
        private readonly Observable<TitleEquipTab> _tab = new(TitleEquipTab.All);
        private readonly Observable<string> _search = new(string.Empty);
        private readonly Observable<TitleEquipTabCounts> _counts = new();
        private readonly Observable<TitleEquipSummary> _summary = new();

        private readonly List<Entry> _entries = new();
        private readonly List<TitleEquipRowData> _buffer = new();
        private readonly Dictionary<AbstractTitleSo, Title> _runTitles = new();

        private ITitleModule _run;
        private ITitleEquipModule _equip;
        private ITitleRecordManager _record;
        private string _selectedKey;

        // 필터와 검색에 맞는 칭호 목록(도감 번호 순).
        public IReadOnlyObservableList<TitleEquipRowData> Rows => _rows;
        // 선택한 칭호. 연결되지 않았으면 null.
        public IReadOnlyObservable<TitleEquipDetailData> Detail => _detail;
        public IReadOnlyObservable<TitleEquipTab> Tab => _tab;
        public IReadOnlyObservable<string> Search => _search;
        public IReadOnlyObservable<TitleEquipTabCounts> Counts => _counts;
        public IReadOnlyObservable<TitleEquipSummary> Summary => _summary;

        public void SetTab(TitleEquipTab tab)
        {
            if (_tab.Value == tab) return;

            _tab.Value = tab;
            Refresh();
        }

        public void SetSearch(string text)
        {
            text ??= string.Empty;
            if (_search.Value == text) return;

            _search.Value = text;
            Refresh();
        }

        public void Select(string key)
        {
            if (_selectedKey == key) return;

            _selectedKey = key;
            Refresh();
        }

        protected override void OnConnect(TitleEquipModel model)
        {
            _run = model.Run;
            _equip = model.Equip;
            _record = model.Record;
            if (_run != null) _run.OnTitlesChanged += Refresh;
            if (_equip != null) _equip.OnEquipChanged += Refresh;
            if (_record != null) _record.OnRecordChanged += Refresh;
            Refresh();
        }

        protected override void OnDisconnect()
        {
            if (_run != null) _run.OnTitlesChanged -= Refresh;
            if (_equip != null) _equip.OnEquipChanged -= Refresh;
            if (_record != null) _record.OnRecordChanged -= Refresh;
            _run = null;
            _equip = null;
            _record = null;
        }

        protected override void ResetState()
        {
            _entries.Clear();
            _buffer.Clear();
            _runTitles.Clear();
            _selectedKey = null;
            _rows.Clear();
            _detail.Value = null;
            _tab.Value = TitleEquipTab.All;
            _search.Value = string.Empty;
            _counts.Value = default;
            _summary.Value = default;
        }

        private void Refresh()
        {
            BuildEntries();
            EnsureSelection();

            _counts.Value = CountTabs();
            _summary.Value = CountSummary();

            _buffer.Clear();
            foreach (Entry entry in _entries)
            {
                if (MatchesTab(entry) && MatchesSearch(entry))
                    _buffer.Add(CreateRow(entry));
            }
            _rows.SetAll(_buffer);

            _detail.Value = TryFindSelected(out Entry selected) ? CreateDetail(selected) : null;
        }

        // 도감 순서의 전체 칭호 + 도감에 없는 이번 런 칭호.
        private void BuildEntries()
        {
            _entries.Clear();
            _runTitles.Clear();
            if (_run != null)
            {
                foreach (Title title in _run.Titles)
                    _runTitles[title.TitleSo] = title;
            }

            TitleCatalogSo catalog = _record?.Catalog;
            if (catalog != null)
            {
                foreach (TitleCatalogEntry entry in catalog.Entries)
                    _entries.Add(CreateEntry(entry.Title, entry.Number, entry.MapLabel));
            }

            foreach (AbstractTitleSo title in _runTitles.Keys)
            {
                if (catalog != null && catalog.TryGetEntry(title, out _)) continue;
                _entries.Add(CreateEntry(title, 0, string.Empty));
            }
        }

        private Entry CreateEntry(AbstractTitleSo title, int number, string mapLabel)
        {
            TitleEquipState state;
            if (_runTitles.TryGetValue(title, out Title runTitle) && runTitle.IsComplete) state = TitleEquipState.Owned;
            else if (_record != null && _record.HasEarned(title)) state = TitleEquipState.Recorded;
            else state = TitleEquipState.Locked;

            return new Entry(title, number, mapLabel, state, state == TitleEquipState.Owned && _equip.IsEquipped(title));
        }

        // 선택한 칭호가 없거나 사라졌으면 첫 칭호를 고른다.
        private void EnsureSelection()
        {
            if (_entries.Count == 0)
            {
                _selectedKey = null;
                return;
            }

            if (!TryFindSelected(out _))
                _selectedKey = _entries[0].Key;
        }

        private bool TryFindSelected(out Entry selected)
        {
            foreach (Entry entry in _entries)
            {
                if (entry.Key != _selectedKey) continue;

                selected = entry;
                return true;
            }

            selected = default;
            return false;
        }

        private TitleEquipTabCounts CountTabs()
        {
            int equipped = 0, held = 0, missing = 0;
            foreach (Entry entry in _entries)
            {
                if (entry.State != TitleEquipState.Owned) missing++;
                else if (entry.IsEquipped) equipped++;
                else held++;
            }

            return new TitleEquipTabCounts(_entries.Count, equipped, held, missing);
        }

        private TitleEquipSummary CountSummary()
        {
            int owned = 0, recorded = 0;
            foreach (Entry entry in _entries)
            {
                if (entry.State == TitleEquipState.Owned) owned++;
                else if (entry.State == TitleEquipState.Recorded) recorded++;
            }

            return new TitleEquipSummary(owned, recorded, _entries.Count);
        }

        private bool MatchesTab(Entry entry) => _tab.Value switch
        {
            TitleEquipTab.Equipped => entry.IsEquipped,
            TitleEquipTab.Held => entry.State == TitleEquipState.Owned && !entry.IsEquipped,
            TitleEquipTab.Missing => entry.State != TitleEquipState.Owned,
            _ => true,
        };

        // 공개된 칭호는 번호·이름·조건 문구로, 잠긴 칭호는 번호로만 찾는다(도감 검색과 같은 규칙).
        private bool MatchesSearch(Entry entry)
        {
            string term = _search.Value.Trim();
            if (term.Length == 0) return true;

            string searchable = entry.IsKnown
                ? $"{entry.NumberText} {entry.Title.TitleName} {entry.Title.TitleDescription}"
                : entry.NumberText;
            return searchable.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private TitleEquipRowData CreateRow(Entry entry)
        {
            string key = entry.Key;
            return new TitleEquipRowData(key, entry.NumberText, entry.DisplayName, entry.State, entry.IsEquipped,
                key == _selectedKey, () => Select(key));
        }

        private TitleEquipDetailData CreateDetail(Entry entry)
        {
            AbstractTitleSo title = entry.Title;
            bool owned = entry.State == TitleEquipState.Owned;
            // 해제는 무료. 장착은 비용을 낼 수 있어야 한다.
            bool canToggle = owned && (entry.IsEquipped || _equip.CanPayEquipCost(title));

            string buttonLabel = !owned ? NotUnlockedLabel : entry.IsEquipped ? "해제" : CreateEquipLabel(title);
            Action onToggle = null;
            if (canToggle)
            {
                ITitleEquipModule equip = _equip;
                onToggle = () => equip.TrySetEquipped(title, !equip.IsEquipped(title));
            }

            return new TitleEquipDetailData(entry.Key, entry.DisplayName, CreateMeta(entry),
                entry.IsKnown ? title.TitleDescription : LockedText,
                entry.IsKnown ? title.UpgradeDescription : LockedText,
                entry.IsKnown ? title.TitleIcon : null,
                entry.State, entry.IsEquipped, buttonLabel, canToggle, onToggle);
        }

        // 최초 장착은 "장착 (무료)", 이후는 "장착 (30 코인)". 코인이 모자라면 "코인 부족 (30 코인)".
        private string CreateEquipLabel(AbstractTitleSo title)
        {
            int cost = _equip.GetEquipCost(title);
            if (cost <= 0) return "장착 (무료)";
            return _equip.CanPayEquipCost(title) ? $"장착 ({cost} 코인)" : $"코인 부족 ({cost} 코인)";
        }

        // "No. 001 · 1번 맵". 잠긴 칭호는 맵도 숨기고 번호만 보여 준다.
        private static string CreateMeta(Entry entry)
        {
            string number = entry.Number > 0 ? $"No. {entry.NumberText}" : string.Empty;
            if (!entry.IsKnown || string.IsNullOrEmpty(entry.MapLabel)) return number;
            return number.Length > 0 ? $"{number} · {entry.MapLabel}" : entry.MapLabel;
        }
    }
}
