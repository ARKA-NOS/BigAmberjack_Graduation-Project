using System.Collections.Generic;
using _Scripts.UI.MVVM.Disposables;
using _Scripts.UI.MVVM.Observables;
using _Scripts.UI.MVVM.Views;
using UnityEngine;

namespace _Scripts.UI.PlayerUI.HUD_UI.Effect_UI_System.Effect_UI
{
    // HUD의 버프/디버프 아이콘 줄. 효과마다 PlayerEffectUI를 하나씩 만들어 이 오브젝트 아래에 둔다.
    public class PlayerEffectListView : ViewBase<EffectViewModel>
    {
        [SerializeField] private GameObject playerEffectUIPrefab;

        private readonly List<PlayerEffectUI> _items = new();

        protected override void OnBind(EffectViewModel vm)
        {
            vm.Effects.Subscribe(change => HandleEffectsChanged(vm.Effects, change)).AddTo(Disposables);
        }

        private void HandleEffectsChanged(IReadOnlyList<EffectViewData> effects, ListChange<EffectViewData> change)
        {
            // 개수가 그대로면 바뀐 칸만 다시 그린다.
            if (change.Kind == ListChangeKind.Replace)
            {
                if (change.Index < _items.Count)
                    _items[change.Index].SetViewData(change.NewItem);
                return;
            }

            EnsureCount(effects.Count);

            for (int i = 0; i < _items.Count; i++)
            {
                bool active = i < effects.Count;
                _items[i].gameObject.SetActive(active);
                if (active) _items[i].SetViewData(effects[i]);
            }
        }

        private void EnsureCount(int count)
        {
            if (playerEffectUIPrefab == null)
            {
                Debug.LogError($"[{name}] playerEffectUIPrefab is null", this);
                return;
            }

            while (_items.Count < count)
            {
                GameObject go = Instantiate(playerEffectUIPrefab, transform);
                if (!go.TryGetComponent(out PlayerEffectUI item))
                {
                    Debug.LogError($"[{name}] playerEffectUIPrefab에 PlayerEffectUI가 없습니다.", this);
                    Destroy(go);
                    return;
                }

                _items.Add(item);
            }
        }
    }
}
