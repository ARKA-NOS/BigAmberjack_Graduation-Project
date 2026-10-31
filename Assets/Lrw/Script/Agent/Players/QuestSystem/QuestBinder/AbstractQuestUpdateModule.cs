using DevLib.ModuleSystem;
using UnityEngine;

namespace Lrw.Script.Agent.Players.QuestSystem.QuestBinder
{
    public abstract class AbstractQuestUpdateModule : MonoBehaviour
    {
        [SerializeField] private QuestKey questKey;
        
        protected ModuleOwner Owner { get; private set; }
        private IQuestModule  _questModule;
        
        public void Init(ModuleOwner owner)
        {
            Owner = owner;
            _questModule = Owner.GetModule<IQuestModule>();
            OnInit(owner);
        }
        
        protected abstract void OnInit(ModuleOwner owner);
        
        protected void AddQuestAmount(float amount)
        {
            _questModule.AddQuestAmount(questKey, amount);
        }
        
    }
}