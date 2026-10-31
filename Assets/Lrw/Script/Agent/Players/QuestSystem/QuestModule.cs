using System.Collections.Generic;
using DevLib.ModuleSystem;
using Lrw.Script._Core._EventSystem;
using Lrw.Script.Agent.Players.QuestSystem.QuestBinder;

namespace Lrw.Script.Agent.Players.QuestSystem
{
    public class QuestModule : Module, IQuestModule, IAfterInitModule
    {
        private Dictionary<QuestKey, List<Quest>> _questDict;
        
        public override void Initialize(ModuleOwner owner)
        {
            base.Initialize(owner);
            _questDict = new();
            
        }
        
        public void AfterInit()
        {
            AbstractQuestUpdateModule[] questModuleList = GetComponentsInChildren<AbstractQuestUpdateModule>();

            foreach (AbstractQuestUpdateModule questModule in questModuleList)
            {
                questModule.Init(_owner);
            }
        }

        public Quest AddQuest(QuestSo questSo)
        {
            QuestKey key = questSo.QuestKey;
            if (!_questDict.TryGetValue(key, out List<Quest> questList))
            {
                questList = new List<Quest>();
                _questDict.Add(questSo.QuestKey, questList);
            }
            
            Quest quest = new Quest(questSo);
            questList.Add(quest);

            return quest;
        }
        
        public void AddQuestAmount(QuestKey key, float amount)
        {
            if (_questDict.TryGetValue(key, out List<Quest> questList))
            {
                foreach (Quest quest in questList)
                {
                    quest.AddAmount(amount);
                }
            }
        }

        
    }
}