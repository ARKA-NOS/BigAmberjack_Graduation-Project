using System;
using Agents.Players;
using Lrw.Script.Upgrade;
using Lrw.Script.Upgrade.SO;
using UnityEngine;

namespace Lrw.Script.Player.CharacterSystem
{
    [CreateAssetMenu(fileName = "Character Data", menuName = "Character/Character Data", order = 0)]
    public class CharacterData : ScriptableObject
    {
        [field: SerializeField] public GameObject Prefab { get; private set; }
        [field: SerializeField] public UpgradeTreeSO TreeSo { get; private set; }

        
        
    }
}