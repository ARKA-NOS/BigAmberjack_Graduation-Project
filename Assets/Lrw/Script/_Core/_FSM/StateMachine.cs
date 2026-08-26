using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Lrw.Script._Core._FSM
{
    public class StateMachine<TK>
    {
        private Dictionary<TK,IState> _states = new();
        
        private IState _currentState;
        
        public void AddState(TK key, IState state)
        {
            if (!_states.TryAdd(key, state))
            {
                Debug.LogError($"{state.GetType().Name} is already added!");
            }
        }

        public void ChangeState(TK key)
        {
            _currentState?.Exit();
            _currentState = GetState(key);
            _currentState?.Enter();
        }

        private IState GetState(TK key)
        {
            if (key == null)
            {
                Debug.LogWarning("State Key is null");
                return null;
            }
            
            if(_states.TryGetValue(key,out IState state))
                return state;
            
            Debug.LogWarning("State not found");
            return null;
        }

        public IState[] GetStates() => _states.Values.ToArray();
        public TK[] GetKeys() => _states.Keys.ToArray();
        
        public void Update()
        {
            _currentState?.StateUpdate();
        }

        public void FixedUpdate()
        {
            _currentState?.StateFixedUpdate();
        }

    }
}