using _Scripts.Maps.Data;
using UnityEngine;

namespace _Scripts.Agent.Players.TitleSystem
{
    [CreateAssetMenu(fileName = "Title Group", menuName = "Title/Title Group", order = 0)]
    public class TitleGroupSo : ScriptableObject
    {
        [field:SerializeField, Tooltip("이 그룹의 칭호를 진행할 맵(RoomDataSO). 비워 두면 모든 맵에서 진행한다.")]
        public RoomDataSO Stage { get; private set; }

        [field:SerializeField] public AbstractTitleSo[] TitleList { get; private set; }
    }
}
