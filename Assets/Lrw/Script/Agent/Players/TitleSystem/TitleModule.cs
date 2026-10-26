using System.Collections.Generic;
using DevLib.ModuleSystem;
using Lrw.Script.Agent.Players.QuestSystem;
using UnityEngine;

namespace Lrw.Script.Agent.Players.TitleSystem
{
    public class TitleModule : Module, IAfterInitModule
    {
        [SerializeField] private TitleGroupSo titleGroup;
        
        private Dictionary<AbstractTitleSo,Title> _titles;
        private IQuestModule _questModule;
        public override void Initialize(ModuleOwner owner)
        {
            base.Initialize(owner);

            _questModule = owner.GetModule<IQuestModule>();
            
            _titles = new();
        }
        
        public void AfterInit()
        {
            foreach (AbstractTitleSo titleSo in titleGroup.TitleList)
            {
                _titles.Add(titleSo,new Title(titleSo,_questModule));
            }
            
        }
        
        
        
    }
}