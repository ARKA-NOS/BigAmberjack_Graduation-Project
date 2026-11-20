using System;
using _Scripts._Core._ServiceLocator;
using _Scripts.Agent.Players.TitleSystem;
using UnityEngine;

namespace _Scripts.TitleCodex
{
    // GameManager가 없는 씬에서 쓰는 빈 구현. 기록도 카탈로그도 없다.
    public class NullTitleRecordManager : ITitleRecordManager
    {
        public event Action OnRecordChanged { add { } remove { } }

        public TitleCatalogSo Catalog => null;
        public int EarnedCount => 0;

        public bool HasEarned(AbstractTitleSo title) => false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Initialize()
        {
            ServiceLocator.Register<ITitleRecordManager>(new NullTitleRecordManager());
        }
    }
}
