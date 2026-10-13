using System;
using Agents;
using DevLib.AnimatorSystem;
using DevLib.BattleSystem;
using DevLib.ModuleSystem;
using Lrw.Script._Core._Debug;
using Lrw.Script.Agent.SkillSystem.NormalSkill;
using Lrw.Script.Agent.StatSystem;
using UnityEngine;

namespace Lrw.Script.Player.Skills
{
    public class MeleeAttackSkill : AbstractCoolTimeSkill
    {
        [Header("Stat Data")]
        [SerializeField] private StatData physicalDamage;
        [SerializeField] private StatData attackSpeed;
        
        [Header("Animation Setting")]
        [SerializeField] private AnimationCurve animationSpeedCurve;
        [SerializeField] private HashDataSO animHash;
        [SerializeField] private AbstractDamageCaster damageCaster;
        [SerializeField] private int animatorLayer = 0;
        
        private IRenderer _renderer;
        private IAnimationTrigger _trigger;
        private Animator _animator;
        
        private Stat damageStat;
        private Stat attackSpeedStat;
        
        public override float CoolTime => BaseCoolTime / attackSpeedStat.Value;
        
        public override void InitSkill(ModuleOwner owner)
        {
            base.InitSkill(owner);
            
            FDebug.Assert(owner.TryGetModule(out IStatModule statModule),
                $"[MeleeAttackSkill : {gameObject.name}] IStatModule is not found");
            
            FDebug.Assert(owner.TryGetModule(out _renderer),
                $"[MeleeAttackSkill : {gameObject.name}] IRenderer is not found");

            _animator = _renderer.Animator;
            
            FDebug.Assert(owner.TryGetModule(out _trigger),
                $"[MeleeAttackSkill : {gameObject.name}] IAnimationTrigger is not found");
            
            damageStat = statModule.GetStat(physicalDamage);
            attackSpeedStat = statModule.GetStat(attackSpeed);
        }


        protected override void OnUseSkill()
        {
            base.OnUseSkill();
            _renderer.PlayClip(animHash.HashValue);
        }

        public override void UpdateSkill()
        {
            base.UpdateSkill();
            AnimatorStateInfo info = _animator.GetCurrentAnimatorStateInfo(animatorLayer);
            _animator.speed = animationSpeedCurve.Evaluate(info.normalizedTime);
        }
        
        
        
        
    }
}