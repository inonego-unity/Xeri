using UnityEngine;
using UnityEditor;

namespace inonego.Xeri.Serializable.Editor
{
    // ==============================================================
    /// <summary>
    /// PropertyDrawer for MinMax. Displays Min and Max fields inline.
    /// </summary>
    // ==============================================================
    [CustomPropertyDrawer(typeof(MinMax<>))]
    public class MinMaxDrawer : PropertyDrawer
    {
        // "Min" / "Max" label width
        private const float MinMaxLabelWidth = 30f;

        // ------------------------------------------------------------
        /// <summary>
        /// Draws Min and Max fields side by side.
        /// <br/> Labels act as drag handles for numeric types.
        /// </summary>
        // ------------------------------------------------------------
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            SerializedProperty minProp = property.FindPropertyRelative("min");
            SerializedProperty maxProp = property.FindPropertyRelative("max");

            float contentX   = XDrawerHelper.DrawLabelAndGetContentX(position, label);
            float fieldWidth = XDrawerHelper.GetFieldWidth(position, contentX, 2);

            float savedLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = MinMaxLabelWidth;

            EditorGUI.PropertyField(new Rect(contentX, position.y, fieldWidth, position.height), minProp, new GUIContent("Min"));
            EditorGUI.PropertyField(new Rect(contentX + fieldWidth + XDrawerHelper.FieldSpacing, position.y, fieldWidth, position.height), maxProp, new GUIContent("Max"));

            EditorGUIUtility.labelWidth = savedLabelWidth;

            EditorGUI.EndProperty();
        }
    }
}
