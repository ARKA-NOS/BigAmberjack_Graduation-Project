using Lrw.Script._Core.SceneSystem;
using UnityEngine;

namespace Lrw.Script.Agent.Players.TitleSystem
{
    [CreateAssetMenu(fileName = "Title Group", menuName = "Title/Title Group", order = 0)]
    public class TitleGroupSo : ScriptableObject
    {
        [field:SerializeField, Tooltip("이 그룹의 칭호를 진행할 스테이지. 비워 두면 모든 스테이지에서 진행한다.")]
        public SceneSo Stage { get; private set; }

        [field:SerializeField] public AbstractTitleSo[] TitleList { get; private set; }
    }
}
