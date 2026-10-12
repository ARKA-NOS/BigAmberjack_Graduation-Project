using System.Collections.Generic;
using DevLib.ModuleSystem;
using Lrw.Script._Core._Debug;
using Lrw.Script.Agent.SkillSystem.NormalSkill;
using UnityEngine;

namespace Lrw.Script.Agent.SkillSystem
{
    public class SkillModule : Module, ISkillModule,IAfterInitModule
    {
        private Dictionary<SkillSO, ISkill> _skillPlayers = new();
        
        public ModuleOwner Owner { get; private set; }
        
        private ISkill _normalSkill;

        public override void Initialize(ModuleOwner owner)
        {
            base.Initialize(owner);
            Owner = owner;
        }
        
        public void AfterInit()
        {
            _skillPlayers = GetSkillPlayers(GetComponentsInChildren<ISkill>());
        }

        private Dictionary<SkillSO, ISkill> GetSkillPlayers(ISkill[] skillPlayers)
        {
            Dictionary<SkillSO, ISkill> skillDict = new();

            foreach (ISkill skillPlayer in skillPlayers)
            {
                if (skillPlayer.SkillSo == null)
                {
                    FDebug.LogError($"[{skillPlayer}] Skill SO가 null 입니다.");
                    continue;
                }

                if (!skillDict.TryAdd(skillPlayer.SkillSo, skillPlayer))
                {
                    FDebug.LogError($"[{skillPlayer}] 같은 Skill SO가 있습니다.");
                    continue;
                }
                
                skillPlayer.InitSkill(_owner);
            }

            return skillDict;
        }
        
        public bool CanUseSkill(SkillSO skillSo)
        {
            if(skillSo == null) return false;
            return _skillPlayers.TryGetValue(skillSo, out ISkill player) && player.CanUseSkill();
        }

        public void UseSkill(SkillSO skillSo, bool keyPressed)
        {
            if(skillSo == null) return;
            if (!_skillPlayers.TryGetValue(skillSo, out ISkill player)) return;
            
            player.UseSkill();
            
        }

        
    }
}