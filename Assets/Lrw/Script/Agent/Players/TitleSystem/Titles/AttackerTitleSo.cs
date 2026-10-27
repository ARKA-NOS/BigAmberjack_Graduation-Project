using DevLib.ModuleSystem;
using Lrw.Script._Core._Debug;
using Lrw.Script.Agent.StatSystem;
using UnityEngine;

namespace Lrw.Script.Agent.Players.TitleSystem.Titles
{
    [CreateAssetMenu(fileName = "Attacker Title SO", menuName = "Title/Titles/Attacker Title SO")]
    public class AttackerTitleSo : AbstractTitleSo
    {
        [SerializeField] private StatData statData;
        [SerializeField] private StatModifyData statModifyData;
        
        public override void Upgrade(ModuleOwner owner)
        {
            if (!owner.TryGetModule(out IStatModule statModule)) return;
            if(statData == null) return;
            FDebug.Log("Title Upgrade");
            statModule.GetStat(statData).SetModify(this,statModifyData);
        }
    }
}