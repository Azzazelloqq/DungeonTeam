using Code.GameConfig.ScriptableObjectParser.RemoteData.Skills.Effect;
using UnityEditor;
using UnityEngine;

namespace Code.GameConfig.Editor
{
#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(SkillEffectRemote))]
public class SkillEffectDrawer : PropertyDrawer
{
	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
	{
		// Using EditorGUI.PropertyField, we'll draw fields step-by-step.
		EditorGUI.BeginProperty(position, label, property);

		// We need to find the child properties
		var effectTypeProp = property.FindPropertyRelative("_effectType");
		var effectIdProp = property.FindPropertyRelative("_effectId");
		var effectImpactProp = property.FindPropertyRelative("_effectImpact");
		var effectDurationProp = property.FindPropertyRelative("_effectDuration");
		var intervalProp = property.FindPropertyRelative("_interval");

		// Calculate rects. We'll just stack fields vertically.
		var lineRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

		// Draw EffectType first
		EditorGUI.PropertyField(lineRect, effectTypeProp);
		lineRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

		// Draw EffectId
		EditorGUI.PropertyField(lineRect, effectIdProp);
		lineRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

		// Draw EffectImpact
		EditorGUI.PropertyField(lineRect, effectImpactProp);
		lineRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

		// Now get the current effectType enum value
		var currentType = (EffectType)effectTypeProp.enumValueIndex;

		// If it's an over-time effect, show duration and interval
		if (currentType == EffectType.OverTimeHeal ||
			currentType == EffectType.OverTimeDamage ||
			currentType == EffectType.OverTimeBuff)
		{
			EditorGUI.PropertyField(lineRect, effectDurationProp);
			lineRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

			EditorGUI.PropertyField(lineRect, intervalProp);
			lineRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
		}

		EditorGUI.EndProperty();
	}

	public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
	{
		var effectTypeProp = property.FindPropertyRelative("_effectType");
		var currentType = (EffectType)effectTypeProp.enumValueIndex;

		// Base fields: effectType, effectId, effectImpact = 3 lines
		var lines = 3;

		// If over-time, add 2 more lines
		if (currentType == EffectType.OverTimeHeal ||
			currentType == EffectType.OverTimeDamage ||
			currentType == EffectType.OverTimeBuff)
		{
			lines += 2;
		}

		return lines * (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing);
	}
}
#endif
}