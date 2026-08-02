using System;
using System.Collections.Generic;
using DungeonTeam.Feedback.Runtime;
using UnityEditor;
using UnityEngine;

namespace DungeonTeam.Feedback.Editor
{
    [CustomPropertyDrawer(typeof(FeedbackCue))]
    internal sealed class FeedbackCueDrawer : PropertyDrawer
    {
        private const float RemoveButtonWidth = 22f;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var height = EditorGUIUtility.singleLineHeight;
            if (!property.isExpanded)
            {
                return height;
            }

            var payloads = property.FindPropertyRelative("_payloads");
            for (var index = 0; index < payloads.arraySize; index++)
            {
                height += EditorGUI.GetPropertyHeight(
                              payloads.GetArrayElementAtIndex(index),
                              includeChildren: true) +
                          EditorGUIUtility.standardVerticalSpacing;
            }

            return height + EditorGUIUtility.singleLineHeight +
                   EditorGUIUtility.standardVerticalSpacing;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            var line = new Rect(
                position.x,
                position.y,
                position.width,
                EditorGUIUtility.singleLineHeight);
            property.isExpanded = EditorGUI.Foldout(
                line,
                property.isExpanded,
                label,
                toggleOnLabelClick: true);
            if (!property.isExpanded)
            {
                EditorGUI.EndProperty();
                return;
            }

            EditorGUI.indentLevel++;
            var payloads = property.FindPropertyRelative("_payloads");
            var y = line.yMax + EditorGUIUtility.standardVerticalSpacing;
            for (var index = 0; index < payloads.arraySize; index++)
            {
                var element = payloads.GetArrayElementAtIndex(index);
                var elementHeight = EditorGUI.GetPropertyHeight(element, includeChildren: true);
                var elementRect = new Rect(
                    position.x,
                    y,
                    position.width - RemoveButtonWidth - 2f,
                    elementHeight);
                var removeRect = new Rect(
                    elementRect.xMax + 2f,
                    y,
                    RemoveButtonWidth,
                    EditorGUIUtility.singleLineHeight);

                EditorGUI.PropertyField(
                    elementRect,
                    element,
                    new GUIContent(GetPayloadLabel(element)),
                    includeChildren: true);
                if (GUI.Button(removeRect, "×"))
                {
                    payloads.DeleteArrayElementAtIndex(index);
                    property.serializedObject.ApplyModifiedProperties();
                    break;
                }

                y += elementHeight + EditorGUIUtility.standardVerticalSpacing;
            }

            var addRect = new Rect(
                position.x,
                y,
                position.width,
                EditorGUIUtility.singleLineHeight);
            if (GUI.Button(addRect, "Add feedback payload"))
            {
                ShowPayloadMenu(property);
            }

            EditorGUI.indentLevel--;
            EditorGUI.EndProperty();
        }

        private static string GetPayloadLabel(SerializedProperty property)
        {
            var typeName = property.managedReferenceFullTypename;
            if (string.IsNullOrEmpty(typeName))
            {
                return "Missing payload";
            }

            var separator = typeName.LastIndexOf(' ');
            var fullName = separator >= 0 ? typeName[(separator + 1)..] : typeName;
            var namespaceSeparator = fullName.LastIndexOf('.');
            return namespaceSeparator >= 0
                ? fullName[(namespaceSeparator + 1)..]
                : fullName;
        }

        private static void ShowPayloadMenu(SerializedProperty cueProperty)
        {
            var menu = new GenericMenu();
            var types = new List<Type>(TypeCache.GetTypesDerivedFrom<FeedbackPayload>());
            types.RemoveAll(type => type.IsAbstract || type.IsGenericTypeDefinition);
            types.Sort((first, second) => string.CompareOrdinal(first.FullName, second.FullName));

            if (types.Count == 0)
            {
                menu.AddDisabledItem(new GUIContent("No payload types found"));
            }

            var serializedObject = cueProperty.serializedObject;
            var propertyPath = cueProperty.propertyPath;
            for (var index = 0; index < types.Count; index++)
            {
                var payloadType = types[index];
                menu.AddItem(
                    new GUIContent(ObjectNames.NicifyVariableName(payloadType.Name)),
                    false,
                    () => AddPayload(serializedObject, propertyPath, payloadType));
            }

            menu.ShowAsContext();
        }

        private static void AddPayload(
            SerializedObject serializedObject,
            string cuePropertyPath,
            Type payloadType)
        {
            serializedObject.Update();
            var cue = serializedObject.FindProperty(cuePropertyPath);
            var payloads = cue.FindPropertyRelative("_payloads");
            var index = payloads.arraySize;
            payloads.InsertArrayElementAtIndex(index);
            payloads.GetArrayElementAtIndex(index).managedReferenceValue =
                Activator.CreateInstance(payloadType);
            serializedObject.ApplyModifiedProperties();
        }
    }
}
