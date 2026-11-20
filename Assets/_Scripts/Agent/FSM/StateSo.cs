using _Scripts.DevLib.AnimatorSystem;
using UnityEngine;

namespace _Scripts.Agent.FSM
{
    [CreateAssetMenu(fileName = "State data", menuName = "Agent/State data", order = 0)]
    public class StateSo : ScriptableObject
    {
        public string stateName;
        public string className;
        public int assetIndex;
        public int layerIndex;
        public HashDataSO stateParam;
    }
}