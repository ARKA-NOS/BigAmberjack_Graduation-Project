using UnityEngine;

namespace _Scripts._Core.DataSystem
{
    [CreateAssetMenu(fileName = "Google Sheet", menuName = "Google Sheet", order = 0)]
    public class GoogleSheet : ScriptableObject
    {
        [SerializeField, TextArea] private string googleSheetURL;
        
        
        
        
    }
}