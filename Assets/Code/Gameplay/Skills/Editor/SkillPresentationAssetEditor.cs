using System.Collections.Generic;
using DungeonTeam.Gameplay.Skills.Runtime;
using UnityEditor;
using UnityEngine;

namespace DungeonTeam.Gameplay.Skills.Editor
{
    [CustomEditor(typeof(SkillPresentationAsset))]
    internal sealed class SkillPresentationAssetEditor : UnityEditor.Editor
    {
        private static readonly Color AnimationColor = new(0.3f, 0.65f, 1f);
        private static readonly Color VfxColor = new(1f, 0.45f, 0.15f);

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(
                serializedObject.FindProperty("_animationCues"),
                includeChildren: true);
            EditorGUILayout.PropertyField(
                serializedObject.FindProperty("_vfxCues"),
                includeChildren: true);
            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space();
            DrawValidation();
            DrawTimeline();
        }

        private void DrawValidation()
        {
            var errors = new List<string>();
            ((SkillPresentationAsset)target).CollectValidationErrors(errors);
            if (errors.Count == 0)
            {
                EditorGUILayout.HelpBox("Sequence is valid.", MessageType.Info);
                return;
            }

            EditorGUILayout.HelpBox(string.Join("\n", errors), MessageType.Error);
        }

        private void DrawTimeline()
        {
            EditorGUILayout.LabelField("Sequence Timeline", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Commit timing is gameplay-authoritative and configured on the Skill level. " +
                "Cues here are visual offsets relative to lifecycle phases.",
                MessageType.None);

            var animations = serializedObject.FindProperty("_animationCues");
            var vfx = serializedObject.FindProperty("_vfxCues");
            var maxDelay = Mathf.Max(1f, FindMaxEnd(animations, isVfx: false),
                FindMaxEnd(vfx, isVfx: true));
            foreach (SkillPresentationPhase phase in
                     System.Enum.GetValues(typeof(SkillPresentationPhase)))
            {
                var row = EditorGUILayout.GetControlRect(false, 22f);
                var label = new Rect(row.x, row.y, 70f, row.height);
                var track = new Rect(row.x + 74f, row.y + 2f, row.width - 74f, row.height - 4f);
                EditorGUI.LabelField(label, phase.ToString());
                EditorGUI.DrawRect(track, new Color(0.12f, 0.12f, 0.12f, 0.35f));
                DrawMarkers(animations, phase, track, maxDelay, AnimationColor, isVfx: false);
                DrawMarkers(vfx, phase, track, maxDelay, VfxColor, isVfx: true);
            }

            EditorGUILayout.BeginHorizontal();
            DrawLegend(AnimationColor, "Animation");
            DrawLegend(VfxColor, "VFX");
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField($"0 - {maxDelay:0.##} s", GUILayout.Width(80f));
            EditorGUILayout.EndHorizontal();
        }

        private static float FindMaxEnd(SerializedProperty cues, bool isVfx)
        {
            var max = 0f;
            for (var index = 0; index < cues.arraySize; index++)
            {
                var cue = cues.GetArrayElementAtIndex(index);
                var delay = cue.FindPropertyRelative("_delay").floatValue;
                var lifetime = isVfx
                    ? cue.FindPropertyRelative("_lifetime").floatValue
                    : 0f;
                max = Mathf.Max(max, delay + lifetime);
            }

            return max;
        }

        private static void DrawMarkers(
            SerializedProperty cues,
            SkillPresentationPhase phase,
            Rect track,
            float maxDelay,
            Color color,
            bool isVfx)
        {
            for (var index = 0; index < cues.arraySize; index++)
            {
                var cue = cues.GetArrayElementAtIndex(index);
                if ((SkillPresentationPhase)cue.FindPropertyRelative("_phase").enumValueIndex != phase)
                    continue;

                var delay = cue.FindPropertyRelative("_delay").floatValue;
                var x = track.x + track.width * Mathf.Clamp01(delay / maxDelay);
                var width = isVfx
                    ? Mathf.Max(4f, track.width * Mathf.Max(0.01f,
                        cue.FindPropertyRelative("_lifetime").floatValue / maxDelay))
                    : 4f;
                EditorGUI.DrawRect(new Rect(x, track.y + 2f, width, track.height - 4f), color);
            }
        }

        private static void DrawLegend(Color color, string label)
        {
            var rect = GUILayoutUtility.GetRect(70f, 16f, GUILayout.Width(70f));
            EditorGUI.DrawRect(new Rect(rect.x, rect.y + 4f, 8f, 8f), color);
            EditorGUI.LabelField(new Rect(rect.x + 12f, rect.y, 58f, rect.height), label);
        }
    }
}
