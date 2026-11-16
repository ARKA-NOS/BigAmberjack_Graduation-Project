using System;
using System.Collections.Generic;
using _Scripts._Core._Debug;
using _Scripts._Core._Manager;
using _Scripts._Core._SaveSystem;
using _Scripts._Core._ServiceLocator;
using _Scripts.Settings.Appliers;
using UnityEngine;

namespace _Scripts.Settings
{
    // 설정 값을 불러오고, 바뀌면 각 Applier로 게임에 반영한 뒤 저장한다.
    // GameManager는 런이 끝날 때 파괴되고 다시 만들어지므로, GameEnd에서 저장하고 다음 Initialize에서 다시 불러온다.
    public class SettingsManager : AbstractManager, ISettingsManager
    {
        private const string SaveName = "Settings";

        private static readonly Vector2Int[] CandidateResolutions =
        {
            new(1280, 720),
            new(1920, 1080),
            new(2560, 1440)
        };

        private readonly List<Vector2Int> _resolutionOptions = new();
        private ISettingsApplier[] _appliers = Array.Empty<ISettingsApplier>();

        public event Action<SettingsData> OnSettingsChanged;

        public SettingsData Settings { get; private set; } = SettingsData.Default;
        public IReadOnlyList<Vector2Int> ResolutionOptions => _resolutionOptions;

        public override void Initialize()
        {
            _appliers = CreateAppliers();
            BuildResolutionOptions();

            Settings = SaveSystem.Load(SaveName, SettingsData.Default).Sanitized();
            ApplyAll();

            ServiceLocator.Register<ISettingsManager>(this);
        }

        public override void GameEnd()
        {
            base.GameEnd();
            Save();
            ServiceLocator.Register<ISettingsManager>(new NullSettingsManager());
        }

        public void UpdateSettings(SettingsData data)
        {
            data = data.Sanitized();
            if (data.Equals(Settings)) return;

            Settings = data;
            ApplyAll();
            OnSettingsChanged?.Invoke(Settings);
        }

        public void ResetToDefault() => UpdateSettings(SettingsData.Default);

        public void Save()
        {
            if (!SaveSystem.Save(SaveName, Settings))
                FDebug.LogWarning("[SettingsManager] 설정 저장 실패");
        }

        // 게임에 반영할 대상 목록. 새 대상은 ISettingsApplier 구현을 만들어 여기에 추가한다.
        protected virtual ISettingsApplier[] CreateAppliers() => new ISettingsApplier[]
        {
            new DisplaySettingsApplier(),
            new AudioSettingsApplier(),
            new CameraShakeSettingsApplier()
        };

        private void ApplyAll()
        {
            foreach (ISettingsApplier applier in _appliers)
            {
                try
                {
                    applier.Apply(Settings);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

        // 모니터보다 큰 해상도는 뺀다. 모니터가 아주 작아 하나도 안 남으면 가장 작은 것만 둔다.
        private void BuildResolutionOptions()
        {
            _resolutionOptions.Clear();
            Resolution display = Screen.currentResolution;

            foreach (Vector2Int resolution in CandidateResolutions)
            {
                if (resolution.x <= display.width && resolution.y <= display.height)
                    _resolutionOptions.Add(resolution);
            }

            if (_resolutionOptions.Count == 0)
                _resolutionOptions.Add(CandidateResolutions[0]);
        }
    }
}
