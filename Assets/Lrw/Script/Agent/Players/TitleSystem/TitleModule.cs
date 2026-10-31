using System;
using System.Collections.Generic;
using System.Linq;
using DevLib.ModuleSystem;
using Lrw.Script._Core.SceneSystem;
using Lrw.Script.Agent.Players.QuestSystem;
using Lrw.Script.DataStructure;
using Lrw.Script.UI.PlayerUI.Title_UI;
using UnityEngine;

namespace Lrw.Script.Agent.Players.TitleSystem
{
    public class TitleModule : Module, IAfterInitModule, ITitleStageReceiver
    {
        [SerializeField] private TitleGroupSo[] titleGroups;
        [SerializeField] private TitleListViewModel titleListViewModel;

        [Header("Stage Setting")]
        [Tooltip("스테이지 변경 이벤트가 생기기 전까지 쓰는 시작 스테이지. 비워 두면 Stage가 없는 그룹만 진행한다.")]
        [SerializeField] private SceneSo startStage;

        private Dictionary<AbstractTitleSo,Title> _titles;
        private HashSet<AbstractTitleSo> _completedTitles;
        private Dictionary<AbstractTitleSo,List<AbstractTitleSo>> _waitingTitles;
        private List<AbstractTitleSo> _allTitles;
        private HashSet<AbstractTitleSo> _stageTitles;
        private IQuestModule _questModule;

        public SceneSo CurrentStage { get; private set; }

        public override void Initialize(ModuleOwner owner)
        {
            base.Initialize(owner);
            _questModule = owner.GetModule<IQuestModule>();
            _titles = new();
            _completedTitles = new();
            _waitingTitles = new();
            _allTitles = new();
            _stageTitles = new();

            if(titleListViewModel != null)
                titleListViewModel.Bind(new TitleListViewData(new()));
        }

        public void AfterInit()
        {
            CollectAllTitles();

            if (CurrentStage == null) CurrentStage = startStage;
            RefreshStageTitles();

            foreach (AbstractTitleSo titleSo in _allTitles)
            {
                AbstractTitleSo[] requiredTitles = titleSo.RequiredTitles;
                if (requiredTitles == null || requiredTitles.Length == 0)
                {
                    InitTitle(titleSo);
                }
                else
                {
                    AddWaitingTitle(titleSo, requiredTitles);
                }
            }
        }

        private void CollectAllTitles()
        {
            if (titleGroups == null) return;

            foreach (TitleGroupSo group in titleGroups)
            {
                if (group == null || group.TitleList == null) continue;

                foreach (AbstractTitleSo titleSo in group.TitleList)
                {
                    if (titleSo != null && !_allTitles.Contains(titleSo))
                        _allTitles.Add(titleSo);
                }
            }
        }

        #region Stage

        public void ChangeStage(SceneSo stage)
        {
            CurrentStage = stage;
            RefreshStageTitles();

            foreach (Title title in _titles.Values)
            {
                title.SetActive(_stageTitles.Contains(title.TitleSo));
            }
        }

        // 현재 스테이지 그룹과 Stage가 비어 있는 공통 그룹의 칭호만 진행 대상으로 모은다.
        private void RefreshStageTitles()
        {
            _stageTitles.Clear();
            if (titleGroups == null) return;

            foreach (TitleGroupSo group in titleGroups)
            {
                if (group == null || group.TitleList == null) continue;
                if (group.Stage != null && !IsSameStage(group.Stage, CurrentStage)) continue;

                foreach (AbstractTitleSo titleSo in group.TitleList)
                {
                    if (titleSo != null)
                        _stageTitles.Add(titleSo);
                }
            }
        }

        // 같은 씬을 가리키는 SceneSo 에셋이 여러 개일 수 있어서 씬 이름으로도 비교한다.
        private static bool IsSameStage(SceneSo a, SceneSo b)
        {
            if (a == b) return true;
            if (a == null || b == null) return false;
            return !string.IsNullOrEmpty(a.SceneName) && a.SceneName == b.SceneName;
        }

        [ContextMenu("Change Stage To Start Stage")]
        private void DebugChangeStage() => ChangeStage(startStage);

        #endregion

        private void AddWaitingTitle(AbstractTitleSo titleSo, AbstractTitleSo[] requiredTitles)
        {
            foreach (AbstractTitleSo requiredTitle in requiredTitles)
            {
                if (requiredTitle == null || requiredTitle == titleSo || !_allTitles.Contains(requiredTitle))
                {
                    Debug.LogWarning($"[TitleModule] {titleSo.name}의 선행 칭호가 어느 TitleGroup에도 없거나 자기 자신이라 해금되지 않습니다.", titleSo);
                    continue;
                }

                if (!_waitingTitles.TryGetValue(requiredTitle, out List<AbstractTitleSo> waitingList))
                {
                    waitingList = new List<AbstractTitleSo>();
                    _waitingTitles.Add(requiredTitle, waitingList);
                }
                waitingList.Add(titleSo);
            }
        }

        private void InitTitle(AbstractTitleSo titleSo)
        {
            Title title = new Title(titleSo, _questModule);
            _titles.Add(titleSo, title);
            title.OnComplete += OnTitleComplete;
            title.SetActive(_stageTitles.Contains(titleSo));
        }

        private void OnTitleComplete(Title title)
        {
            title.OnComplete -= OnTitleComplete;
            Upgrade(title);
            _completedTitles.Add(title.TitleSo);
            UnlockWaitingTitles(title.TitleSo);
        }

        private void Upgrade(Title title)
        {
            title.TitleSo.Upgrade(_owner);
        }

        private void UnlockWaitingTitles(AbstractTitleSo completedTitle)
        {
            if (!_waitingTitles.Remove(completedTitle, out List<AbstractTitleSo> waitingList)) return;

            foreach (AbstractTitleSo waitingTitle in waitingList)
            {
                if (_titles.ContainsKey(waitingTitle)) continue;

                if (waitingTitle.RequiredTitles.All(_completedTitles.Contains))
                {
                    InitTitle(waitingTitle);
                }
            }
        }

        private void Update()
        {
            UpdateViewModel();
        }

        private void UpdateViewModel()
        {
            if(titleListViewModel == null || titleListViewModel.BindData == null) return;

            List<TitleViewData> titleViewDataList = titleListViewModel.BindData.TitleViewList;

            if (titleViewDataList == null) return;

            titleViewDataList.Clear();

            foreach (Title title in _titles.Values)
            {
                // 다른 스테이지의 칭호는 완료한 것만 보여준다.
                if (!title.IsActive && !title.IsComplete) continue;

                TitleViewData viewData = new TitleViewData(
                    title.TitleSo.TitleName,title.TitleSo.TitleDescription,title.TitleSo.UpgradeDescription,
                    title.GetQuests()
                        .Select(x =>
                            new QuestViewData(x.QuestSo.QuestName, x.QuestSo.QuestDescription,
                                new Gauge(x.CurrentAmount,x.RequiredAmount),
                                x.IsComplete)
                        )
                        .ToList(),
                    title.IsComplete
                );
                titleViewDataList.Add(viewData);
            }

        }
    }
}
