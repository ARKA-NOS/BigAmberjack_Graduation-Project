using UnityEngine;

namespace Lrw.Script.Agent.Players.TitleSystem
{
    [CreateAssetMenu(fileName = "Title Group", menuName = "Title/Title Group", order = 0)]
    public class TitleGroupSo : ScriptableObject
    {
        [field:SerializeField] public AbstractTitleSo[] TitleList { get; private set; }
    }
}