using System;
using DevLib.EventChannelSystem;
using DevLib.SoundSystem;
using UnityEngine;

namespace DevLib.SkillSystem
{
    public abstract class AbstractSkill : MonoBehaviour, ISkill
    {
        public event Action<AbstractSkill> OnSkillEnd;
        [field: SerializeField] public SkillDataSo SkillData { get; private set; }
        
        public float NormalizedCooldown
        {
            get
            {
                if (SkillData == null || SkillData.cooldown <= 0f) return 0f;
                return Mathf.Clamp01(1f - (Time.time - LastUsedTime) / SkillData.cooldown);
            }
        }
        public bool IsUsing { get; private set; }

        protected ISkillModule SkillModule;
        protected float LastUsedTime = float.NegativeInfinity;
        
        public virtual void InitializeSkill(ISkillModule skillModule)
        {
            SkillModule = skillModule;
        }

        public abstract bool CanUseSkill(GameObject target = null);

        public virtual void UseSkill(GameObject target = null)
        {
            IsUsing = true;
        }

        public void StopSkill()
        {
            CleanUpSkillData();
        }

        public virtual void CleanUpSkillData()
        {
            LastUsedTime = Time.time;
            IsUsing = false;
            OnSkillEnd?.Invoke(this);
        }
    }
}