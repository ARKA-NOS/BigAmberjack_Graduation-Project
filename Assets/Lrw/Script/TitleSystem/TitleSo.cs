using UnityEngine;

namespace Lrw.Script.TitleSystem
{
    [CreateAssetMenu(fileName = "Title So", menuName = "Title/Title So", order = 0)]
    public class TitleSo : ScriptableObject
    {
        [field:SerializeField] public string TitleName { get; private set; }
        [field:SerializeField,TextArea] public string TitleDescription { get; private set; }
        [field:SerializeField] public TitleSo[] NeedTitle { get; private set; }
        
        [field:SerializeField] public int NeedCount { get; private set; }
        
        
    }
}