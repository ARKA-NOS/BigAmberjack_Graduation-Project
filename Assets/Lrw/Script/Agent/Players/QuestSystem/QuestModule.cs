using System.Collections.Generic;
using DevLib.ModuleSystem;
using UnityEngine;

namespace Lrw.Script.Agent.Players.QuestSystem
{
    public class QuestModule : Module
    {
        private Dictionary<QuestKey, List<Quest>> _questDict;

        public override void Initialize(ModuleOwner owner)
        {
            base.Initialize(owner);
            
        }

        public void AddQuest(QuestSo questSo)
        {
            QuestKey key = questSo.QuestKey;
            if (!_questDict.TryGetValue(key, out List<Quest> questList))
            {
                questList = new List<Quest>();
                _questDict.Add(questSo.QuestKey, questList);
            }

            Quest quest = new Quest(questSo);
            questList.Add(quest);
        }

        public void AddQuestStack(QuestKey key, int amount)
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