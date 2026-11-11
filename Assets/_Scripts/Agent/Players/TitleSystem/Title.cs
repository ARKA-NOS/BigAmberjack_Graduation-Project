using System;
using System.Collections.Generic;
using System.Linq;
using _Scripts.Agent.Players.QuestSystem;

namespace _Scripts.Agent.Players.TitleSystem
{
    public class Title
    {
        public readonly AbstractTitleSo TitleSo;
        private readonly List<Quest> quests;
        public event Action<Title> OnComplete;
        // 퀘스트 진행도가 바뀌었을 때.
        public event Action<Title> OnProgressChanged;

        public bool IsComplete { get; private set; }
        public bool IsActive { get; private set; } = true;

        public Title(AbstractTitleSo titleSo, IQuestModule questModule)
        {
            TitleSo = titleSo;
            quests = new();
            IsComplete = false;
            
            foreach (QuestSo questSo in titleSo.Quests)
            {
                Quest quest = questModule.AddQuest(questSo);
                quest.OnChanged += QuestValueChanged;
                quest.OnComplete += QuestComplete;
                quests.Add(quest);
            }
        }
        
        public void SetActive(bool value)
        {
            IsActive = value;
            foreach (Quest quest in quests)
            {
                quest.SetActive(value);
            }
        }

        private void QuestValueChanged()
        {
            OnProgressChanged?.Invoke(this);
        }
        
        private void QuestComplete()
        {
            if (quests.All(x => x.IsComplete))
            {
                TitleComplete();
            }
        }

        private void TitleComplete()
        {
            IsComplete = true;
            OnComplete?.Invoke(this);
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
            OnComplete = null;
            OnProgressChanged = null;
        }
        
        public IReadOnlyList<Quest> GetQuests() => quests;
    }
}