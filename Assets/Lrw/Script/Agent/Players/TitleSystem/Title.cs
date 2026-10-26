using System;
using System.Collections.Generic;
using Lrw.Script.Agent.Players.QuestSystem;

namespace Lrw.Script.Agent.Players.TitleSystem
{
    public class Title
    {
        public readonly AbstractTitleSo TitleSo;
        private readonly List<Quest> quests;
        private readonly int _questCount;
        private int _completedQuestCount;
        public bool InProgress { get; private set; }
        public bool IsComplete { get; private set; }
        event Action OnComplete;
        
        public Title(AbstractTitleSo titleSo, IQuestModule questModule)
        {
            TitleSo = titleSo;
            quests = new();
            InProgress = false;
            IsComplete = false;
            _questCount = titleSo.Quests.Length;
            _completedQuestCount = 0;
            
            foreach (QuestSo questSo in titleSo.Quests)
            {
                Quest quest = questModule.GetQuest(questSo);
                quest.OnChanged += QuestValueChanged;
                quest.OnComplete += QuestComplete;
                quests.Add(quest);
            }
        }
        
        private void QuestValueChanged()
        {
            InProgress = true;
        }
        
        private void QuestComplete()
        {
            _completedQuestCount++;

            if (_questCount == _completedQuestCount)
            {
                TitleComplete();
            }
        }

        private void TitleComplete()
        {
            IsComplete = true;
            OnComplete?.Invoke();
            Clear();
        }

        private void Clear()
        {
            foreach (Quest quest in quests)
            {
                quest.OnChanged -= QuestValueChanged;
                quest.OnComplete -= QuestComplete;
            }
            quests.Clear();
        }
        
        public IReadOnlyList<Quest> GetQuests() => quests;
    }
}