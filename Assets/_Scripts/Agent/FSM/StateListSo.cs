using UnityEngine;

namespace _Scripts.Agent.FSM
{
    [CreateAssetMenu(fileName = "State", menuName = "Agent/State list", order = 2)]
    public class StateListSo : ScriptableObject
    {
        [HideInInspector] public string generatePath;
        public string enumName;
        public StateSo[] states;
    }
}