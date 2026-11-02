using UnityEngine;

namespace Lrw.Script.Agent.Players.QuestSystem
{
    [CreateAssetMenu(fileName = "Quest Key", menuName = "Quest/Quest Key", order = 0)]
    public class QuestKey : ScriptableObject
    {
        [SerializeField, TextArea] private string description;
        
    }
}