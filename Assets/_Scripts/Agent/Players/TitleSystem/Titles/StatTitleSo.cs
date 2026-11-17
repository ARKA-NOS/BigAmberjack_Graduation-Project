using System;
using _Scripts.Agent.StatSystem;
using _Scripts.DevLib.ModuleSystem;
using UnityEngine;

namespace _Scripts.Agent.Players.TitleSystem.Titles
{
    // 여러 스탯을 한 번에 바꾸는 칭호
    [CreateAssetMenu(fileName = "Stat Title SO", menuName = "Title/Titles/Stat Title SO")]
    public class StatTitleSo : AbstractTitleSo
    {
        [Serializable]
        public struct StatModifyEntry
        {
            [field:SerializeField] public StatData StatData { get; private set; }
            [field:SerializeField] public StatModifyData ModifyData { get; private set; }
        }

        [SerializeField] private StatModifyEntry[] modifies;

        public override void Upgrade(ModuleOwner owner)
        {
            if (!owner.TryGetModule(out IStatModule statModule)) return;
            if (modifies == null) return;

            foreach (StatModifyEntry entry in modifies)
            {
                if (entry.StatData == null) continue;
                statModule.GetStat(entry.StatData).SetModify(this, entry.ModifyData);
            }
        }
    }
}
