using DevLib.ModuleSystem;
using Lrw.Script.Agent.SkillSystem.NormalSkill;

namespace Lrw.Script.Agent.SkillSystem.ReuseSkill
{
    public abstract class AbstractReuseSkillOwner : AbstractNormalSkill
    {
        private IReuseSkill[] _reuseSkills;
        
        private int _index;
        
        public override void InitSkill(ModuleOwner owner)
        {
            _reuseSkills = GetComponentsInChildren<IReuseSkill>(true);
        }

        #region CanUseSkill

        public sealed override bool CanUseSkill()
        {
            if (IndexOut()) return false;
            return _index == 0 ? base.CanUseSkill() && CanUseFirstSkill() : _reuseSkills[_index].CanUseSkill();
        }

        protected abstract bool CanUseFirstSkill();

        #endregion
        
        #region UseSkill

        public sealed override void UseSkill()
        {
            if (IndexOut()) return;
            
            base.UseSkill();
            
            if (_index == 0)
            {
                UseFirstSkill();
            }
            else
            {
                _reuseSkills[_index].UseSkill();
            }

            NextSkill();
        }

        protected abstract void UseFirstSkill();
        
        #endregion
        
        private bool IndexOut()
            => _index < 0 || _index > _reuseSkills.Length;
        
        private void NextSkill()
        {
            _index++;
            if (IndexOut())
            {
                _index = 0;
            }
        }
        
        
    }
}