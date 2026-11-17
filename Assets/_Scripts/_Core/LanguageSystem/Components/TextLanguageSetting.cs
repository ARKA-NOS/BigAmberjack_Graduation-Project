using _Scripts._Core._EventSystem;
using _Scripts._Core.LanguageSystem.Font;
using _Scripts._Core.LanguageSystem.Text;
using TMPro;
using UnityEngine;

namespace _Scripts._Core.LanguageSystem.Components
{
    [RequireComponent(typeof(TMP_Text))]
    public class TextLanguageSetting : MonoBehaviour
    {
        [SerializeField] private FontSettingSO fontSo;
        [SerializeField] private TextSettingSO textSo;
        
        private TMP_Text _text;
        
        private void Awake()
        {
            _text = GetComponent<TMP_Text>();
            
            EventBus<LanguageChangeEvent>.Subscribe(HandleLanguageChange);
        }
        
        private void OnDestroy()
        {
            EventBus<LanguageChangeEvent>.UnSubscribe(HandleLanguageChange);
        }
        
        private void HandleLanguageChange(LanguageChangeEvent evt)
        {
            _text.font = fontSo?.GetFont(evt.Value);
            _text.text = textSo?.GetText(evt.Value) ?? "";
        }
        
    }
}