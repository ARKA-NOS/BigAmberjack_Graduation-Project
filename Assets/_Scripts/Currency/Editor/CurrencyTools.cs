using _Scripts._Core._SaveSystem;
using _Scripts._Core._SaveSystem.Data;
using UnityEditor;
using UnityEngine;

namespace _Scripts.Currency.Editor
{
    public static class CurrencyTools
    {
        private const string CurrencySaveName = "CurrencyData";

        [MenuItem("Tools/Delete CurrencyData")]
        private static void DeleteCurrencyData()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("Delete CurrencyData",
                    "플레이 모드를 끝낼 때 현재 재화가 다시 저장되므로, 플레이 모드가 아닐 때 실행하세요.", "확인");
                return;
            }

            SaveSystem.Delete<JsonDict<string, int>>(CurrencySaveName);
            Debug.Log("[CurrencyTools] 영구 재화 저장 데이터를 삭제했습니다.");
        }
    }
}
