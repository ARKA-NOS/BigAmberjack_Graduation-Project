using System.Collections.Generic;
using DevLib.ModuleSystem;
using UnityEngine;

namespace Lrw.Script.Agent.Players.QuestSystem
{
    public class QuestModule : Module, IQuestModule
    {
        private Dictionary<QuestKey, List<Quest>> _questDict;

        public override void Initialize(ModuleOwner owner)
        {
            base.Initialize(owner);
            _questDict = new();
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
        
        public void AddQuestAmount(QuestKey key, int amount)
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