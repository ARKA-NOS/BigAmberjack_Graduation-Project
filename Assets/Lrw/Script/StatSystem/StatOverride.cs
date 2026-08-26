using System;
using UnityEngine;

namespace Lrw.Script.StatSystem
{
    [Serializable]
    public class StatOverride
    {
        [field:SerializeField] public StatData StatData { get;private set; }
        [field:SerializeField] public float BaseValue { get;private set; }
        
    }
}