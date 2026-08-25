using System;
using System.Collections.Generic;
using Agents.FSM;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Agents.Players
{
    public class StateMachine
    {
        public AgentState CurrentState { get; private set; }

        private Dictionary<int, AgentState> _stateDict;

        public StateMachine(Agent agent, StateSo[] stateList)
        {
            _stateDict = new Dictionary<int, AgentState>();
            foreach (StateSo stateData in stateList)
            {
                Type type = Type.GetType(stateData.className); //해당 이름의 클래스 타입을 가져온다.
                Debug.Assert(type != null, $"타입을 찾는데 실패했습니다. : {stateData.className}");

                int paramHash = stateData.stateParam == null ? 0 : stateData.stateParam.HashValue;
                int layerIndex = stateData.layerIndex;
                
                // 런타임에 객체의 형식정보를 들여다보는 기능  
                AgentState agentState = (AgentState)Activator.CreateInstance(type, agent, paramHash, layerIndex);

                _stateDict.Add(stateData.assetIndex, agentState);
            }
        }

        public void ChangeState(int newStateIndex, float transitionDuration = 0.1f)
        {
            CurrentState?.Exit();
            AgentState newState = _stateDict.GetValueOrDefault(newStateIndex);
            Debug.Assert(newState != null, $"찾고자 하는 인덱스의 상태가 없습니다. : {newStateIndex}");
            
            CurrentState = newState;
            CurrentState.Enter(transitionDuration);
        }

        public void UpdateMachine() => CurrentState?.Update();
    }
}