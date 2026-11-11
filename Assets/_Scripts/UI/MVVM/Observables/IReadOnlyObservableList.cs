using System;
using System.Collections.Generic;

namespace _Scripts.UI.MVVM.Observables
{
    // ViewModel이 View에 노출하는 읽기 전용 목록. View는 Subscribe로 바뀐 칸만 받는다.
    public interface IReadOnlyObservableList<T> : IReadOnlyList<T>
    {
        // invokeResetNow가 true면 구독하는 즉시 Reset을 한 번 보내서, View가 현재 목록으로 전부 그리게 한다.
        IDisposable Subscribe(Action<ListChange<T>> onChanged, bool invokeResetNow = true);
    }
}
