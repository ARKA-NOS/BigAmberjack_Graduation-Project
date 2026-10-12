using DevLib.ModuleSystem;
using Lrw.Script.Agent.SkillSystem.Interface;
using UnityEngine;

namespace Lrw.Script.Agent.SkillSystem.NormalSkill
{
    public abstract class AbstractSkill : MonoBehaviour, ISkill
    {
        [field:SerializeField] public SkillSO SkillSo { get; private set; }

        public bool IsUsing { get; protected set; }
        
        protected ModuleOwner Owner { get; private set; }

        public virtual void InitSkill(ModuleOwner owner)
        {
            Owner = owner;
        }

        public abstract bool CanUseSkill();

        public virtual void UseSkill()
        {
            
        }

        public virtual void StopSkill()
        {
            
        }
        
    }
}