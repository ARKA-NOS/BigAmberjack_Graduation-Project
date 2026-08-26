using DevLib.ModuleSystem;
using UnityEngine;
using UnityEngine.Events;

namespace Agents
{
    public class Agent : ModuleOwner
    {
        [SerializeField] protected UnityEvent OnHit;
    }
}