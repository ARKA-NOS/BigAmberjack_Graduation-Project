using System;
using System.Collections.Generic;
using _Scripts.Settings;

namespace _Scripts.UI.SettingsUI
{
    // 순환 설정 행 하나의 정의. 옵션 목록, 표시 문구, SettingsData에서 값을 읽고 쓰는 방법을 묶는다.
    public interface ISettingOptionCycle
    {
        string GetLabel(SettingsData data);

        // 현재 값에서 direction(-1/+1)만큼 옮긴 값을 넣은 복사본을 돌려준다. 끝에서는 반대쪽 끝으로 넘어간다.
        SettingsData Step(SettingsData data, int direction);
    }

    public class SettingOptionCycle<T> : ISettingOptionCycle
    {
        private readonly Func<IReadOnlyList<T>> _options;
        private readonly Func<T, string> _label;
        private readonly Func<SettingsData, T> _get;
        private readonly Func<SettingsData, T, SettingsData> _set;

        public SettingOptionCycle(Func<IReadOnlyList<T>> options, Func<T, string> label,
            Func<SettingsData, T> get, Func<SettingsData, T, SettingsData> set)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _label = label ?? throw new ArgumentNullException(nameof(label));
            _get = get ?? throw new ArgumentNullException(nameof(get));
            _set = set ?? throw new ArgumentNullException(nameof(set));
        }

        public SettingOptionCycle(IReadOnlyList<T> options, Func<T, string> label,
            Func<SettingsData, T> get, Func<SettingsData, T, SettingsData> set)
            : this(() => options, label, get, set)
        {
        }

        public string GetLabel(SettingsData data) => _label(_get(data));

        public SettingsData Step(SettingsData data, int direction)
        {
            IReadOnlyList<T> options = _options();
            if (options == null || options.Count == 0) return data;

            // 목록에 없는 값(예: 저장된 해상도를 이 모니터에서 못 쓰는 경우)이면 첫 옵션부터 시작한다.
            int index = IndexOf(options, _get(data));
            int next = index < 0 ? 0 : Wrap(index + Math.Sign(direction), options.Count);
            return _set(data, options[next]);
        }

        private static int IndexOf(IReadOnlyList<T> options, T value)
        {
            EqualityComparer<T> comparer = EqualityComparer<T>.Default;
            for (int i = 0; i < options.Count; i++)
            {
                if (comparer.Equals(options[i], value)) return i;
            }

            return -1;
        }

        private static int Wrap(int index, int count) => (index % count + count) % count;
    }
}
