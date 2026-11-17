using System.Collections.Generic;
using _Scripts._Core._EventSystem;
using _Scripts.DataStructure;
using UnityEngine;

namespace _Scripts.Loading
{
    // 로딩 상태와 진행도를 알리는 정적 진입점. 로딩하는 쪽(씬 이동, 맵 생성)은 이 클래스만 알고 로딩 UI는 모른다.
    // 메인 스레드에서만 호출한다(구독한 UI가 바로 화면을 바꾼다).
    public static class LoadingEvents
    {
        private static readonly SetLoadingGaugeEvent SetLoadingGauge = new();
        private static readonly LoadingStateChangedEvent LoadingStateChanged = new();

        // 진행 중인 로딩. 하나라도 남아 있으면 로딩 중이다.
        // 씬 이동 로딩이 끝나기 전에 새 씬의 맵 생성 로딩이 시작되므로, 둘 사이에 로딩창이 꺼지지 않고 이어진다.
        private static readonly HashSet<LoadingScope> ActiveScopes = new();

        public static bool IsLoading => ActiveScopes.Count > 0;

        public static void RaiseSetLoadingGauge(string name, Gauge gauge)
        {
            LoadingData loadingData = new(name, gauge);
            EventBus<SetLoadingGaugeEvent>.Raise(SetLoadingGauge.Init(loadingData));
        }

        // 로딩 하나를 시작한다. 돌려받은 LoadingScope를 Dispose하면 그 로딩이 끝난다.
        // 모든 로딩이 끝나야 로딩창이 사라진다.
        public static LoadingScope BeginLoading()
        {
            LoadingScope scope = new();
            ActiveScopes.Add(scope);

            if (ActiveScopes.Count == 1)
                RaiseLoadingStateChanged(true);

            return scope;
        }

        internal static void EndLoading(LoadingScope scope)
        {
            if (!ActiveScopes.Remove(scope)) return;

            if (ActiveScopes.Count == 0)
                RaiseLoadingStateChanged(false);
        }

        private static void RaiseLoadingStateChanged(bool isLoading)
            => EventBus<LoadingStateChangedEvent>.Raise(LoadingStateChanged.Init(isLoading));

        // 도메인 리로드 없이 플레이를 다시 시작해도 이전 플레이의 로딩이 남지 않게 한다.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay() => ActiveScopes.Clear();
    }

    public class SetLoadingGaugeEvent : TemplateEvent<SetLoadingGaugeEvent, LoadingData>
    { }

    // 로딩 중 여부가 바뀔 때(처음 시작, 모두 끝남)만 온다.
    public class LoadingStateChangedEvent : TemplateEvent<LoadingStateChangedEvent, bool>
    { }
}
