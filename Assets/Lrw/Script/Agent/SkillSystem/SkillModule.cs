using System.Collections.Generic;
using DevLib.ModuleSystem;
using Lrw.Script._Core._Debug;
using Lrw.Script.Agent.SkillSystem.NormalSkill;

namespace Lrw.Script.Agent.SkillSystem
{
    public class SkillModule : Module, ISkillModule
    {
        private Dictionary<SkillSO, INormalSkill> _skillPlayers = new();
        
        public ModuleOwner Owner { get; private set; }

        public override void Initialize(ModuleOwner owner)
        {
            base.Initialize(owner);
            Owner = owner;
            _skillPlayers = GetSkillPlayers(GetComponentsInChildren<INormalSkill>());
        }

        private Dictionary<SkillSO, INormalSkill> GetSkillPlayers(INormalSkill[] skillPlayers)
        {
            Dictionary<SkillSO, INormalSkill> skillDict = new();

            foreach (INormalSkill skillPlayer in skillPlayers)
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
            return _skillPlayers.TryGetValue(skillSo, out INormalSkill player) && player.CanUseSkill();
        }

        public void UseSkill(SkillSO skillSo)
        {
            if(skillSo == null) return;
            if (_skillPlayers.TryGetValue(skillSo, out INormalSkill player))
            {
                player.UseSkill();
            }
        }
        
    }
}