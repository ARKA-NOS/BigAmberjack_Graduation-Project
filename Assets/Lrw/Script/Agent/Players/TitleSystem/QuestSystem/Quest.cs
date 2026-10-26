using System;
using UnityEngine;

namespace Lrw.Script.Agent.Players.TitleSystem.QuestSystem
{
    public class Quest
    {
        public readonly QuestSo QuestSo;
        public float CurrentAmount = 0f;
        public bool IsComplete { get; private set; }
        public event Action OnComplete;
        public event Action OnChanged;
        
        public Quest(QuestSo questSo)
        {
            QuestSo = questSo;
            CurrentAmount = 0f;
            IsComplete = false;
        }
        
        public void AddAmount(float amount)
        {
            if(IsComplete) return;
            float requiredAmount = QuestSo.RequiredAmount;
            float prevAmount = CurrentAmount;
            CurrentAmount = Mathf.Clamp(CurrentAmount + amount,0f,requiredAmount);
            
            if(Mathf.Approximately(prevAmount,CurrentAmount)) 
                OnChanged?.Invoke();
            
            if (Mathf.Approximately(CurrentAmount, requiredAmount))
            {
                IsComplete = true;
                OnComplete?.Invoke();
            }
            
        }
    }
}