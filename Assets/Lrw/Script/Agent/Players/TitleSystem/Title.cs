using System;
using System.Collections.Generic;
using System.Linq;
using Lrw.Script.Agent.Players.QuestSystem;

namespace Lrw.Script.Agent.Players.TitleSystem
{
    public class Title
    {
        public readonly AbstractTitleSo TitleSo;
        private readonly List<Quest> quests;
        public event Action<Title> OnComplete;

        public bool IsComplete { get; private set; }

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
        
        private void QuestValueChanged()
        {
            
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
        }
        
        public IReadOnlyList<Quest> GetQuests() => quests;
    }
}