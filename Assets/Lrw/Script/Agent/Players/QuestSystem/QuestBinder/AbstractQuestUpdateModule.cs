using DevLib.ModuleSystem;
using UnityEngine;

namespace Lrw.Script.Agent.Players.QuestSystem.QuestBinder
{
    public abstract class AbstractQuestUpdateModule : MonoBehaviour
    {
        protected ModuleOwner Owner { get; private set; }

        public void Init(ModuleOwner owner)
        {
            Owner = owner;
            OnInit(owner);
        }

        protected abstract void OnInit(ModuleOwner owner);
        
    }
}