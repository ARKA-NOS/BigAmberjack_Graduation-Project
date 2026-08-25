using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.UIElements;

namespace Agents.FSM.Editor
{
    [CustomEditor(typeof(StateSo))]
    public class StateSoEditor : UnityEditor.Editor
    {
        [SerializeField] private VisualTreeAsset editorView = default;

        private StateSo _targetData;
        public override VisualElement CreateInspectorGUI()
        {
            _targetData = (StateSo)target; // 타겟은 Editor의 내부 변수이다.
            
            VisualElement root = new VisualElement();
            editorView.CloneTree(root);

            FillDropdownField(root);
            return root;
        }

        private void FillDropdownField(VisualElement root)
        {
            DropdownField field = root.Q<DropdownField>("ClassNameDropdown");
            Assembly stateAssembly = Assembly.GetAssembly(typeof(StateSo));
            IEnumerable<string> choices = stateAssembly.GetTypes()
                .Where(type => type.IsClass // Assembly에 있는 모든 타입을 갖고와
                               && !type.IsAbstract
                               && type.IsSubclassOf(typeof(AgentState)))
                .Select(type => type.FullName);
            
            field.choices.AddRange(choices);

            if (_targetData != null && !string.IsNullOrEmpty(_targetData.className)
                                    && field.choices.Contains(_targetData.className))
            {
                field.value = _targetData.className;
            }
            else if (_targetData != null && field.choices.Count > 0)
            {
                _targetData.className = field.choices.First();
                EditorUtility.SetDirty(_targetData);
            }

            AssetDatabase.SaveAssetIfDirty(_targetData);
        }
    }
}