using UnityEditor;
using UnityEngine;

namespace _Scripts.Editor.CheatEditor
{
    public class CheatWindow : EditorWindow
    {
        private ICheatSection[] _sections;
        private Vector2 _scroll;

        [MenuItem("Tools/Cheat")]
        private static void Open()
        {
            GetWindow<CheatWindow>("Cheat");
        }

        private void OnEnable()
        {
            _sections = new ICheatSection[]
            {
                new PlayerCheatSection(),
                new CurrencyCheatSection(),
                new ItemCheatSection(),
                new TitleCheatSection(),
                new SpawnCheatSection(),
                new EnemyCheatSection(),
                new StageCheatSection()
            };
        }

        private void OnDisable()
        {
            if (!EditorApplication.isPlaying) return;

            foreach (ICheatSection section in _sections)
                section.Release();
        }

        // 초당 10번 호출된다. 플레이 중 값 표시와 유지형 치트를 갱신한다.
        private void OnInspectorUpdate()
        {
            if (!EditorApplication.isPlaying) return;

            foreach (ICheatSection section in _sections)
                section.Tick();

            Repaint();
        }

        private void OnGUI()
        {
            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox("플레이 모드에서 사용할 수 있습니다.", MessageType.Info);
                return;
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            foreach (ICheatSection section in _sections)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField(section.Title, EditorStyles.boldLabel);
                section.Draw();
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
            }

            EditorGUILayout.EndScrollView();
        }
    }
}
