using System;
using System.Collections.Generic;
using _Scripts._Core._Debug;
using _Scripts._Core._EventSystem;
using _Scripts._Core._Manager;
using _Scripts._Core._SaveSystem;
using _Scripts._Core._ServiceLocator;
using _Scripts.Agent.Players.TitleSystem;
using UnityEngine;

namespace _Scripts.TitleCodex
{
    // 칭호 달성 이력을 런을 넘어 저장한다. 칭호 모듈이 보내는 TitleCompletedEvent를 받아 기록하고 바로 저장한다.
    // 칭호는 에셋 이름으로 기록하므로 칭호 에셋 이름을 바꾸면 이전 기록과 이어지지 않는다.
    public class TitleRecordManager : AbstractManager, ITitleRecordManager
    {
        private const string SaveName = "TitleRecord";

        [SerializeField] private TitleCatalogSo catalog;

        private readonly HashSet<string> _earned = new();

        public event Action OnRecordChanged;

        public TitleCatalogSo Catalog => catalog;
        public int EarnedCount => CountEarnedInCatalog();

        public override void Initialize()
        {
            Load();
            EventBus<TitleCompletedEvent>.Subscribe(HandleTitleCompleted);
            ServiceLocator.Register<ITitleRecordManager>(this);
        }

        public override void GameEnd()
        {
            EventBus<TitleCompletedEvent>.UnSubscribe(HandleTitleCompleted);
            Save();
            ServiceLocator.Register<ITitleRecordManager>(new NullTitleRecordManager());
        }

        public bool HasEarned(AbstractTitleSo title) => title != null && _earned.Contains(title.name);

        private void HandleTitleCompleted(TitleCompletedEvent evt)
        {
            if (evt.Title == null || !_earned.Add(evt.Title.name)) return;

            Save();
            OnRecordChanged?.Invoke();
        }

        // 카탈로그에 없는 예전 칭호 기록은 개수에서 뺀다.
        private int CountEarnedInCatalog()
        {
            if (catalog == null) return 0;

            int count = 0;
            foreach (TitleCatalogEntry entry in catalog.Entries)
            {
                if (_earned.Contains(entry.Title.name)) count++;
            }
            return count;
        }

        private void Load()
        {
            _earned.Clear();
            TitleRecordData data = SaveSystem.Load(SaveName, new TitleRecordData());
            if (data?.earnedTitles == null) return;

            foreach (string titleName in data.earnedTitles)
            {
                if (!string.IsNullOrEmpty(titleName)) _earned.Add(titleName);
            }
        }

        private void Save()
        {
            var data = new TitleRecordData { earnedTitles = new List<string>(_earned) };
            if (!SaveSystem.Save(SaveName, data))
                FDebug.LogWarning("[TitleRecordManager] 칭호 달성 이력을 저장하지 못했습니다.");
        }

        [ContextMenu("Clear Title Record")]
        private void ClearRecord()
        {
            _earned.Clear();
            Save();
            OnRecordChanged?.Invoke();
        }
    }

    [Serializable]
    public class TitleRecordData
    {
        public List<string> earnedTitles = new();
    }
}
