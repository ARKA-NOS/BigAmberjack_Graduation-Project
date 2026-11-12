using System.Collections.Generic;
using System.Linq;
using _Scripts.Agent.Players.QuestSystem;
using _Scripts.Agent.Players.TitleSystem;
using _Scripts.DataStructure;
using _Scripts.UI.MVVM.Observables;
using _Scripts.UI.MVVM.ViewModels;
using UnityEngine;

namespace _Scripts.UI.PlayerUI.Title_UI
{
    // 칭호 창의 ViewModel. 칭호 모듈이 바뀌었다고 알리면(해금, 달성, 진행도, 스테이지) 표시 목록을 진행도 높은 순으로 다시 만든다.
    [CreateAssetMenu(fileName = "Title List View Model", menuName = "UI/MVVM/Title List View Model")]
    public class TitleListViewModel : ViewModelBase<ITitleModule>
    {
        private readonly ObservableList<TitleViewData> _titles = new();
        private readonly List<TitleViewData> _buffer = new();
        private ITitleModule _model;

        // 보여줄 칭호. 진행도가 높은 것이 앞.
        public IReadOnlyObservableList<TitleViewData> Titles => _titles;

        protected override void OnConnect(ITitleModule model)
        {
            _model = model;
            _model.OnTitlesChanged += Refresh;
            Refresh();
        }

        protected override void OnDisconnect()
        {
            _model.OnTitlesChanged -= Refresh;
            _model = null;
        }

        protected override void ResetState()
        {
            _titles.Clear();
        }

        private void Refresh()
        {
            _buffer.Clear();

            foreach (Title title in _model.Titles)
            {
                // 다른 스테이지의 칭호는 완료한 것만 보여준다.
                if (!title.IsActive && !title.IsComplete) continue;

                _buffer.Add(CreateViewData(title));
            }

            // 진행도가 같으면 해금된 순서를 유지한다(안정 정렬).
            _titles.SetAll(_buffer.OrderByDescending(viewData => viewData.Progress).ToList());
        }

        // 달성한 칭호는 퀘스트 목록이 비워지므로(Title.Clear) 퀘스트 없이 보여준다.
        private static TitleViewData CreateViewData(Title title)
        {
            List<QuestViewData> quests = title.IsComplete
                ? new List<QuestViewData>()
                : title.GetQuests().Select(CreateQuestViewData).ToList();

            return new TitleViewData(
                title.TitleSo.TitleName,
                title.TitleSo.TitleDescription,
                title.TitleSo.UpgradeDescription,
                quests,
                title.IsComplete);
        }

        private static QuestViewData CreateQuestViewData(Quest quest)
            => new(quest.QuestSo.QuestName, quest.QuestSo.QuestDescription,
                new Gauge(quest.CurrentAmount, quest.RequiredAmount), quest.IsComplete);
    }
}
