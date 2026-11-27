using System.Collections.Generic;
using _Scripts.UI.MVVM.Disposables;
using _Scripts.UI.MVVM.Views;
using TMPro;
using UnityEngine;

namespace _Scripts.UI.PlayerUI.Title_UI.Equip
{
    // 칭호 목록을 그린다. 칭호마다 TitleEquipRowUI를 listParent 아래에 만든다.
    // 보여줄 칭호가 없으면 안내(emptyHint)를 켜고, 검색어가 있으면 검색 결과 없음 문구를 보여 준다.
    // 목록 위 요약("달성 3 · 기록 1 / 전체 6")도 이 View가 그린다.
    public class TitleEquipListView : ViewBase<TitleEquipViewModel>
    {
        [SerializeField] private TitleEquipRowUI rowPrefab;
        [Tooltip("칭호 줄을 만들 부모. 비워 두면 이 오브젝트.")]
        [SerializeField] private Transform listParent;
        [Tooltip("보여줄 칭호가 없을 때 켤 안내. 없으면 비워 둔다.")]
        [SerializeField] private GameObject emptyHint;
        [SerializeField] private TMP_Text emptyText;
        [Tooltip("\"달성 n · 기록 n / 전체 n\"을 보여 줄 글자. 없으면 비워 둔다.")]
        [SerializeField] private TMP_Text summaryText;
        [SerializeField] private string noResultMessage = "검색 결과가 없습니다.";
        [SerializeField] private string emptyMessage = "해당하는 칭호가 없습니다.";

        private readonly List<TitleEquipRowUI> _rows = new();

        protected override void OnBind(TitleEquipViewModel vm)
        {
            if (rowPrefab == null)
            {
                Debug.LogError($"[{name}] rowPrefab이 없습니다.", this);
                return;
            }

            vm.Rows.Subscribe(_ => Redraw()).AddTo(Disposables);
            vm.Summary.Subscribe(ShowSummary).AddTo(Disposables);
        }

        private void Redraw()
        {
            IReadOnlyList<TitleEquipRowData> list = ViewModel.Rows;
            Transform parent = listParent != null ? listParent : transform;

            for (int i = _rows.Count; i < list.Count; i++)
                _rows.Add(Instantiate(rowPrefab, parent));

            for (int i = 0; i < _rows.Count; i++)
            {
                bool shown = i < list.Count;
                _rows[i].gameObject.SetActive(shown);
                if (shown) _rows[i].SetData(list[i]);
            }

            if (emptyHint != null)
                emptyHint.SetActive(list.Count == 0);

            if (emptyText != null)
                emptyText.text = ViewModel.Search.Value.Trim().Length > 0 ? noResultMessage : emptyMessage;
        }

        private void ShowSummary(TitleEquipSummary summary)
        {
            if (summaryText != null)
                summaryText.text = $"달성 {summary.Owned} · 기록 {summary.Recorded} / 전체 {summary.Total}";
        }
    }
}
