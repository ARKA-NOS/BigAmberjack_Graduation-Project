using System.Collections;
using System.Linq;
using Lrw.Script._Core;
using Lrw.Script._Core._Debug;
using UnityEngine;

namespace Lrw.Script.Agent.SkillSystem
{
    public class SkillPlayer : MonoBehaviour, ISkillPlayer
    {
        [field: SerializeField] public SkillSO Skill {get; private set;}

        public GameObject SkillPlayerGameObject => gameObject;
        
        private ISkillModule _skillModule;
        
        public void InitSkill(ISkillModule skillModule)
        {
            _skillModule = skillModule;
            FDebug.Assert(Skill != null, "Skill SO is null");
        }
        
        public bool CanUseSkill()
            => Skill.Conditions.All(x => x.UseCondition(_skillModule.Owner,this));

        public void UseSkill()
            => StartCoroutine(SkillUseLoop());
        

        private IEnumerator SkillUseLoop()
        {
            foreach (var action in Skill.Actions)
            {
                Coroutine coroutine = StartCoroutine(action.SkillUse(_skillModule.Owner,this));
                yield return coroutine;
            }
        }
        
    }
}