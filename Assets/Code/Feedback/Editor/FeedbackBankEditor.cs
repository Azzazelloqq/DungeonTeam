using System;
using DungeonTeam.Feedback.Runtime.Banks;
using UnityEditor;
using UnityEngine;

namespace DungeonTeam.Feedback.Editor
{
    [CustomEditor(typeof(FeedbackBank), editorForChildClasses: true)]
    internal sealed class FeedbackBankEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space();
            if (!GUILayout.Button("Validate feedback bank"))
            {
                return;
            }

            try
            {
                ((FeedbackBank)target).Validate();
                Debug.Log($"Feedback bank '{target.name}' is valid.", target);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, target);
            }
        }
    }
}
