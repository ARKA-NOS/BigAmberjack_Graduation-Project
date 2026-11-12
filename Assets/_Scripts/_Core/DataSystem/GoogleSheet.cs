using System;
using System.Collections.Generic;
using System.Text;
using _Scripts._Core._Debug;
using _Scripts._Core.DataSystem.GoogleSheetSystem;
using _Scripts._Core.DataSystem.SheetSystem;
using UnityEngine;

namespace _Scripts._Core.DataSystem
{
    [CreateAssetMenu(fileName = "Google Sheet", menuName = "Google Sheet", order = 0)]
    public class GoogleSheet : ScriptableObject
    {
        [SerializeField, TextArea] private string googleSheetURL;
        
        public Sheet GetSheetData()
        {
            string csvString = GetGoogleSheetData(googleSheetURL);
            return new Sheet(csvString);
        }
        
        //sheet를 csv로 변환하고 그걸 텍스트화 하여 반환한다.
        private string GetGoogleSheetData(string url)
        {
            if (!GoogleSheetUrl.TryGetCsvUrl(url, out string csvUrl))
            {
                FDebug.LogError($"[GoogleSheet] 구글 시트 주소가 아닙니다: {url}", this);
                return string.Empty;
            }

            if (!GoogleSheetDownloader.TryDownloadCsv(csvUrl, out string csv, out string error))
            {
                FDebug.LogError($"[GoogleSheet] 시트를 내려받지 못했습니다: {error}\n{csvUrl}", this);
                return string.Empty;
            }

            return csv;
        }
        
#if UNITY_EDITOR
        [ContextMenu("Reload Sheet")]
        private void ReloadSheet()
        {
            StringBuilder log = new();
            int rowCount = 0;

            foreach (RowData row in GetSheetData())
            {
                rowCount++;
                foreach (KeyValuePair<string, string> cell in row)
                    log.Append($"{cell.Key}={cell.Value}  ");
                log.AppendLine();
            }

            Debug.Log($"[GoogleSheet] {name}: {rowCount}행\n{log}", this);
        }
#endif
    }
}
