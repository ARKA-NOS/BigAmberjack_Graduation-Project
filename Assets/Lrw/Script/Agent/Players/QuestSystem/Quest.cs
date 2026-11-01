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
        // 비활성 퀘스트는 진행도가 바뀌지 않는다(다른 스테이지의 칭호 퀘스트 등).
        public bool IsActive { get; private set; }
        public event Action OnComplete;
        public event Action OnChanged;

        public Quest(QuestSo questSo)
        {
            QuestSo = questSo;
            CurrentAmount = 0f;
            RequiredAmount = QuestSo.RequiredAmount;
            IsComplete = false;
            IsActive = true;
        }

        public void SetActive(bool value) => IsActive = value;

        public void AddAmount(float amount) => SetAmount(CurrentAmount + amount);

        public void SetAmount(float amount)
        {
            if(IsComplete || !IsActive) return;
            float prevAmount = CurrentAmount;
            CurrentAmount = Mathf.Clamp(amount,0f,RequiredAmount);
            
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