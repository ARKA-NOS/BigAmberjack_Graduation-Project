using System;
using System.Collections.Generic;
using System.Linq;
using _Scripts._Core._Debug;
using _Scripts._Core._EventSystem;
using _Scripts._Core._ServiceLocator;
using _Scripts.Agent.Players.QuestSystem;
using _Scripts.Currency;
using _Scripts.DevLib.ModuleSystem;
using _Scripts.Maps;
using _Scripts.Maps.Data;
using _Scripts.Settings;
using _Scripts.UI.AlarmUI;
using UnityEngine;

namespace _Scripts.Agent.Players.TitleSystem
{
    public class TitleModule : Module, IAfterInitModule, ITitleStageReceiver, ITitleModule, ITitleEquipModule
    {
        [SerializeField] private TitleGroupSo[] titleGroups;

        [Header("Equip")]
        [Tooltip("칭호를 다시 장착할 때마다 내는 비용(칭호별 최초 장착은 무료). 재화가 비어 있거나 수량이 0이면 항상 무료.")]
        [SerializeField] private Cost equipCost;

        [Header("Stage Setting")]
        [Tooltip("첫 MapEnteredEvent가 오기 전까지 쓰는 시작 맵. 비워 두면 Stage가 없는 그룹만 진행한다.")]
        [SerializeField] private RoomDataSO startStage;

        private Dictionary<AbstractTitleSo,Title> _titles;
        private HashSet<AbstractTitleSo> _completedTitles;
        private HashSet<AbstractTitleSo> _equippedTitles;
        private HashSet<AbstractTitleSo> _everEquippedTitles; // 이번 런에 한 번이라도 장착한 칭호
        private Dictionary<AbstractTitleSo,List<AbstractTitleSo>> _waitingTitles;
        private List<AbstractTitleSo> _allTitles;
        private HashSet<AbstractTitleSo> _stageTitles;
        private IQuestModule _questModule;

        public RoomDataSO CurrentStage { get; private set; }

        public event Action OnTitlesChanged;
        public event Action OnEquipChanged;

        public IReadOnlyCollection<Title> Titles
            => _titles != null ? (IReadOnlyCollection<Title>)_titles.Values : Array.Empty<Title>();

        public override void Initialize(ModuleOwner owner)
        {
            base.Initialize(owner);
            _questModule = owner.GetModule<IQuestModule>();
            _titles = new();
            _completedTitles = new();
            _equippedTitles = new();
            _everEquippedTitles = new();
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
            SetupTitles();

            OnTitlesChanged?.Invoke();
        }

        public void ResetTitles()
        {
            foreach (Title title in _titles.Values)
            {
                title.OnComplete -= OnTitleComplete;
                title.OnProgressChanged -= OnTitleProgressChanged;
            }

            foreach (AbstractTitleSo completedTitle in _completedTitles)
                completedTitle.Downgrade(_owner);

            _titles.Clear();
            _completedTitles.Clear();
            _equippedTitles.Clear(); // 효과는 위 Downgrade에서 이미 되돌렸다. 장착 표시만 지운다.
            _everEquippedTitles.Clear();
            _waitingTitles.Clear();
            _questModule.ClearQuests();

            CurrentStage = startStage;
            RefreshStageTitles();
            SetupTitles();

            FDebug.Log("[TitleModule] 칭호 초기화", this);
            OnTitlesChanged?.Invoke();
            OnEquipChanged?.Invoke();
        }

        private void SetupTitles()
        {
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
                    FDebug.LogWarning($"[TitleModule] {titleSo.name}의 선행 칭호가 어느 TitleGroup에도 없거나 자기 자신이라 해금되지 않습니다.", titleSo);
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
            // 달성만으로는 효과가 적용되지 않는다. 장착(TrySetEquipped)할 때 적용된다.
            _completedTitles.Add(title.TitleSo);
            UnlockWaitingTitles(title.TitleSo);

            SendAlarm(title);
            TitleEvents.RaiseTitleCompleted(title.TitleSo);
            OnTitlesChanged?.Invoke();
        }
        
        // 설정의 "칭호 알림"이 꺼짐이면 알림만 생략한다. 달성 처리와 도감 기록은 그대로다.
        private void SendAlarm(Title title)
        {
            if (ServiceLocator.Get<ISettingsManager>()?.Settings.titleNotice == TitleNoticeOption.Off) return;

            AlarmEvents.SendAlarm(
                $"퀘스트 완료! : {title.TitleSo.TitleName}",
                title.TitleSo.TitleDescription,
                title.TitleSo.TitleIcon
            );
        }

        #region Equip

        public bool IsEquipped(AbstractTitleSo title)
            => title != null && _equippedTitles.Contains(title);

        public int GetEquipCost(AbstractTitleSo title)
        {
            if (equipCost.Type == null || !_everEquippedTitles.Contains(title)) return 0;
            return Mathf.Max(0, equipCost.Amount);
        }

        public bool CanPayEquipCost(AbstractTitleSo title)
        {
            int cost = GetEquipCost(title);
            if (cost <= 0) return true;

            CurrencyData currency = CurrencySystem.GetCurrencyData(equipCost.Type);
            return currency != null && currency.Has(cost);
        }

        public bool TrySetEquipped(AbstractTitleSo title, bool equipped)
        {
            if (title == null || !_completedTitles.Contains(title)) return false;
            if (_equippedTitles.Contains(title) == equipped) return false;

            if (equipped)
            {
                if (!CanPayEquipCost(title)) return false;
                PayEquipCost(title);

                _everEquippedTitles.Add(title);
                _equippedTitles.Add(title);
                title.Upgrade(_owner);
            }
            else
            {
                _equippedTitles.Remove(title);
                title.Downgrade(_owner);
            }

            OnEquipChanged?.Invoke();
            return true;
        }

        private void PayEquipCost(AbstractTitleSo title)
        {
            int cost = GetEquipCost(title);
            if (cost > 0)
                CurrencySystem.GetCurrencyData(equipCost.Type).Add(-cost);
        }

        #endregion

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
