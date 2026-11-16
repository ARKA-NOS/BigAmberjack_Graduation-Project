using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace _Scripts._Core.LanguageSystem.Text
{
    [CreateAssetMenu(fileName = "Text SO", menuName = "Language/Text SO", order = -1)]
    public class TextSettingSO : ScriptableObject
    {
        [field: SerializeField] public List<TextSetting> TextSettings { get; private set; }
        
        public string GetText(Language language)
            => TextSettings.FirstOrDefault(x => x.Language == language, null)?
                .Text ?? "";
    }
}