using System.IO;
using System.Linq;
using _Scripts._Core._EventSystem;
using _Scripts._Core.SceneSystem;
using _Scripts.Maps.New_Map;
using UnityEditor;
using UnityEngine;

namespace _Scripts.Editor.CheatEditor
{
    public class StageCheatSection : ICheatSection
    {
        public string Title => "스테이지 / 시간";

        public void Draw()
        {
            // 맵 이동 오브젝트와 같은 이벤트를 보낸다. MapManager가 없는 씬에서는 아무 일도 없다.
            if (GUILayout.Button("다음 맵으로"))
                EventBus<MapMoveEvent>.Raise(new MapMoveEvent());

            EditorGUILayout.LabelField("씬 이동");
            using (new EditorGUI.DisabledScope(SceneLoader.IsLoadingScene))
            {
                foreach (string sceneName in EditorBuildSettings.scenes
                             .Where(scene => scene.enabled)
                             .Select(scene => Path.GetFileNameWithoutExtension(scene.path)))
                {
                    if (GUILayout.Button(sceneName))
                        SceneLoader.LoadScene(sceneName);
                }
            }

            float timeScale = EditorGUILayout.Slider("게임 속도", Time.timeScale, 0f, 5f);
            if (!Mathf.Approximately(timeScale, Time.timeScale))
                Time.timeScale = timeScale;
        }

        public void Tick() { }

        public void Release() { }
    }
}
