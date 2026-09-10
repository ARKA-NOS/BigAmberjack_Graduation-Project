using System;
using DevLib.ModuleSystem;
using Lrw.Script.Agent.SkillSystem;
using UnityEngine;

namespace Lrw.Script.Agent.BaseSkills
{
    public class SkillGroup : MonoBehaviour,ISkill
    {
        [field:SerializeField] public SkillSO SkillSo { get; private set; }

        float ISkill.NormalizeCooldown => _normalizeCooldown;

        private ISkill[] _skills;
        private int _currentIndex = 0;
        
        private ISkill CurrentSkill => _skills[_currentIndex];
        
        protected float _lastUsedTime = float.NegativeInfinity;
        private float _normalizeCooldown;

        public void InitSkill(ModuleOwner owner)
        {
            _skills = GetComponentsInChildren<ISkill>(true);
        }

        public bool CanUseSkill()
            => CurrentSkill.CanUseSkill();

        public void UseSkill()
        {
            CurrentSkill.UseSkill();
            _currentIndex++;
            if (_currentIndex >= _skills.Length)
            {
                _currentIndex = 0;
            }
            
        }

        public float GetMaxCooldown()
        {
            throw new Exception("123");
        }

        public float NormalizeCooldown()
        {
            throw new Exception("123");
        }
        
        
    }
}