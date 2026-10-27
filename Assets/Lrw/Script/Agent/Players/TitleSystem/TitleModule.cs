using System;
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
                Title title = new Title(titleSo, _questModule);
                _titles.Add(titleSo, title);
                title.OnComplete += Upgrade;
            }
        }

        private void Upgrade(Title title)
        {
            title.TitleSo.Upgrade(_owner);
            title.OnComplete -= Upgrade;
        }

        
    }
}