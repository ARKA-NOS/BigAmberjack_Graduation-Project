using System;
using System.Collections.Generic;

namespace SDW.Scripts.UI.Dialogue
{
    // 대화창에 표시할 내용. DialogueViewModel에 Bind하면 대화창이 이 내용으로 열린다.
    // 대화창을 닫을 때는 null을 Bind한다.
    public class DialogueData
    {
        public string Speaker { get; }
        public string Line { get; }
        // 비어 있으면 선택지 영역을 숨긴다.
        public IReadOnlyList<string> Choices { get; }
        // 선택지를 클릭하면 그 순서(index)로 호출된다.
        public Action<int> OnSelected { get; }

        public DialogueData(string speaker, string line, IReadOnlyList<string> choices = null, Action<int> onSelected = null)
        {
            Speaker = speaker;
            Line = line;
            Choices = choices ?? Array.Empty<string>();
            OnSelected = onSelected;
        }
    }
}
