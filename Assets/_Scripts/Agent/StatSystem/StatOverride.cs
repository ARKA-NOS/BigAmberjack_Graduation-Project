using System;
using UnityEngine;

namespace _Scripts.Agent.StatSystem
{
    [Serializable]
    public class StatOverride
    {
        [field:SerializeField] public StatData StatData { get;private set; }
        [field:SerializeField] public float BaseValue { get;private set; }
        
    }
}