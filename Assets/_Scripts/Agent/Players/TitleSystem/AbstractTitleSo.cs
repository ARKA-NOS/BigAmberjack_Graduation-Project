using _Scripts.Agent.Players.QuestSystem;
using _Scripts.DevLib.ModuleSystem;
using UnityEngine;

namespace _Scripts.Agent.Players.TitleSystem
{
    //[CreateAssetMenu(fileName = "??? Title SO", menuName = "Title/Titles/??? Title SO")]
    public abstract class AbstractTitleSo : ScriptableObject
    {
        [field:SerializeField] public string TitleName { get; private set; }
        [field:SerializeField, TextArea] public string TitleDescription { get; private set; }
        [field:SerializeField, TextArea] public string UpgradeDescription { get; private set; }
        [field:SerializeField] public Sprite TitleIcon { get; private set; }
        
        [field:SerializeField] public AbstractTitleSo[] RequiredTitles { get; private set; }
        
        [field:SerializeField] public QuestSo[] Quests { get; private set; }

        public abstract void Upgrade(ModuleOwner owner);
    }
}