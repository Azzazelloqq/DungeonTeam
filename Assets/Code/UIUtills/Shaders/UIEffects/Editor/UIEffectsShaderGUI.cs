using System;
using UnityEditor;

namespace Code.UI.Editor
{
    public sealed class UIEffectsShaderGUI : ShaderGUI
    {
        private const float EnabledThreshold = 0.5f;

        private bool _showAdvanced;

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            var tint = RequireProperty(properties, "_Color");

            var rounded = RequireProperty(properties, "_Rounded");
            var topLeftRadius = RequireProperty(properties, "_TopLeftRadius");
            var topRightRadius = RequireProperty(properties, "_TopRightRadius");
            var bottomRightRadius = RequireProperty(properties, "_BottomRightRadius");
            var bottomLeftRadius = RequireProperty(properties, "_BottomLeftRadius");

            var gradient = RequireProperty(properties, "_Gradient");
            var gradientColorA = RequireProperty(properties, "_GradientColorA");
            var gradientColorB = RequireProperty(properties, "_GradientColorB");
            var gradientDirection = RequireProperty(properties, "_GradientDirection");
            var gradientStart = RequireProperty(properties, "_GradientStart");
            var gradientEnd = RequireProperty(properties, "_GradientEnd");

            var mask = RequireProperty(properties, "_Mask");
            var maskTexture = RequireProperty(properties, "_MaskTex");
            var maskScaleOffset = RequireProperty(properties, "_MaskScaleOffset");
            var maskRedChannel = RequireProperty(properties, "_MaskRedChannel");
            var maskInvert = RequireProperty(properties, "_MaskInvert");
            var maskStrength = RequireProperty(properties, "_MaskStrength");
            var maskCutoff = RequireProperty(properties, "_MaskCutoff");
            var maskSoftness = RequireProperty(properties, "_MaskSoftness");

            var useAlphaClip = RequireProperty(properties, "_UseUIAlphaClip");

            materialEditor.ShaderProperty(tint, tint.displayName);

            EditorGUILayout.Space();
            materialEditor.ShaderProperty(rounded, rounded.displayName);
            if (ShouldShowDetails(rounded))
            {
                EditorGUI.indentLevel++;
                materialEditor.ShaderProperty(topLeftRadius, topLeftRadius.displayName);
                materialEditor.ShaderProperty(topRightRadius, topRightRadius.displayName);
                materialEditor.ShaderProperty(bottomRightRadius, bottomRightRadius.displayName);
                materialEditor.ShaderProperty(bottomLeftRadius, bottomLeftRadius.displayName);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();
            materialEditor.ShaderProperty(gradient, gradient.displayName);
            if (ShouldShowDetails(gradient))
            {
                EditorGUI.indentLevel++;
                materialEditor.ShaderProperty(gradientColorA, gradientColorA.displayName);
                materialEditor.ShaderProperty(gradientColorB, gradientColorB.displayName);
                materialEditor.ShaderProperty(gradientDirection, gradientDirection.displayName);
                materialEditor.ShaderProperty(gradientStart, gradientStart.displayName);
                materialEditor.ShaderProperty(gradientEnd, gradientEnd.displayName);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();
            materialEditor.ShaderProperty(mask, mask.displayName);
            if (ShouldShowDetails(mask))
            {
                EditorGUI.indentLevel++;
                materialEditor.ShaderProperty(maskTexture, maskTexture.displayName);
                materialEditor.ShaderProperty(maskScaleOffset, maskScaleOffset.displayName);
                materialEditor.ShaderProperty(maskRedChannel, maskRedChannel.displayName);
                materialEditor.ShaderProperty(maskInvert, maskInvert.displayName);
                materialEditor.ShaderProperty(maskStrength, maskStrength.displayName);
                materialEditor.ShaderProperty(maskCutoff, maskCutoff.displayName);
                materialEditor.ShaderProperty(maskSoftness, maskSoftness.displayName);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();
            _showAdvanced = EditorGUILayout.Foldout(_showAdvanced, "Advanced", true);
            if (_showAdvanced)
            {
                EditorGUI.indentLevel++;
                materialEditor.ShaderProperty(useAlphaClip, useAlphaClip.displayName);
                EditorGUI.indentLevel--;
            }
        }

        private static bool ShouldShowDetails(MaterialProperty toggle)
        {
            return toggle.hasMixedValue || toggle.floatValue > EnabledThreshold;
        }

        private static MaterialProperty RequireProperty(MaterialProperty[] properties, string propertyName)
        {
            foreach (var property in properties)
            {
                if (property.name == propertyName)
                {
                    return property;
                }
            }

            throw new ArgumentException($"Shader property '{propertyName}' was not found.", nameof(properties));
        }
    }
}
