using System;
using UnityEngine;

namespace _Scripts._Core.LanguageSystem.Text
{
    [Serializable]
    public class TextSetting
    {
        [field: SerializeField] public Language Language { get; private set; }
        [field: SerializeField] public string Text { get; private set; }
    }
}