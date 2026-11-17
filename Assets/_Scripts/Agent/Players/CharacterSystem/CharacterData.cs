using _Scripts.Upgrade.SO;
using UnityEngine;

namespace _Scripts.Agent.Players.CharacterSystem
{
    [CreateAssetMenu(fileName = "Character Data", menuName = "Character/Character Data", order = 0)]
    public class CharacterData : ScriptableObject
    {
        [field: SerializeField] public GameObject Prefab { get; private set; }
        [field: SerializeField] public UpgradeTreeSO TreeSo { get; private set; }

        
        
    }
}