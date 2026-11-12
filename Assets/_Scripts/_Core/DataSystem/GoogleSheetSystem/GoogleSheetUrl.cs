using System.Text.RegularExpressions;

namespace _Scripts._Core.DataSystem.GoogleSheetSystem
{
    /// <summary>
    /// 구글 시트 주소(편집·공유 링크, 웹에 게시 링크)를 CSV 내보내기 주소로 바꾼다.
    /// </summary>
    public static class GoogleSheetUrl
    {
        private const string BaseUrl = "https://docs.google.com/spreadsheets/d/";

        // 웹에 게시한 시트: .../spreadsheets/d/e/{게시 ID}/pubhtml
        private static readonly Regex PublishedIdRegex = new(@"docs\.google\.com/spreadsheets/d/e/([A-Za-z0-9_-]+)");

        // 일반 시트: .../spreadsheets/d/{시트 ID}/edit
        private static readonly Regex SheetIdRegex = new(@"docs\.google\.com/spreadsheets/d/([A-Za-z0-9_-]+)");

        // 탭 ID: ?gid=123, &gid=123, #gid=123
        private static readonly Regex GidRegex = new(@"[?&#]gid=(\d+)");

        /// <summary>
        /// 구글 시트 주소를 CSV 내보내기 주소로 바꾼다. 주소에 탭(gid)이 없으면 첫 번째 탭을 내보낸다.
        /// </summary>
        public static bool TryGetCsvUrl(string url, out string csvUrl)
        {
            csvUrl = null;
            if (string.IsNullOrWhiteSpace(url))
                return false;

            url = url.Trim();
            string gid = FindGid(url);

            // 게시 주소(/d/e/...)도 일반 주소 정규식에 걸리므로 먼저 확인한다.
            Match published = PublishedIdRegex.Match(url);
            if (published.Success)
            {
                csvUrl = $"{BaseUrl}e/{published.Groups[1].Value}/pub?output=csv";
                if (gid != null)
                    csvUrl += $"&gid={gid}&single=true";
                return true;
            }

            Match sheet = SheetIdRegex.Match(url);
            if (sheet.Success)
            {
                csvUrl = $"{BaseUrl}{sheet.Groups[1].Value}/export?format=csv";
                if (gid != null)
                    csvUrl += $"&gid={gid}";
                return true;
            }

            return false;
        }

        private static string FindGid(string url)
        {
            Match gid = GidRegex.Match(url);
            return gid.Success ? gid.Groups[1].Value : null;
        }
    }
}
