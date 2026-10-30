using System;
using UnityEngine;

namespace Lrw.Script.Agent.Players.QuestSystem
{
    public class Quest
    {
        public readonly QuestSo QuestSo;
        public float CurrentAmount { get; private set; }= 0f;
        public readonly float RequiredAmount;
        public float NormalizedAmount => CurrentAmount / RequiredAmount;
        public bool IsComplete { get; private set; }
        public event Action OnComplete;
        public event Action OnChanged;
        
        public Quest(QuestSo questSo)
        {
            QuestSo = questSo;
            CurrentAmount = 0f;
            RequiredAmount = QuestSo.RequiredAmount;
            IsComplete = false;
        }
        
        public void AddAmount(float amount)
        {
            if(IsComplete) return;
            float prevAmount = CurrentAmount;
            CurrentAmount = Mathf.Clamp(CurrentAmount + amount,0f,RequiredAmount);
            
            if(!Mathf.Approximately(prevAmount,CurrentAmount)) 
                OnChanged?.Invoke();
            
            if (Mathf.Approximately(CurrentAmount, RequiredAmount))
            {
                IsComplete = true;
                OnComplete?.Invoke();
            }
            
        }
    }
}