using LrwLib.UnityPosition;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace LrwLib.UnityPosition.Editor
{
    [CustomPropertyDrawer(typeof(UnityPos))]
    public sealed class UnityPosDrawer : PropertyDrawer
    {
        private const string PositionPropertyName = "position";
        private const float ButtonWidth = 150f;

        private static int activeTargetId;
        private static string activePropertyPath;

        static UnityPosDrawer()
        {
            SceneView.duringSceneGui -= DrawActivePositionHandle;
            SceneView.duringSceneGui += DrawActivePositionHandle;

            Selection.selectionChanged -= ClearHandleIfActiveTargetNotSelected;
            Selection.selectionChanged += ClearHandleIfActiveTargetNotSelected;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight * 2f + EditorGUIUtility.standardVerticalSpacing;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var positionProperty = property.FindPropertyRelative(PositionPropertyName);
            if (positionProperty == null)
            {
                EditorGUI.LabelField(position, label.text, $"Missing {PositionPropertyName} field");
                EditorGUI.EndProperty();
                return;
            }

            var valueRect = new Rect(
                position.x,
                position.y,
                position.width,
                EditorGUIUtility.singleLineHeight);

            var fieldRect = EditorGUI.PrefixLabel(
                valueRect,
                GUIUtility.GetControlID(FocusType.Passive),
                label);

            var buttonRect = new Rect(
                fieldRect.x,
                valueRect.yMax + EditorGUIUtility.standardVerticalSpacing,
                fieldRect.width,
                EditorGUIUtility.singleLineHeight);

            var previousIndentLevel = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            EditorGUI.PropertyField(fieldRect, positionProperty, GUIContent.none);
            EditorGUI.indentLevel = previousIndentLevel;

            var isActive = IsActive(property);
            var buttonLabel = isActive ? "Stop Position Handle" : "Edit Position Handle";

            if (buttonRect.width > ButtonWidth)
            {
                buttonRect.width = ButtonWidth;
            }

            if (GUI.Button(buttonRect, buttonLabel))
            {
                if (isActive)
                {
                    ClearActiveHandle();
                }
                else
                {
                    SetActiveHandle(property);
                }

                SceneView.RepaintAll();
            }

            EditorGUI.EndProperty();
        }

        private static bool IsActive(SerializedProperty property)
        {
            var target = property.serializedObject.targetObject;
            return target != null
                   && activeTargetId == target.GetInstanceID()
                   && activePropertyPath == property.propertyPath;
        }

        private static void SetActiveHandle(SerializedProperty property)
        {
            var target = property.serializedObject.targetObject;
            if (target == null)
            {
                ClearActiveHandle();
                return;
            }

            activeTargetId = target.GetInstanceID();
            activePropertyPath = property.propertyPath;
        }

        private static void ClearActiveHandle()
        {
            activeTargetId = 0;
            activePropertyPath = null;
        }

        private static void ClearHandleIfActiveTargetNotSelected()
        {
            if (activeTargetId == 0)
            {
                return;
            }

            var target = EditorUtility.EntityIdToObject(activeTargetId);
            if (target == null || !IsTargetSelected(target))
            {
                ClearActiveHandle();
                SceneView.RepaintAll();
            }
        }

        private static void DrawActivePositionHandle(SceneView sceneView)
        {
            if (activeTargetId == 0 || string.IsNullOrEmpty(activePropertyPath))
            {
                return;
            }

            var target = EditorUtility.EntityIdToObject(activeTargetId);
            if (target == null)
            {
                ClearActiveHandle();
                return;
            }

            if (!IsTargetSelected(target) || IsCollapsedComponent(target))
            {
                ClearActiveHandle();
                SceneView.RepaintAll();
                return;
            }

            var serializedObject = new SerializedObject(target);
            serializedObject.Update();

            var property = serializedObject.FindProperty(activePropertyPath);
            var positionProperty = property?.FindPropertyRelative(PositionPropertyName);
            if (positionProperty == null)
            {
                ClearActiveHandle();
                return;
            }

            var currentPosition = positionProperty.vector3Value;

            Handles.color = Color.cyan;
            Handles.SphereHandleCap(
                0,
                currentPosition,
                Quaternion.identity,
                HandleUtility.GetHandleSize(currentPosition) * 0.12f,
                EventType.Repaint);

            EditorGUI.BeginChangeCheck();
            var nextPosition = Handles.PositionHandle(currentPosition, Quaternion.identity);
            if (!EditorGUI.EndChangeCheck())
            {
                return;
            }

            Undo.RecordObject(target, "Move Unity Pos");
            positionProperty.vector3Value = nextPosition;
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
        }

        private static bool IsTargetSelected(Object target)
        {
            if (ContainsSelection(target))
            {
                return true;
            }

            return target is Component component && ContainsSelection(component.gameObject);
        }

        private static bool ContainsSelection(Object target)
        {
            foreach (var selectedObject in Selection.objects)
            {
                if (selectedObject == target)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsCollapsedComponent(Object target)
        {
            return target is Component && !InternalEditorUtility.GetIsInspectorExpanded(target);
        }
    }
}
