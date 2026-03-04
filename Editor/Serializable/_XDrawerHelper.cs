using UnityEngine;
using UnityEditor;

namespace inonego.Xeri.Serializable.Editor
{
    // ==============================================================
    /// <summary>
    /// Shared drawing helper for all custom property drawers.
    /// </summary>
    // ==============================================================
    internal static class XDrawerHelper
    {
        #region Constants

        // Width for short inline labels
        internal const float LabelWidth    = 13f;

        // Bool toggle checkbox width
        private const float CheckboxWidth  = 16f;

        // Gap between adjacent bool entries
        private const float Spacing        = 2f;

        // Extra gap appended after each bool toggle entry
        private const float ToggleSpacing  = 10f;

        // Gap between adjacent int/float fields
        internal const float FieldSpacing  = 5f;

        #endregion

        #region Shared

        // ------------------------------------------------------------------------------------------
        /// <summary>
        /// Draws the property label and returns the X where content begins.
        /// <br/> Returns position.x if label is empty (e.g. GUIContent.none from XNullable).
        /// </summary>
        // ------------------------------------------------------------------------------------------
        public static float DrawLabelAndGetContentX(Rect position, GUIContent label)
        {
            if (label == GUIContent.none || string.IsNullOrEmpty(label.text))
                return position.x;

            float labelWidth = EditorGUIUtility.labelWidth;
            EditorGUI.LabelField(new Rect(position.x, position.y, labelWidth, position.height), label);
            return position.x + labelWidth + Spacing;
        }

        // ---------------------------------------------------------------
        /// <summary>
        /// Calculates the per-field width so all fields fill
        /// the remaining content area evenly.
        /// </summary>
        // ---------------------------------------------------------------
        public static float GetFieldWidth(Rect position, float contentX, int fieldCount)
        {
            float contentWidth = position.x + position.width - contentX;
            return (contentWidth - FieldSpacing * (fieldCount - 1)) / fieldCount;
        }

        #endregion

        #region Bool

        // ------------------------------------------------------------
        /// <summary>
        /// Draws a label and toggle for a bool field.
        /// <br/> Returns the next X start position.
        /// </summary>
        // ------------------------------------------------------------
        public static float DrawToggle(Rect position, string fieldLabel, SerializedProperty prop, float startX)
        {
            EditorGUI.LabelField(new Rect(startX, position.y, LabelWidth, position.height), fieldLabel);

            prop.boolValue = EditorGUI.Toggle(
                new Rect(startX + LabelWidth + 2f, position.y, CheckboxWidth, position.height),
                prop.boolValue);

            return startX + LabelWidth + CheckboxWidth + Spacing + ToggleSpacing;
        }

        #endregion

        #region Int

        // ------------------------------------------------------------------
        /// <summary>
        /// Draws a draggable label and int field.
        /// <br/> Drag behavior is provided natively by EditorGUI.IntField
        /// <br/> when a GUIContent label is supplied.
        /// <br/> Returns the next X start position.
        /// </summary>
        // ------------------------------------------------------------------
        public static float DrawIntField(Rect position, string fieldLabel, SerializedProperty prop, float startX, float fieldWidth)
        {
            float savedLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = LabelWidth;

            prop.intValue = EditorGUI.IntField(
                new Rect(startX, position.y, fieldWidth, position.height),
                new GUIContent(fieldLabel),
                prop.intValue);

            EditorGUIUtility.labelWidth = savedLabelWidth;

            return startX + fieldWidth + FieldSpacing;
        }

        #endregion

        #region Float

        // -------------------------------------------------------------------------
        /// <summary>
        /// Draws a draggable label and float field.
        /// <br/> Drag behavior is provided natively by EditorGUI.FloatField
        /// <br/> when a GUIContent label is supplied.
        /// <br/> Returns the next X start position.
        /// </summary>
        // -------------------------------------------------------------------------
        public static float DrawFloatField(Rect position, string fieldLabel, SerializedProperty prop, float startX, float fieldWidth)
        {
            float savedLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = LabelWidth;

            prop.floatValue = EditorGUI.FloatField(
                new Rect(startX, position.y, fieldWidth, position.height),
                new GUIContent(fieldLabel),
                prop.floatValue);

            EditorGUIUtility.labelWidth = savedLabelWidth;

            return startX + fieldWidth + FieldSpacing;
        }

        #endregion

        #region Property

        // -----------------------------------------------------------------------
        /// <summary>
        /// Draws a labeled PropertyField with drag support.
        /// <br/> Suitable for any serialized type (int, float, struct, etc.).
        /// <br/> Returns the next X start position.
        /// </summary>
        // -----------------------------------------------------------------------
        public static float DrawPropertyField(Rect position, string fieldLabel, SerializedProperty prop, float startX, float fieldWidth, float labelWidth)
        {
            float savedLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = labelWidth;

            EditorGUI.PropertyField(
                new Rect(startX, position.y, fieldWidth, position.height),
                prop,
                new GUIContent(fieldLabel));

            EditorGUIUtility.labelWidth = savedLabelWidth;

            return startX + fieldWidth + FieldSpacing;
        }

        #endregion
    }
}
