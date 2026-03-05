using UnityEngine;
using UnityEditor;

namespace inonego.Xeri.Serializable.Editor
{
    // ==============================================================
    /// <summary>
    /// PropertyDrawer for XNullable.
    /// </summary>
    // ==============================================================
    [CustomPropertyDrawer(typeof(XNullable<>))]
    public class XNullableDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            SerializedProperty valueProp = property.FindPropertyRelative("value");
            
            return EditorGUI.GetPropertyHeight(valueProp, label, true);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            SerializedProperty hasValueProp = property.FindPropertyRelative("hasValue");
            SerializedProperty valueProp = property.FindPropertyRelative("value");

            // Divide the full area into 3 sections (label, value, checkbox)
            float labelWidth = EditorGUIUtility.labelWidth - 2;
            float checkboxWidth = 16f;
            float spacing = 4f;
            float singleLineHeight = EditorGUIUtility.singleLineHeight;

            // Label and checkbox are shown only on the first line
            Rect labelRect = new Rect(position.x, position.y, labelWidth, singleLineHeight);
            Rect valueRect = new Rect(position.x + labelWidth + spacing, position.y, position.width - (labelWidth + checkboxWidth + spacing * 2), position.height);
            Rect checkboxRect = new Rect(position.x + position.width - checkboxWidth, position.y, checkboxWidth, singleLineHeight);

            // Draw label
            EditorGUI.LabelField(labelRect, label);

            int savedIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            bool enabled = GUI.enabled;

            GUI.enabled = enabled && hasValueProp.boolValue;
            EditorGUI.PropertyField(valueRect, valueProp, GUIContent.none, true);
            GUI.enabled = enabled;

            // Draw HasValue toggle
            EditorGUI.BeginChangeCheck();
            bool hasValue = EditorGUI.Toggle(checkboxRect, hasValueProp.boolValue);
            if (EditorGUI.EndChangeCheck())
            {
                hasValueProp.boolValue = hasValue;
            }

            EditorGUI.indentLevel = savedIndent;

            EditorGUI.EndProperty();
        }
    }
}