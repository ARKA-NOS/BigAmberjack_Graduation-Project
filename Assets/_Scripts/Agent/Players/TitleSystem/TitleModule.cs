using System;
using System.Collections.Generic;
using System.Linq;
using _Scripts._Core._EventSystem;
using _Scripts.Agent.Players.QuestSystem;
using _Scripts.DevLib.ModuleSystem;
using _Scripts.Maps;
using _Scripts.Maps.Data;
using _Scripts.UI.AlarmUI;
using UnityEngine;

namespace _Scripts.Agent.Players.TitleSystem
{
    public class TitleModule : Module, IAfterInitModule, ITitleStageReceiver, ITitleModule
    {
        [SerializeField] private TitleGroupSo[] titleGroups;

        [Header("Stage Setting")]
        [Tooltip("첫 MapEnteredEvent가 오기 전까지 쓰는 시작 맵. 비워 두면 Stage가 없는 그룹만 진행한다.")]
        [SerializeField] private RoomDataSO startStage;

        private Dictionary<AbstractTitleSo,Title> _titles;
        private HashSet<AbstractTitleSo> _completedTitles;
        private Dictionary<AbstractTitleSo,List<AbstractTitleSo>> _waitingTitles;
        private List<AbstractTitleSo> _allTitles;
        private HashSet<AbstractTitleSo> _stageTitles;
        private IQuestModule _questModule;

        public RoomDataSO CurrentStage { get; private set; }

        public event Action OnTitlesChanged;

        public IReadOnlyCollection<Title> Titles
            => _titles != null ? (IReadOnlyCollection<Title>)_titles.Values : Array.Empty<Title>();

        public override void Initialize(ModuleOwner owner)
        {
            base.Initialize(owner);
            _questModule = owner.GetModule<IQuestModule>();
            _titles = new();
            _completedTitles = new();
            _waitingTitles = new();
            _allTitles = new();
            _stageTitles = new();
            
            EventBus<MapEnteredEvent>.Subscribe(SceneMoveEvent);
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

            OnTitlesChanged?.Invoke();
        }

        private void OnDestroy()
        {
            EventBus<MapEnteredEvent>.UnSubscribe(SceneMoveEvent);
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
        
        private void SceneMoveEvent(MapEnteredEvent value)
        {
            ChangeStage(value.Room);
        }

        public void ChangeStage(RoomDataSO stage)
        {
            CurrentStage = stage;
            RefreshStageTitles();

            foreach (Title title in _titles.Values)
            {
                title.SetActive(_stageTitles.Contains(title.TitleSo));
            }

            OnTitlesChanged?.Invoke();
        }

        // 현재 스테이지 그룹과 Stage가 비어 있는 공통 그룹의 칭호만 진행 대상으로 모은다.
        private void RefreshStageTitles()
        {
            _stageTitles.Clear();
            if (titleGroups == null) return;

            foreach (TitleGroupSo group in titleGroups)
            {
                if (group == null || group.TitleList == null) continue;
                if (group.Stage != null && group.Stage != CurrentStage) continue;

                foreach (AbstractTitleSo titleSo in group.TitleList)
                {
                    if (titleSo != null)
                        _stageTitles.Add(titleSo);
                }
            }
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
            title.OnProgressChanged += OnTitleProgressChanged;
            title.SetActive(_stageTitles.Contains(titleSo));
        }

        private void OnTitleProgressChanged(Title title)
        {
            OnTitlesChanged?.Invoke();
        }

        private void OnTitleComplete(Title title)
        {
            title.OnComplete -= OnTitleComplete;
            title.OnProgressChanged -= OnTitleProgressChanged;
            Upgrade(title);
            _completedTitles.Add(title.TitleSo);
            UnlockWaitingTitles(title.TitleSo);

            SendAlarm(title);
            OnTitlesChanged?.Invoke();
        }
        
        private void SendAlarm(Title title)
            => AlarmEvents.SendAlarm(
                $"퀘스트 완료! : {title.TitleSo.TitleName}",
                title.TitleSo.TitleDescription,
                title.TitleSo.TitleIcon
            );

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
    }
}
