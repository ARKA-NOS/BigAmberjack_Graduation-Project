using System;
using DevLib.ModuleSystem;
using Lrw.Script.Agent.SkillSystem.Interface;
using UnityEngine;

namespace Lrw.Script.Agent.SkillSystem.NormalSkill
{
    public abstract class AbstractSkill : MonoBehaviour, ISkill
    {
        [field:SerializeField] public SkillSO SkillSo { get; private set; }

        public bool IsUsing { get; private set; }

        protected ModuleOwner Owner { get; private set; }

        public virtual void InitSkill(ModuleOwner owner)
        {
            Owner = owner;
        }
        
        public virtual bool CanUseSkill() => !IsUsing;
        
        public void UseSkill()
        {
            IsUsing = true;

            OnUseSkill();
        }

        protected virtual void Update()
        {
            if (IsUsing)
            {
                UpdateSkill();
            }
        }

        /// <summary>
        /// 외부에서 스킬을 끊을 때(캔슬, 강제 종료) 호출한다.
        /// </summary>
        public void StopSkill()
        {
            IsUsing = false;
            OnStopSkill();
        }

        /// <summary>
        /// 스킬이 정상적으로 끝났을 때(애니메이션 종료 등) 하위 클래스가 호출한다.
        /// </summary>
        protected void EndSkill()
        {
            IsUsing = false;
            OnEndSkill();
        }

        /// <summary>
        /// 쿨타임 같은 공통 기능을 붙이는 추상 클래스용 훅. 실제 스킬은 OnUseSkill을 쓴다.
        /// </summary>
        protected abstract void OnUseSkill();

        public virtual void UpdateSkill() {}
        /// <summary>
        /// 정상 종료됐을 때 정리
        /// </summary>
        protected virtual void OnEndSkill() { }
        
        /// <summary>
        /// 외부에서 끊겼을 때 정리(코루틴, 히트박스, VFX 등)
        /// </summary>
        protected virtual void OnStopSkill() { }

        
    }
}
