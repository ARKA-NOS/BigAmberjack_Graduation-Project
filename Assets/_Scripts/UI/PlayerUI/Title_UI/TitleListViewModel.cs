using System.Collections.Generic;
using System.Linq;
using _Scripts.Agent.Players.QuestSystem;
using _Scripts.Agent.Players.TitleSystem;
using _Scripts.DataStructure;
using _Scripts.TitleCodex;
using _Scripts.UI.MVVM.Observables;
using _Scripts.UI.MVVM.ViewModels;
using UnityEngine;

namespace _Scripts.UI.PlayerUI.Title_UI
{
    // 칭호 도감 창의 ViewModel. 이번 런의 칭호(칭호 모듈)와 런을 넘는 달성 이력(기록 매니저)을 합쳐
    // 탭·검색어·맵 선택에 맞는 목록과 도감 달성 이력을 만든다. 모델이나 필터가 바뀌면 목록을 다시 만든다.
    [CreateAssetMenu(fileName = "Title List View Model", menuName = "UI/MVVM/Title List View Model")]
    public class TitleListViewModel : ViewModelBase<TitleBookModel>
    {
        public const string AllMapsLabel = "전체 맵";

        private static readonly string[] DefaultMapOptions = { AllMapsLabel };

        private readonly ObservableList<TitleViewData> _titles = new();
        private readonly Observable<TitleCompletionData> _completion = new(new TitleCompletionData(0, 0));
        private readonly Observable<TitleTab> _tab = new(TitleTab.Run);
        private readonly Observable<string> _search = new(string.Empty);
        private readonly Observable<int> _mapIndex = new(0);
        private readonly Observable<IReadOnlyList<string>> _mapOptions = new(DefaultMapOptions);
        private readonly List<TitleViewData> _buffer = new();

        private ITitleModule _run;
        private ITitleRecordManager _record;

        // 보여줄 칭호. 이번 런 탭은 진행도 높은 순, 나머지 탭은 도감 번호 순.
        public IReadOnlyObservableList<TitleViewData> Titles => _titles;
        public IReadOnlyObservable<TitleCompletionData> Completion => _completion;
        public IReadOnlyObservable<TitleTab> Tab => _tab;
        public IReadOnlyObservable<string> Search => _search;
        public IReadOnlyObservable<int> MapIndex => _mapIndex;
        // 맵 선택 항목. 0번은 "전체 맵".
        public IReadOnlyObservable<IReadOnlyList<string>> MapOptions => _mapOptions;

        public void SetTab(TitleTab tab)
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

        public void SetMap(int index)
        {
            index = Mathf.Clamp(index, 0, _mapOptions.Value.Count - 1);
            if (_mapIndex.Value == index) return;

            _mapIndex.Value = index;
            Refresh();
        }

        protected override void OnConnect(TitleBookModel model)
        {
            _run = model.Run;
            _record = model.Record;
            if (_run != null) _run.OnTitlesChanged += Refresh;
            if (_record != null) _record.OnRecordChanged += Refresh;

            _mapOptions.Value = BuildMapOptions(_record?.Catalog);
            _mapIndex.Value = Mathf.Clamp(_mapIndex.Value, 0, _mapOptions.Value.Count - 1);
            Refresh();
        }

        protected override void OnDisconnect()
        {
            if (_run != null) _run.OnTitlesChanged -= Refresh;
            if (_record != null) _record.OnRecordChanged -= Refresh;
            _run = null;
            _record = null;
        }

        protected override void ResetState()
        {
            _titles.Clear();
            _completion.Value = new TitleCompletionData(0, 0);
            _tab.Value = TitleTab.Run;
            _search.Value = string.Empty;
            _mapIndex.Value = 0;
            _mapOptions.Value = DefaultMapOptions;
        }

