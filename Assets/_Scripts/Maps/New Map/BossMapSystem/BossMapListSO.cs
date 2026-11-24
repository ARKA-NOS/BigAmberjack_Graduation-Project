using System.Collections.Generic;
using UnityEngine;

namespace _Scripts.Maps.New_Map.BossMapSystem
{
    [CreateAssetMenu(fileName = "Boss Map List", menuName = "MapSystem/Boss Map List", order = 0)]
    public class BossMapListSO : ScriptableObject
    {
        [field:SerializeField] public List<BossMap> Prefabs { get; private set; } = new();
        
    }
}