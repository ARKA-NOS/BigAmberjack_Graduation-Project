using System;
using DevLib.ModuleSystem;
using UnityEngine;

namespace Lrw.Script.Agent.SkillSystem
{
    public abstract class AbstractReuseSkill : AbstractSkill
    {
        private ISkill[] _skills;
        private int index = 0;
        
        public override void InitSkill(ModuleOwner owner)
        {
            _skills = GetComponentsInChildren<ISkill>(true);
        }
        
        public override bool CanUseSkill()
        {
            if (index == 0)
            {
                return OnCanUseSkill();
            }

            if (index > 0 && index <= _skills.Length)
            {
                return _skills[index - 1].CanUseSkill();
            }
            
            return false;
        }
        
        protected abstract bool OnCanUseSkill();
        
        public sealed override void UseSkill()
        {
            base.UseSkill();
            if (index == 0) OnUseSkill();
            else if (index > 0 && index <= _skills.Length) _skills[index - 1].UseSkill();
            index++;
        }
        
        protected abstract void OnUseSkill();
        
        
        private void Update()
        {
            
            
            
            
            
        }

        
        
        
    }
}