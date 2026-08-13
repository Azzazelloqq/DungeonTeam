using System.Collections.Generic;
using DungeonTeam.Gameplay.Skills.Runtime;
using UnityEditor;
using UnityEngine;

namespace DungeonTeam.Gameplay.Skills.Editor
{
    [CustomEditor(typeof(SkillPresentationAsset))]
    internal sealed class SkillPresentationAssetEditor : UnityEditor.Editor
    {
        private const float PhaseLabelWidth = 64f;
        private const float CueLabelWidth = 150f;
        private const float TrackHeight = 18f;

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
                "Each cue has its own track. Delay is its start time relative to the phase; " +
                "Lifetime is the bar length, so bars may overlap.",
                MessageType.None);

            var animations = serializedObject.FindProperty("_animationCues");
            var vfx = serializedObject.FindProperty("_vfxCues");
            var maxDelay = Mathf.Max(1f, FindMaxEnd(animations, isVfx: false),
                FindMaxEnd(vfx, isVfx: true));

            DrawTimeRuler(maxDelay);
            foreach (SkillPresentationPhase phase in
                     System.Enum.GetValues(typeof(SkillPresentationPhase)))
            {
                DrawPhaseTracks(animations, vfx, phase, maxDelay);
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

        private static void DrawTimeRuler(float duration)
        {
            var row = EditorGUILayout.GetControlRect(false, TrackHeight);
            var track = new Rect(
                row.x + PhaseLabelWidth + CueLabelWidth,
                row.y,
                row.width - PhaseLabelWidth - CueLabelWidth,
                row.height);
            if (track.width <= 0f)
                return;

            for (var index = 0; index <= 4; index++)
            {
                var normalized = index / 4f;
                var x = track.x + track.width * normalized;
                EditorGUI.DrawRect(new Rect(x, track.yMax - 4f, 1f, 4f), Color.gray);
                EditorGUI.LabelField(
                    new Rect(x - 18f, track.y, 42f, track.height - 3f),
                    $"{duration * normalized:0.##}",
                    EditorStyles.miniLabel);
            }
        }

        private static void DrawPhaseTracks(
            SerializedProperty animations,
            SerializedProperty vfx,
            SkillPresentationPhase phase,
            float duration)
        {
            var rowIndex = 0;
            rowIndex = DrawCueTracks(
                animations,
                phase,
                duration,
                AnimationColor,
                isVfx: false,
                rowIndex);
            rowIndex = DrawCueTracks(
                vfx,
                phase,
                duration,
                VfxColor,
                isVfx: true,
                rowIndex);

            if (rowIndex == 0)
            {
                var emptyRow = EditorGUILayout.GetControlRect(false, TrackHeight);
                DrawTrackBackground(emptyRow, phase, "—", showPhase: true);
            }

            EditorGUILayout.Space(2f);
        }

        private static int DrawCueTracks(
            SerializedProperty cues,
            SkillPresentationPhase phase,
            float duration,
            Color color,
            bool isVfx,
            int rowIndex)
        {
            for (var index = 0; index < cues.arraySize; index++)
            {
                var cue = cues.GetArrayElementAtIndex(index);
                if ((SkillPresentationPhase)cue.FindPropertyRelative("_phase").enumValueIndex != phase)
                    continue;

                var row = EditorGUILayout.GetControlRect(false, TrackHeight);
                var cueName = GetCueName(cue, isVfx, index);
                var track = DrawTrackBackground(row, phase, cueName, rowIndex == 0);
                var delay = cue.FindPropertyRelative("_delay").floatValue;
                var x = track.x + track.width * Mathf.Clamp01(delay / duration);
                var width = isVfx
                    ? Mathf.Max(4f, track.width * Mathf.Max(0.01f,
                        cue.FindPropertyRelative("_lifetime").floatValue / duration))
                    : 4f;
                width = Mathf.Min(width, track.xMax - x);
                var marker = new Rect(x, track.y + 2f, width, track.height - 4f);
                EditorGUI.DrawRect(marker, color);
                if (isVfx && marker.width > 35f)
                {
                    var lifetime = cue.FindPropertyRelative("_lifetime").floatValue;
                    EditorGUI.LabelField(marker, $" {delay:0.##}–{delay + lifetime:0.##}",
                        EditorStyles.miniLabel);
                }

                rowIndex++;
            }

            return rowIndex;
        }

        private static Rect DrawTrackBackground(
            Rect row,
            SkillPresentationPhase phase,
            string cueName,
            bool showPhase)
        {
            var phaseRect = new Rect(row.x, row.y, PhaseLabelWidth, row.height);
            var cueRect = new Rect(
                phaseRect.xMax,
                row.y,
                CueLabelWidth,
                row.height);
            var track = new Rect(
                cueRect.xMax,
                row.y + 1f,
                row.width - PhaseLabelWidth - CueLabelWidth,
                row.height - 2f);
            if (showPhase)
                EditorGUI.LabelField(phaseRect, phase.ToString(), EditorStyles.boldLabel);
            EditorGUI.LabelField(cueRect, cueName, EditorStyles.miniLabel);
            EditorGUI.DrawRect(track, new Color(0.12f, 0.12f, 0.12f, 0.35f));
            return track;
        }

        private static string GetCueName(SerializedProperty cue, bool isVfx, int index)
        {
            if (!isVfx)
            {
                var animation = cue.FindPropertyRelative("_cue");
                return $"Animation: {animation.enumDisplayNames[animation.enumValueIndex]}";
            }

            var prefab = cue.FindPropertyRelative("_prefab").objectReferenceValue;
            return prefab != null ? prefab.name : $"VFX #{index + 1} (missing)";
        }

        private static void DrawLegend(Color color, string label)
        {
            var rect = GUILayoutUtility.GetRect(70f, 16f, GUILayout.Width(70f));
            EditorGUI.DrawRect(new Rect(rect.x, rect.y + 4f, 8f, 8f), color);
            EditorGUI.LabelField(new Rect(rect.x + 12f, rect.y, 58f, rect.height), label);
        }
    }
}
