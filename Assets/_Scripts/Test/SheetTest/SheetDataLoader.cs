using _Scripts._Core._Debug;
using _Scripts._Core.DataSystem;
using _Scripts._Core.DataSystem.SheetSystem;
using UnityEngine;

namespace _Scripts.Test.SheetTest
{
    public class SheetDataLoader : MonoBehaviour
    {
        [SerializeField] private GoogleSheet googleSheet;

        [ContextMenu("Load")]
        public void Load()
        {
            FDebug.Log("첫번째 칭호명 로드");
            Sheet v = googleSheet.GetSheet();
            FDebug.Log($"{v[0]["칭호명"]}");
        }
    }
}