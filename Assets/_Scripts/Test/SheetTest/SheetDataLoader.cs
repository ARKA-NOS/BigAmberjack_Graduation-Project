using System.Linq;
using System.Text;
using _Scripts._Core;
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
            FDebug.Log("모든 칭호명 로드", this);
            Sheet sheet = googleSheet.GetSheetData();

            StringBuilder sb = new();
            sheet.Where(x => x.HasKey("칭호명")).Select(x => x["칭호명"]).Foreach(x => sb.AppendLine(x));
            FDebug.Log(sb, this);
        }
    }
}