using UnityEngine;

namespace Lrw.Script.TitleSystem
{
    [CreateAssetMenu(fileName = "Quest SO", menuName = "Title/Quest SO", order = 0)]
    public class QuestSo : ScriptableObject
    {
        [field: SerializeField] public string QuestName { get; private set; }
        [field: SerializeField, TextArea] public string QuestDescription { get; private set; }

        [field: SerializeField] public int NeedCount { get; private set; } = 1;

    }
}