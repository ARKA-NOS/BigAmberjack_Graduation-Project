using Lrw.Script.Upgrade;
using UnityEngine;

namespace Lrw.Script.Player
{
    [CreateAssetMenu(fileName = "Character Data", menuName = "Character/Character Data", order = 0)]
    public class CharacterData : ScriptableObject
    {
        [field: SerializeField] public GameObject Prefab { get; private set; }
        [field: SerializeField] public UpgradeTree Tree { get; private set; }
        
    }
}