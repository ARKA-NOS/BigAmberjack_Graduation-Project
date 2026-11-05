using System.Collections.Generic;

namespace _Scripts.UI.PlayerUI.Title_UI
{
    public class TitleListViewData
    {
        public readonly List<TitleViewData> TitleViewList;

        public TitleListViewData(List<TitleViewData> titleViewList)
        {
            TitleViewList = titleViewList;
        }
        
    }
}