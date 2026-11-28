using System.Collections.Generic;
using System.Linq;
using _Scripts.Agent.Players;
using _Scripts.Agent.Players.TitleSystem;
using UnityEditor;
using UnityEngine;

namespace _Scripts.Editor.CheatEditor
{
    public class TitleCheatSection : ICheatSection
    {
        private readonly AbstractTitleSo[] _titles = AssetDatabase.FindAssets($"t:{nameof(AbstractTitleSo)}")
            .Select(AssetDatabase.GUIDToAssetPath)
            .Select(AssetDatabase.LoadAssetAtPath<AbstractTitleSo>)
            .Where(title => title != null)
            .OrderBy(title => title.name)
            .ToArray();

        private readonly string[] _titleNames;

        private int _selectedIndex;
        private string _message;

        public TitleCheatSection()
        {
            _titleNames = _titles
                .Select(title => string.IsNullOrEmpty(title.TitleName) ? title.name : $"{title.TitleName} ({title.name})")
                .ToArray();
        }

        public string Title => "칭호";

        public void Draw()
        {
            PlayerController player = Object.FindFirstObjectByType<PlayerController>();
            ITitleModule titleModule = player != null ? player.GetModule<ITitleModule>() : null;
            if (titleModule == null)
            {
                EditorGUILayout.LabelField("칭호 모듈을 가진 플레이어가 없습니다.");
                return;
            }

            if (_titles.Length == 0)
            {
                EditorGUILayout.LabelField("칭호 에셋이 없습니다.");
                return;
            }

            ITitleEquipModule equipModule = player.GetModule<ITitleEquipModule>();

            _selectedIndex = EditorGUILayout.Popup("칭호", _selectedIndex, _titleNames);
            AbstractTitleSo selected = _titles[_selectedIndex];
            Title title = Find(titleModule, selected);
            bool isEquipped = equipModule != null && equipModule.IsEquipped(selected);

            EditorGUILayout.LabelField("상태", GetStateText(title, isEquipped));

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(title is { IsComplete: true }))
                {
                    if (GUILayout.Button("획득"))
                        _message = TryAcquire(titleModule, selected, new HashSet<AbstractTitleSo>())
                            ? null
                            : "획득 실패 (TitleGroup에 없는 칭호이거나 퀘스트가 없음)";
                }

                using (new EditorGUI.DisabledScope(equipModule == null || title is not { IsComplete: true }))
                {
                    if (GUILayout.Button(isEquipped ? "해제" : "장착"))
                        _message = equipModule.TrySetEquipped(selected, !isEquipped) ? null : "장착 비용이 부족합니다.";
                }

                if (GUILayout.Button("모두 획득"))
                {
                    foreach (AbstractTitleSo titleSo in _titles)
                        TryAcquire(titleModule, titleSo, new HashSet<AbstractTitleSo>());
                    _message = null;
                }
            }

            if (!string.IsNullOrEmpty(_message))
                EditorGUILayout.HelpBox(_message, MessageType.Warning);

            EditorGUILayout.HelpBox("획득한 칭호는 도감 기록에 바로 저장됩니다.", MessageType.None);
        }

        public void Tick() { }

        public void Release() { }

        // 퀘스트를 가득 채워 일반 달성 경로(알림, 도감 기록, 후속 칭호 해금)를 그대로 탄다. 잠긴 칭호는 선행 칭호부터 달성한다.
        private static bool TryAcquire(ITitleModule titleModule, AbstractTitleSo titleSo, HashSet<AbstractTitleSo> visited)
        {
            if (Find(titleModule, titleSo) is { IsComplete: true }) return true;
            if (!visited.Add(titleSo)) return false;

            if (titleSo.RequiredTitles != null)
            {
                foreach (AbstractTitleSo required in titleSo.RequiredTitles)
                {
                    if (required != null)
                        TryAcquire(titleModule, required, visited);
                }
            }

            Title title = Find(titleModule, titleSo);
            if (title == null) return false;

            // 마지막 퀘스트를 채우면 칭호가 퀘스트 목록을 비우므로 복사본을 돈다.
            foreach (var quest in title.GetQuests().ToArray())
            {
                // 다른 스테이지의 퀘스트는 비활성이라 진행도가 바뀌지 않는다.
                quest.SetActive(true);
                quest.SetAmount(quest.RequiredAmount);
            }

            return title.IsComplete;
        }

        // 해금되지 않은 칭호는 목록에 없어 null이다.
        private static Title Find(ITitleModule titleModule, AbstractTitleSo titleSo)
            => titleModule.Titles.FirstOrDefault(title => title.TitleSo == titleSo);

        private static string GetStateText(Title title, bool isEquipped)
        {
            if (title == null) return "잠김";
            if (!title.IsComplete) return "진행 중";
            return isEquipped ? "장착 중" : "달성";
        }
    }
}
