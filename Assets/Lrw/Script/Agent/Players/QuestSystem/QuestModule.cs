using System.Collections.Generic;
using DevLib.ModuleSystem;
using Lrw.Script._Core._ServiceLocator;

namespace Lrw.Script.Agent.Players.QuestSystem
{
    public class QuestModule : Module, IQuestModule
    {
        private Dictionary<QuestSo, Quest> _questDict;

        public override void Initialize(ModuleOwner owner)
        {
            base.Initialize(owner);
            _questDict = new();
        }
        
        public void AddQuestStack(QuestSo questType, float value)
            => GetQuest(questType).AddAmount(value);
        
        public Quest GetQuest(QuestSo questType)
        {
            if (_questDict.TryGetValue(questType, out Quest quest)) return quest;
            quest = new Quest(questType);
            _questDict.Add(questType, quest);
            return quest;
        }
        
        
    }
}