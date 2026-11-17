using _Scripts._Core._Debug;
using _Scripts.DataStructure;
using _Scripts.Loading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _Scripts._Core.SceneSystem
{
    /*
     * 로딩창을 띄우고 씬을 비동기로 바꾼다.
     * 로딩 시작 → 로딩창이 화면을 덮을 때까지 대기 → 씬 로드(진행도 표시) → 한 프레임 대기 → 씬 로딩 끝
     * 새 씬에서 Awake/Start에 시작한 로딩(맵 생성 등)이 있으면 그 로딩이 끝날 때까지 로딩창이 남는다.
     * 정적 UniTask라 이전 씬의 오브젝트가 파괴돼도 끝까지 진행된다.
     */
    public static class SceneLoader
    {
        private const string SceneLoadingText = "Scene";

        // LoadingView의 페이드 인 시간(기본 fadeInSpeed 4 = 0.25초)과 맞춘다. 이 시간 동안은 이전 씬이 로딩창 뒤에 그대로 보인다.
        private const float ScreenCoverSeconds = 0.25f;

        // allowSceneActivation을 끄지 않으면 progress는 0.9 다음에 바로 완료된다.
        private const float SceneLoadProgressMax = 0.9f;

        public static bool IsLoadingScene { get; private set; }

        public static void LoadScene(string sceneName) => LoadSceneAsync(sceneName).Forget();

        public static async UniTask LoadSceneAsync(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                FDebug.LogError("[SceneLoader] sceneName is empty");
                return;
            }

            // 포탈을 여러 번 눌러도 씬은 한 번만 바뀐다.
            if (IsLoadingScene)
            {
                FDebug.LogWarning($"[SceneLoader] 이미 씬을 불러오는 중이라 {sceneName} 이동을 무시합니다.");
                return;
            }

            IsLoadingScene = true;
            using LoadingScope loading = LoadingEvents.BeginLoading();

            try
            {
                SetLoadingGauge(0f);
                await UniTask.Delay(System.TimeSpan.FromSeconds(ScreenCoverSeconds), ignoreTimeScale: true);

                AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
                if (op == null)
                {
                    FDebug.LogError($"[SceneLoader] {sceneName} 씬을 불러올 수 없습니다. 빌드 세팅에 씬이 있는지 확인하세요.");
                    return;
                }

                while (!op.isDone)
                {
                    SetLoadingGauge(op.progress / SceneLoadProgressMax);
                    await UniTask.Yield();
                }

                SetLoadingGauge(1f);

                // 새 씬의 Start에서 시작하는 로딩도 이 로딩이 끝나기 전에 잡히도록 한 프레임 기다린다.
                await UniTask.Yield();
            }
            finally
            {
                IsLoadingScene = false;
            }
        }

        private static void SetLoadingGauge(float normalized)
            => LoadingEvents.RaiseSetLoadingGauge(SceneLoadingText, new Gauge(Mathf.Clamp01(normalized), 1f));

        // 도메인 리로드 없이 플레이를 다시 시작해도 이전 플레이의 상태가 남지 않게 한다.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay() => IsLoadingScene = false;
    }
}
