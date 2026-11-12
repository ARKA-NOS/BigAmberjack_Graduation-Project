using System.Threading;
using UnityEngine.Networking;

namespace _Scripts._Core.DataSystem.GoogleSheetSystem
{
    /// <summary>
    /// CSV 내보내기 주소에서 시트 내용을 텍스트로 내려받는다.
    /// 응답이 올 때까지 호출한 스레드를 막는다. WebGL에서는 끝나지 않으므로 쓰지 않는다.
    /// </summary>
    public static class GoogleSheetDownloader
    {
        private const int DefaultTimeoutSeconds = 10;
        private const int PollIntervalMs = 10;

        public static bool TryDownloadCsv(string csvUrl, out string csv, out string error,
            int timeoutSeconds = DefaultTimeoutSeconds)
        {
            csv = null;
            error = null;

            using UnityWebRequest request = UnityWebRequest.Get(csvUrl);
            request.timeout = timeoutSeconds; // 시간이 지나면 요청이 실패로 끝나므로 아래 대기가 멈춘다.

            UnityWebRequestAsyncOperation operation = request.SendWebRequest();
            while (!operation.isDone)
                Thread.Sleep(PollIntervalMs);
            
            if (request.result != UnityWebRequest.Result.Success)
            {
                error = $"{request.error} (HTTP {request.responseCode})";
                return false;
            }

            // 비공개 시트는 CSV 대신 구글 로그인 페이지(HTML)로 넘어간다.
            string contentType = request.GetResponseHeader("Content-Type");
            if (contentType != null && contentType.Contains("text/html"))
            {
                error = "CSV 대신 웹 페이지를 받았습니다. 시트 공유가 '링크가 있는 모든 사용자'인지 확인하세요.";
                return false;
            }

            csv = request.downloadHandler.text; // UTF-8로 읽는다.
            return true;
        }
    }
}
