using UnityEngine;

namespace Lrw.Script.Agent.Players.QuestSystem
{
    [CreateAssetMenu(fileName = "Quest SO", menuName = "Quest/Quest SO", order = 0)]
    public class QuestSo : ScriptableObject
    {
        [field:SerializeField] public QuestKey QuestKey { get; private set; }
        [field: SerializeField] public string QuestName { get; private set; }
        [field: SerializeField, TextArea] public string QuestDescription { get; private set; }
        [field:SerializeField] public Sprite QuestIcon { get; private set; }
        
        [field: SerializeField] public float RequiredAmount { get; private set; } = 100f;
    }
}