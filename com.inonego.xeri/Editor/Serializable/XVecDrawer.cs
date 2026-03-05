using UnityEngine;
using UnityEditor;

namespace inonego.Xeri.Serializable.Editor
{
    #region Bool Drawers

    // ==============================================================
    /// <summary>
    /// PropertyDrawer for XVec2B.
    /// </summary>
    // ==============================================================
    [CustomPropertyDrawer(typeof(XVec2B))]
    public class XVec2BDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            float x = XDrawerHelper.DrawLabelAndGetContentX(position, label);
            x = XDrawerHelper.DrawToggle(position, "X", property.FindPropertyRelative("X"), x);
            x = XDrawerHelper.DrawToggle(position, "Y", property.FindPropertyRelative("Y"), x);

            EditorGUI.EndProperty();
        }
    }

    // ==============================================================
    /// <summary>
    /// PropertyDrawer for XVec3B.
    /// </summary>
    // ==============================================================
    [CustomPropertyDrawer(typeof(XVec3B))]
    public class XVec3BDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            float x = XDrawerHelper.DrawLabelAndGetContentX(position, label);
            x = XDrawerHelper.DrawToggle(position, "X", property.FindPropertyRelative("X"), x);
            x = XDrawerHelper.DrawToggle(position, "Y", property.FindPropertyRelative("Y"), x);
            x = XDrawerHelper.DrawToggle(position, "Z", property.FindPropertyRelative("Z"), x);

            EditorGUI.EndProperty();
        }
    }

    // ==============================================================
    /// <summary>
    /// PropertyDrawer for XVec4B.
    /// </summary>
    // ==============================================================
    [CustomPropertyDrawer(typeof(XVec4B))]
    public class XVec4BDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            float x = XDrawerHelper.DrawLabelAndGetContentX(position, label);
            x = XDrawerHelper.DrawToggle(position, "X", property.FindPropertyRelative("X"), x);
            x = XDrawerHelper.DrawToggle(position, "Y", property.FindPropertyRelative("Y"), x);
            x = XDrawerHelper.DrawToggle(position, "Z", property.FindPropertyRelative("Z"), x);
            x = XDrawerHelper.DrawToggle(position, "W", property.FindPropertyRelative("W"), x);

            EditorGUI.EndProperty();
        }
    }

    #endregion

    #region Int Drawers

    // ==============================================================
    /// <summary>
    /// PropertyDrawer for XVec2I. Axes support drag-to-change.
    /// </summary>
    // ==============================================================
    [CustomPropertyDrawer(typeof(XVec2I))]
    public class XVec2IDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            float x = XDrawerHelper.DrawLabelAndGetContentX(position, label);
            float w = XDrawerHelper.GetFieldWidth(position, x, 2);
            x = XDrawerHelper.DrawIntField(position, "X", property.FindPropertyRelative("X"), x, w);
            x = XDrawerHelper.DrawIntField(position, "Y", property.FindPropertyRelative("Y"), x, w);

            EditorGUI.EndProperty();
        }
    }

    // ==============================================================
    /// <summary>
    /// PropertyDrawer for XVec3I. Axes support drag-to-change.
    /// </summary>
    // ==============================================================
    [CustomPropertyDrawer(typeof(XVec3I))]
    public class XVec3IDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            float x = XDrawerHelper.DrawLabelAndGetContentX(position, label);
            float w = XDrawerHelper.GetFieldWidth(position, x, 3);
            x = XDrawerHelper.DrawIntField(position, "X", property.FindPropertyRelative("X"), x, w);
            x = XDrawerHelper.DrawIntField(position, "Y", property.FindPropertyRelative("Y"), x, w);
            x = XDrawerHelper.DrawIntField(position, "Z", property.FindPropertyRelative("Z"), x, w);

            EditorGUI.EndProperty();
        }
    }

    #endregion

    #region Float Drawers

    // ==============================================================
    /// <summary>
    /// PropertyDrawer for XVec2F. Axes support drag-to-change.
    /// </summary>
    // ==============================================================
    [CustomPropertyDrawer(typeof(XVec2F))]
    public class XVec2FDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            float x = XDrawerHelper.DrawLabelAndGetContentX(position, label);
            float w = XDrawerHelper.GetFieldWidth(position, x, 2);
            x = XDrawerHelper.DrawFloatField(position, "X", property.FindPropertyRelative("X"), x, w);
            x = XDrawerHelper.DrawFloatField(position, "Y", property.FindPropertyRelative("Y"), x, w);

            EditorGUI.EndProperty();
        }
    }

    // ==============================================================
    /// <summary>
    /// PropertyDrawer for XVec3F. Axes support drag-to-change.
    /// </summary>
    // ==============================================================
    [CustomPropertyDrawer(typeof(XVec3F))]
    public class XVec3FDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            float x = XDrawerHelper.DrawLabelAndGetContentX(position, label);
            float w = XDrawerHelper.GetFieldWidth(position, x, 3);
            x = XDrawerHelper.DrawFloatField(position, "X", property.FindPropertyRelative("X"), x, w);
            x = XDrawerHelper.DrawFloatField(position, "Y", property.FindPropertyRelative("Y"), x, w);
            x = XDrawerHelper.DrawFloatField(position, "Z", property.FindPropertyRelative("Z"), x, w);

            EditorGUI.EndProperty();
        }
    }

    // ==============================================================
    /// <summary>
    /// PropertyDrawer for XVec4F. Axes support drag-to-change.
    /// </summary>
    // ==============================================================
    [CustomPropertyDrawer(typeof(XVec4F))]
    public class XVec4FDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            float x = XDrawerHelper.DrawLabelAndGetContentX(position, label);
            float w = XDrawerHelper.GetFieldWidth(position, x, 4);
            x = XDrawerHelper.DrawFloatField(position, "X", property.FindPropertyRelative("X"), x, w);
            x = XDrawerHelper.DrawFloatField(position, "Y", property.FindPropertyRelative("Y"), x, w);
            x = XDrawerHelper.DrawFloatField(position, "Z", property.FindPropertyRelative("Z"), x, w);
            x = XDrawerHelper.DrawFloatField(position, "W", property.FindPropertyRelative("W"), x, w);

            EditorGUI.EndProperty();
        }
    }

    #endregion
}
