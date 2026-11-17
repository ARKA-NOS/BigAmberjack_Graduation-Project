using System.IO;
using UnityEditor;
using UnityEngine;

namespace _Scripts.Item.Editor
{
    // 메뉴 진입점. 실제 가져오기 로직은 ItemSheetImporter가 맡는다.
    public static class ItemSheetImportMenu
    {
        private const string CsvPath = "Assets/_Scripts/Item/ItemSheet/ItemSheet.csv";

        [MenuItem("KHO/Item/Import Item Sheet (CSV)")]
        private static void ImportItemSheet()
        {
            if (!File.Exists(CsvPath))
            {
                Debug.LogError($"[ItemSheet] CSV 파일이 없습니다: {CsvPath}");
                return;
            }

            ItemSheetImportResult result = ItemSheetImporter.Import(File.ReadAllText(CsvPath));

            Debug.Log($"[ItemSheet] 생성 {result.Created.Count}개, 갱신 {result.Updated.Count}개, 제외 {result.Skipped.Count}개");

            foreach (string skipped in result.Skipped)
            {
                Debug.LogWarning($"[ItemSheet] 제외: {skipped}");
            }

            foreach (string warning in result.Warnings)
            {
                Debug.LogWarning($"[ItemSheet] 경고: {warning}");
            }
        }
    }
}
