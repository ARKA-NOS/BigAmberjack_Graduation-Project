using _Scripts._Core._Debug;
using UnityEngine;

namespace _Scripts.Maps.New_Map.BossMapSystem
{
    public class BossMap : MonoBehaviour
    {
        [SerializeField] private Transform playerStartTrm;

        public Vector3 StartPos
        {
            get
            {
                if(playerStartTrm != null)
                    return playerStartTrm.position; 
                FDebug.LogError("PlayerStartTrm is null");
                return transform.position;
            }
        }
        
    }
}