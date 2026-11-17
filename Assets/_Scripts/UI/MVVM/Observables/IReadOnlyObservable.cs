using System;

namespace _Scripts.UI.MVVM.Observables
{
    // ViewModel이 View에 노출하는 읽기 전용 값. View는 Subscribe로 변경을 받는다.
    public interface IReadOnlyObservable<T>
    {
        T Value { get; }

        // 값이 바뀔 때마다 새 값으로 호출한다.
        // invokeNow가 true면 구독하는 즉시 현재 값으로 한 번 호출해서, 구독 시점과 상관없이 화면이 최신 상태가 된다.
        IDisposable Subscribe(Action<T> onChanged, bool invokeNow = true);

        // 이전 값과 새 값을 함께 받는다(피격 연출처럼 증감이 필요할 때).
        // invokeNow가 true면 (현재 값, 현재 값)으로 한 번 호출한다.
        IDisposable Subscribe(Action<T, T> onChanged, bool invokeNow = true);
    }
}
