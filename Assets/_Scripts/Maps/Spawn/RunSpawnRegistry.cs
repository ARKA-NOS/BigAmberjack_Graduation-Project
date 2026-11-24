using System.Collections.Generic;
using _Scripts._Core._EventSystem;
using _Scripts.Agent.Players.InteractSystem;
using _Scripts.UI.RunEnd;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _Scripts.Maps.Spawn
{
    // 한 런에서 이미 나온 "런당 1회" 스폰 후보를 기억한다(InteractableSpawnEntry.UniquePerRun).
    // 런이 끝나거나(RunEndedEvent) 새 씬을 불러오면 비워서, 다음 런에서 다시 나올 수 있게 한다.
    public static class RunSpawnRegistry
    {
        private static readonly HashSet<AbstractInteractObject> Consumed = new();

        public static bool IsConsumed(AbstractInteractObject prefab) => prefab != null && Consumed.Contains(prefab);

        public static void MarkConsumed(AbstractInteractObject prefab)
        {
            if (prefab != null) Consumed.Add(prefab);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Init()
        {
            Consumed.Clear();
            // 도메인 리로드를 끄고 플레이를 다시 시작해도 중복 구독되지 않게 먼저 해제한다.
            EventBus<RunEndedEvent>.UnSubscribe(HandleRunEnded);
            EventBus<RunEndedEvent>.Subscribe(HandleRunEnded);
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private static void HandleRunEnded(RunEndedEvent evt) => Consumed.Clear();

        private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode == LoadSceneMode.Single) Consumed.Clear();
        }
    }
}
