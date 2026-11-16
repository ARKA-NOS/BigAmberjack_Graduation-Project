using System;
using System.Collections.Generic;
using _Scripts._Core._ServiceLocator;
using UnityEngine;

namespace _Scripts.Settings
{
    // GameManager가 없는 씬에서 쓰는 빈 구현. 기본값만 돌려주고 저장·적용은 하지 않는다.
    public class NullSettingsManager : ISettingsManager
    {
        private static readonly Vector2Int[] Resolutions = { SettingsData.Default.Resolution };

        public event Action<SettingsData> OnSettingsChanged { add { } remove { } }

        public SettingsData Settings => SettingsData.Default;
        public IReadOnlyList<Vector2Int> ResolutionOptions => Resolutions;

        public void UpdateSettings(SettingsData data) { }
        public void ResetToDefault() { }
        public void Save() { }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Initialize()
        {
            ServiceLocator.Register<ISettingsManager>(new NullSettingsManager());
        }
    }
}
