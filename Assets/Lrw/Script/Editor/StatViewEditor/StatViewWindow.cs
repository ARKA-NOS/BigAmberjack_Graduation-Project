using Lrw.Script.Agent.StatSystem;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Lrw.Script.Editor.StatViewEditor
{
    public class StatViewWindow : EditorWindow
    {
        private const long RefreshIntervalMs = 100;

        [SerializeField] private StatModule statModule;

        private ObjectField _targetField;
        private HelpBox _messageBox;
        private StatTableView _tableView;

        [MenuItem("Tools/Stat Viewer")]
        private static void Open()
        {
            GetWindow<StatViewWindow>("Stat Viewer");
        }

        private void OnEnable()
        {
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
        }

        public void CreateGUI()
        {
            _targetField = new ObjectField("Target")
            {
                objectType = typeof(StatModule),
                allowSceneObjects = true,
                value = statModule
            };
            _targetField.RegisterValueChangedCallback(evt =>
            {
                statModule = evt.newValue as StatModule;
                Refresh();
            });

            _messageBox = new HelpBox(string.Empty, HelpBoxMessageType.Info);
            _tableView = new StatTableView();

            rootVisualElement.Add(_targetField);
            rootVisualElement.Add(_messageBox);
            rootVisualElement.Add(_tableView);

            // 플레이 중 값이 계속 바뀌므로 이벤트 구독 대신 주기적으로 다시 읽는다
            rootVisualElement.schedule.Execute(Refresh).Every(RefreshIntervalMs);
            Refresh();
        }

        private void HandlePlayModeStateChanged(PlayModeStateChange state)
            => Refresh();

        private void Refresh()
        {
            if (_tableView == null) return;

            ResolveTarget();
            if (!ReferenceEquals(_targetField.value, statModule)) _targetField.SetValueWithoutNotify(statModule);

            string message = GetStateMessage();
            bool showTable = message == null;

            _messageBox.text = message;
            _messageBox.style.display = showTable ? DisplayStyle.None : DisplayStyle.Flex;
            _tableView.style.display = showTable ? DisplayStyle.Flex : DisplayStyle.None;
            _tableView.SetStats(showTable ? statModule.GetStats() : null);
        }

        // 플레이 모드 진입/종료로 씬이 다시 로드되면 기존 참조가 끊기므로 같은 ID의 오브젝트로 다시 연결한다
        private void ResolveTarget()
        {
            if (statModule is null || statModule != null) return;
            statModule = EditorUtility.EntityIdToObject(statModule.GetEntityId()) as StatModule;
        }

        private string GetStateMessage()
        {
            if (statModule == null)
                return "StatModule을 넣어주세요.";
            if (EditorUtility.IsPersistent(statModule))
                return "프리팹 에셋은 초기화되지 않습니다. 씬에 있는 인스턴스를 넣어주세요.";
            if (!EditorApplication.isPlaying)
                return "플레이 모드에서 초기화된 뒤 표시됩니다.";
            if (statModule.GetStats().Count == 0)
                return "등록된 스탯이 없습니다. StatGroup을 확인하세요.";
            return null;
        }
    }
}
