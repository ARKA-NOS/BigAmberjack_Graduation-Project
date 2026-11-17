using System;
using _Scripts.Loading;

namespace _Scripts.UI.LoadingUI
{
    // 로딩 ViewModel이 읽는 Model. 게임 어디서든 시작한 로딩(씬 이동, 맵 생성)의 상태와 진행도를 받는다.
    public interface ILoadingSource
    {
        bool IsLoading { get; }

        // 로딩이 처음 시작되거나 모두 끝날 때 온다.
        event Action<bool> LoadingChanged;

        // 로딩 단계 이름과 진행도가 바뀔 때마다 온다.
        event Action<LoadingData> ProgressChanged;
    }
}
