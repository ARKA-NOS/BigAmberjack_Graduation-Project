using UnityEngine;

namespace _Scripts.Maps.New_Map.BossMapSystem
{
    public class BossMap : MonoBehaviour
    {
        [SerializeField] private Transform playerStartTrm;
        
        public Vector3 StartPos => playerStartTrm.position;
        
    }
}