using System;
using _Scripts.UI.MVVM.Disposables;
using _Scripts.UI.MVVM.Observables;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace _Scripts.UI.MVVM.Bindings
{
    // 자주 쓰는 uGUI 바인딩. 모두 해제 핸들을 돌려주므로 .AddTo(Disposables)로 등록한다.
    // 대상이 비어 있으면(인스펙터 연결 누락) 바로 예외를 던져서 OnBind에서 원인이 드러나게 한다.
    public static class UGuiBindingExtensions
    {
        public static IDisposable BindText(this IReadOnlyObservable<string> source, TMP_Text target)
        {
            RequireTarget(target, nameof(target));
            return source.Subscribe(value => target.text = value);
        }

        public static IDisposable BindText<T>(this IReadOnlyObservable<T> source, TMP_Text target, Func<T, string> format)
        {
            RequireTarget(target, nameof(target));
            if (format == null) throw new ArgumentNullException(nameof(format));
            return source.Subscribe(value => target.text = format(value));
        }

        // 0~1 값을 Image.fillAmount에 넣는다.
        public static IDisposable BindFill(this IReadOnlyObservable<float> source, Image target)
        {
            RequireTarget(target, nameof(target));
            return source.Subscribe(value => target.fillAmount = value);
        }

        // GameObject를 켜고 끈다. View 자신의 GameObject를 넣으면 꺼지는 순간 구독이 해제되어 다시 켜지지 않으므로 자식 오브젝트를 넣는다.
        public static IDisposable BindActive(this IReadOnlyObservable<bool> source, GameObject target)
        {
            RequireTarget(target, nameof(target));
            return source.Subscribe(value => target.SetActive(value));
        }

        // 오브젝트를 끄지 않고 CanvasGroup으로 보이기/숨기기. View 자신을 숨길 때 쓴다.
        public static IDisposable BindVisible(this IReadOnlyObservable<bool> source, CanvasGroup target)
        {
            RequireTarget(target, nameof(target));
            return source.Subscribe(value =>
            {
                target.alpha = value ? 1f : 0f;
                target.interactable = value;
                target.blocksRaycasts = value;
            });
        }

        // 버튼 클릭을 연결한다. 해제하면 리스너를 뺀다.
        public static IDisposable BindClick(this Button button, Action onClick)
        {
            RequireTarget(button, nameof(button));
            if (onClick == null) throw new ArgumentNullException(nameof(onClick));

            UnityAction listener = () => onClick();
            button.onClick.AddListener(listener);
            return Disposable.Create(() =>
            {
                if (button != null) button.onClick.RemoveListener(listener);
            });
        }

        private static void RequireTarget(UnityEngine.Object target, string paramName)
        {
            if (target == null) throw new ArgumentNullException(paramName);
        }
    }
}
