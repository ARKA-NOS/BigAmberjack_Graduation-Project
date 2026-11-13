using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace _Scripts._Core.LanguageSystem.Font
{
    [CreateAssetMenu(fileName = "Font SO", menuName = "Language/Font SO", order = -1)]
    public class FontSettingSO : ScriptableObject
    {
        [SerializeField] private List<FontSetting> fontSettings;
        
        public TMP_FontAsset GetFont(Language language)
            => fontSettings.FirstOrDefault(x => x.Language == language, null)?
                .Font;
    }
}