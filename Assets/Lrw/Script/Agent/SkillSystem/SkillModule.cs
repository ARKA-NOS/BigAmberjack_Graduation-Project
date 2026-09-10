using System.Collections.Generic;
using DevLib.ModuleSystem;
using Lrw.Script._Core;
using Lrw.Script._Core._Debug;

namespace Lrw.Script.Agent.SkillSystem
{
    public class SkillModule : Module, ISkillModule
    {
        private Dictionary<SkillSO, ISkillPlayer> _skillPlayers = new();
        
        public ModuleOwner Owner { get; private set; }

        public override void Initialize(ModuleOwner owner)
        {
            base.Initialize(owner);
            Owner = owner;
            _skillPlayers = GetSkillPlayers(GetComponentsInChildren<ISkillPlayer>());
        }

        private Dictionary<SkillSO, ISkillPlayer> GetSkillPlayers(ISkillPlayer[] skillPlayers)
        {
            Dictionary<SkillSO, ISkillPlayer> skillDict = new();

            foreach (ISkillPlayer skillPlayer in skillPlayers)
            {
                if (skillPlayer.Skill == null)
                {
                    FDebug.LogError("Skill player doesn't have a skill.");
                    continue;
                }
                
                if (skillDict.TryAdd(skillPlayer.Skill, skillPlayer))
                {
                    skillPlayer.InitSkill(this);
                }
                else
                {
                    FDebug.LogError("Skill player doesn't have a skill.");
                }
            }

            return skillDict;
        }
        
        public bool CanUseSkill(SkillSO skillSo)
        {
            if(skillSo == null) return false;
            return _skillPlayers.TryGetValue(skillSo, out ISkillPlayer player) && player.CanUseSkill();
        }

        public void UseSkill(SkillSO skillSo)
        {
            if(skillSo == null) return;
            if (_skillPlayers.TryGetValue(skillSo, out ISkillPlayer player))
            {
                player.UseSkill();
            }
        }
        
    }
}