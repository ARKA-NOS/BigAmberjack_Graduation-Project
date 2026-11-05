using UnityEngine;

namespace KHO.Scripts.UI.Bookmark
{
    // 책갈피와 페이지를 같은 순서로 짝지어, 누른 책갈피의 페이지만 켠다.
    public class BookmarkTabGroup : MonoBehaviour
    {
        [SerializeField] private BookmarkTab[] tabs;
        [SerializeField] private GameObject[] pages;
        [SerializeField] private int startIndex;

        public int CurrentIndex { get; private set; } = -1;

        private void Awake()
        {
            foreach (BookmarkTab tab in tabs)
                tab.OnClicked += HandleTabClicked;
        }

        private void Start()
        {
            Select(startIndex);
        }

        private void OnDestroy()
        {
            foreach (BookmarkTab tab in tabs)
            {
                if (tab != null) tab.OnClicked -= HandleTabClicked;
            }
        }

        public void Select(int index)
        {
            if (index < 0 || index >= pages.Length) return;

            CurrentIndex = index;
            for (int i = 0; i < pages.Length; i++)
                pages[i].SetActive(i == index);
            for (int i = 0; i < tabs.Length; i++)
                tabs[i].SetSelected(i == index);
        }

        private void HandleTabClicked(BookmarkTab tab)
        {
            Select(System.Array.IndexOf(tabs, tab));
        }
    }
}