        private void Refresh()
        {
            TitleCatalogSo catalog = _record?.Catalog;
            _completion.Value = new TitleCompletionData(_record?.EarnedCount ?? 0, catalog != null ? catalog.Entries.Count : 0);

            _buffer.Clear();
            if (_tab.Value == TitleTab.Run) CollectRunTitles(catalog);
            else CollectCatalogTitles(catalog, _tab.Value == TitleTab.Earned);

            _titles.SetAll(_tab.Value == TitleTab.Run
                ? _buffer.OrderByDescending(viewData => viewData.Progress).ToList() // 진행도가 같으면 기존 순서 유지(안정 정렬)
                : _buffer.ToList());
        }

        // 이번 런: 진행 중인 맵의 칭호와 이번 런에 달성한 칭호. 퀘스트 진행을 함께 보여 준다.
        private void CollectRunTitles(TitleCatalogSo catalog)
        {
            if (_run == null) return;

            foreach (Title title in _run.Titles)
            {
                // 다른 스테이지의 칭호는 완료한 것만 보여준다.
                if (!title.IsActive && !title.IsComplete) continue;

                TitleCatalogEntry entry = default;
                if (catalog != null) catalog.TryGetEntry(title.TitleSo, out entry);

                TryAdd(CreateRunData(title, entry));
            }
        }

        // 달성한 칭호 / 전체 칭호: 도감 순서대로. 달성 기록이 없는 칭호는 잠근다(전체 탭).
        private void CollectCatalogTitles(TitleCatalogSo catalog, bool earnedOnly)
        {
            if (catalog == null) return;

            foreach (TitleCatalogEntry entry in catalog.Entries)
            {
                bool earned = _record != null && _record.HasEarned(entry.Title);
                if (earnedOnly && !earned) continue;

                AbstractTitleSo so = entry.Title;
                TryAdd(new TitleViewData(so.name, so.TitleName, so.TitleDescription, so.UpgradeDescription, so.TitleIcon,
                    new List<QuestViewData>(), entry.Number, entry.MapLabel, !earned,
                    earned ? TitleEntryStatus.Earned : TitleEntryStatus.NotEarned));
            }
        }

        private void TryAdd(TitleViewData data)
        {
            if (MatchesMap(data) && MatchesSearch(data)) _buffer.Add(data);
        }

        private bool MatchesMap(TitleViewData data)
        {
            if (_mapIndex.Value <= 0) return true;

            IReadOnlyList<string> options = _mapOptions.Value;
            return _mapIndex.Value < options.Count && data.MapLabel == options[_mapIndex.Value];
        }

        // 보이는 칭호는 번호·이름·조건 문구로, 잠긴 칭호는 번호로만 찾는다.
        private bool MatchesSearch(TitleViewData data)
        {
            string term = _search.Value.Trim();
            if (term.Length == 0) return true;

            string number = data.Number > 0 ? data.Number.ToString("000") : string.Empty;
            string searchable = data.IsLocked ? number : $"{number} {data.TitleName} {data.TitleDescription}";
            return searchable.IndexOf(term, System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static IReadOnlyList<string> BuildMapOptions(TitleCatalogSo catalog)
        {
            if (catalog == null) return DefaultMapOptions;

            var options = new List<string> { AllMapsLabel };
            foreach (string label in catalog.MapLabels)
            {
                if (!string.IsNullOrEmpty(label)) options.Add(label);
            }
            return options;
        }

        // 달성한 칭호는 퀘스트 목록이 비워지므로(Title.Clear) 퀘스트 없이 보여준다.
        private static TitleViewData CreateRunData(Title title, TitleCatalogEntry entry)
        {
            List<QuestViewData> quests = title.IsComplete
                ? new List<QuestViewData>()
                : title.GetQuests().Select(CreateQuestViewData).ToList();

            AbstractTitleSo so = title.TitleSo;
            return new TitleViewData(so.name, so.TitleName, so.TitleDescription, so.UpgradeDescription, so.TitleIcon,
                quests, entry.Number, entry.MapLabel, false,
                title.IsComplete ? TitleEntryStatus.RunCompleted : TitleEntryStatus.RunInProgress);
        }

        private static QuestViewData CreateQuestViewData(Quest quest)
            => new(quest.QuestSo.QuestName, quest.QuestSo.QuestDescription,
                new Gauge(quest.CurrentAmount, quest.RequiredAmount), quest.IsComplete);
    }
}
