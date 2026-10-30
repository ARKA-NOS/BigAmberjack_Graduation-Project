using System.Collections.Generic;
using Lrw.Script._Core._Debug;
using Lrw.Script.Agent.Players;
using Lrw.Script.UI.BindSystem;
using UnityEngine;

namespace Lrw.Script.UI.PlayerUI.Title_UI
{
    public class TitleUIBinder : AbstractUIBinder<TitleListViewData>
    {
        [SerializeField] private PlayerInputSo inputSo;
        
        [SerializeField] private TitleItemUI titleItemPrefab;
        private List<TitleItemUI> _titleItemUIs;
        private HashSet<string> _openTitles;

        protected override void Awake()
        {
            base.Awake();
            
            _titleItemUIs = new();
            _openTitles = new();

            if (titleItemPrefab == null)
            {
                FDebug.LogError("[TitleUIBinder] titleItemPrefab is null");
            }
        }
        
        protected override void DataUpdate()
        {
            if (CurrentValue?.TitleViewList == null || titleItemPrefab == null) return;

            CurrentValue.TitleViewList.Sort(CompareViewData);

            List<TitleViewData> list = CurrentValue.TitleViewList;

            SecuringTitleItemUI(list.Count);

            UpdateUI(list);
        }

        private void SecuringTitleItemUI(int count)
        {
            for (int i = _titleItemUIs.Count; i < count; i++)
            {
                TitleItemUI titleItemUI = Instantiate(titleItemPrefab, transform);
                titleItemUI.OnHeaderClick += ToggleOpen;
                _titleItemUIs.Add(titleItemUI);
            }
        }

        private void UpdateUI(List<TitleViewData> list)
        {
            for (int i = 0; i < _titleItemUIs.Count; i++)
            {
                if (i < list.Count)
                {
                    _titleItemUIs[i].gameObject.SetActive(true);
                    _titleItemUIs[i].SetViewData(list[i], _openTitles.Contains(list[i].TitleName));
                }
                else
                {
                    _titleItemUIs[i].gameObject.SetActive(false);
                }
            }
        }

        // 목록이 매 프레임 진행도순으로 다시 정렬되므로 펼침 상태는 줄 순서가 아니라 칭호 이름으로 기억한다.
        private void ToggleOpen(TitleItemUI titleItemUI)
        {
            if (!_openTitles.Remove(titleItemUI.TitleName))
                _openTitles.Add(titleItemUI.TitleName);
        }
        
        #region Helper

        private static int CompareViewData(TitleViewData x, TitleViewData y)
            => x.Progress.CompareTo(y.Progress);

        #endregion
        
    }
}
