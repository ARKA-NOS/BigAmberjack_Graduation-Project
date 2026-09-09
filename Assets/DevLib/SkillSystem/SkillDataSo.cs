using System;
using DevLib.AnimatorSystem;
using UnityEngine;

namespace DevLib.SkillSystem
{
    [CreateAssetMenu(fileName = "SkillData", menuName = "Lib/Skill/SkillData", order = 0)]
    public class SkillDataSo : ScriptableObject
    {
        public Sprite icon;
        public float maxRange;
        public HashDataSO skillIdHash;
        public HashDataSO skillAnimationHash;
        public float baseDamage = 1f;
        public float kbForce;
        public float cooldown;
        
    }
}