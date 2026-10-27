using System;
using System.Collections.Generic;
using System.Linq;
using DevLib.ModuleSystem;
using Lrw.Script.Agent.Players.QuestSystem;
using UnityEngine;

namespace Lrw.Script.Agent.Players.TitleSystem
{
    public class TitleModule : Module, IAfterInitModule
    {
        [SerializeField] private TitleGroupSo titleGroup;
        
        private Dictionary<AbstractTitleSo,Title> _titles;
        private HashSet<AbstractTitleSo> _completedTitles;
        private Dictionary<AbstractTitleSo,List<AbstractTitleSo>> _waitingTitles;
        private IQuestModule _questModule;
        
        public override void Initialize(ModuleOwner owner)
        {
            base.Initialize(owner);
            _questModule = owner.GetModule<IQuestModule>();
            _titles = new();
            _completedTitles = new();
            _waitingTitles = new();
        }
        
        public void AfterInit()
        {
            foreach (AbstractTitleSo titleSo in titleGroup.TitleList)
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

        private void AddWaitingTitle(AbstractTitleSo titleSo, AbstractTitleSo[] requiredTitles)
        {
            foreach (AbstractTitleSo requiredTitle in requiredTitles)
            {
                if (requiredTitle == null || requiredTitle == titleSo || !titleGroup.TitleList.Contains(requiredTitle))
                {
                    Debug.LogWarning($"[TitleModule] {titleSo.name}의 선행 칭호가 TitleList에 없거나 자기 자신이라 해금되지 않습니다.", titleSo);
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
        
    }
}