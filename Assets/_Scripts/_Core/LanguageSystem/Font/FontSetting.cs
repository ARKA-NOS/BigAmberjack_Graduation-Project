using System;
using TMPro;
using UnityEngine;

namespace _Scripts._Core.LanguageSystem.Font
{
    [Serializable]
    public class FontSetting
    {
        [field: SerializeField] public Language Language { get; private set; }
        [field: SerializeField] public TMP_FontAsset Font {get; private set;}
    }
}