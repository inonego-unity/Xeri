/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : XVecDrawer.cs
수정일 : 2026-05-02

# 설명
XVec2B·XVec3B·XVec4B / XVec2I·XVec3I / XVec2F·XVec3F·XVec4F 전용 UI Toolkit PropertyDrawer.
Inspector에서 [Label | 축 필드...] 단일 행 레이아웃을 렌더링한다.
Integer·Float 축은 레이블 드래그로 값 조정이 가능하다.
========================================================================= BLOCK_HEADER_END */

using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace inonego.Xeri
{
    using Serializable;

    // ============================================================
    /// <summary>
    /// XVec 드로어 공용 헬퍼.
    /// </summary>
    // ============================================================
    internal static class XVecDrawerHelper
    {
        // ------------------------------------------------------------
        /// <summary>
        /// field-label을 포함한 기본 행을 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        public static VisualElement CreateRow(SerializedProperty property)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems    = Align.Center;

            var label = new Label(property.displayName);
            label.AddToClassList("unity-base-field__label");

            row.Add(label);
            return row;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Bool 축 (레이블 + Toggle)을 행에 추가한다.
        /// </summary>
        // ------------------------------------------------------------
        public static void AppendToggle(VisualElement row, SerializedProperty prop, string axis)
        {
            var group = new VisualElement();
            group.style.flexDirection = FlexDirection.Row;
            group.style.alignItems    = Align.Center;
            group.style.marginRight   = 8;

            var label = new Label(axis);
            label.style.minWidth   = 13;
            label.style.flexShrink = 0;

            var toggle = new Toggle();
            toggle.BindProperty(prop);

            group.Add(label);
            group.Add(toggle);
            row.Add(group);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Int 축 (레이블 드래그 지원 IntegerField)을 행에 추가한다.
        /// </summary>
        // ------------------------------------------------------------
        public static void AppendIntField(VisualElement row, SerializedProperty prop, string axis)
        {
            var field = new IntegerField(axis);

            field.labelElement.style.minWidth    = 0;
            field.labelElement.style.flexShrink  = 0;
            field.labelElement.style.marginLeft  = 4;
            field.labelElement.style.marginRight = 4;

            field.style.flexGrow  = 1;
            field.style.flexBasis = 0;
            field.style.minWidth  = 30;

            field.BindProperty(prop);
            row.Add(field);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Float 축 (레이블 드래그 지원 FloatField)을 행에 추가한다.
        /// </summary>
        // ------------------------------------------------------------
        public static void AppendFloatField(VisualElement row, SerializedProperty prop, string axis)
        {
            var field = new FloatField(axis);

            field.labelElement.style.minWidth    = 0;
            field.labelElement.style.flexShrink  = 0;
            field.labelElement.style.marginLeft  = 4;
            field.labelElement.style.marginRight = 4;

            field.style.flexGrow  = 1;
            field.style.flexBasis = 0;
            field.style.minWidth  = 30;

            field.BindProperty(prop);
            row.Add(field);
        }
    }

    // ============================================================
    // Bool 드로어
    // ============================================================

    [CustomPropertyDrawer(typeof(XVec2B))]
    public class XVec2BDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var row = XVecDrawerHelper.CreateRow(property);
            XVecDrawerHelper.AppendToggle(row, property.FindPropertyRelative("X"), "X");
            XVecDrawerHelper.AppendToggle(row, property.FindPropertyRelative("Y"), "Y");
            return row;
        }
    }

    [CustomPropertyDrawer(typeof(XVec3B))]
    public class XVec3BDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var row = XVecDrawerHelper.CreateRow(property);
            XVecDrawerHelper.AppendToggle(row, property.FindPropertyRelative("X"), "X");
            XVecDrawerHelper.AppendToggle(row, property.FindPropertyRelative("Y"), "Y");
            XVecDrawerHelper.AppendToggle(row, property.FindPropertyRelative("Z"), "Z");
            return row;
        }
    }

    [CustomPropertyDrawer(typeof(XVec4B))]
    public class XVec4BDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var row = XVecDrawerHelper.CreateRow(property);
            XVecDrawerHelper.AppendToggle(row, property.FindPropertyRelative("X"), "X");
            XVecDrawerHelper.AppendToggle(row, property.FindPropertyRelative("Y"), "Y");
            XVecDrawerHelper.AppendToggle(row, property.FindPropertyRelative("Z"), "Z");
            XVecDrawerHelper.AppendToggle(row, property.FindPropertyRelative("W"), "W");
            return row;
        }
    }

    // ============================================================
    // Integer 드로어
    // ============================================================

    [CustomPropertyDrawer(typeof(XVec2I))]
    public class XVec2IDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var row = XVecDrawerHelper.CreateRow(property);
            XVecDrawerHelper.AppendIntField(row, property.FindPropertyRelative("X"), "X");
            XVecDrawerHelper.AppendIntField(row, property.FindPropertyRelative("Y"), "Y");
            return row;
        }
    }

    [CustomPropertyDrawer(typeof(XVec3I))]
    public class XVec3IDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var row = XVecDrawerHelper.CreateRow(property);
            XVecDrawerHelper.AppendIntField(row, property.FindPropertyRelative("X"), "X");
            XVecDrawerHelper.AppendIntField(row, property.FindPropertyRelative("Y"), "Y");
            XVecDrawerHelper.AppendIntField(row, property.FindPropertyRelative("Z"), "Z");
            return row;
        }
    }

    // ============================================================
    // Float 드로어
    // ============================================================

    [CustomPropertyDrawer(typeof(XVec2F))]
    public class XVec2FDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var row = XVecDrawerHelper.CreateRow(property);
            XVecDrawerHelper.AppendFloatField(row, property.FindPropertyRelative("X"), "X");
            XVecDrawerHelper.AppendFloatField(row, property.FindPropertyRelative("Y"), "Y");
            return row;
        }
    }

    [CustomPropertyDrawer(typeof(XVec3F))]
    public class XVec3FDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var row = XVecDrawerHelper.CreateRow(property);
            XVecDrawerHelper.AppendFloatField(row, property.FindPropertyRelative("X"), "X");
            XVecDrawerHelper.AppendFloatField(row, property.FindPropertyRelative("Y"), "Y");
            XVecDrawerHelper.AppendFloatField(row, property.FindPropertyRelative("Z"), "Z");
            return row;
        }
    }

    [CustomPropertyDrawer(typeof(XVec4F))]
    public class XVec4FDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var row = XVecDrawerHelper.CreateRow(property);
            XVecDrawerHelper.AppendFloatField(row, property.FindPropertyRelative("X"), "X");
            XVecDrawerHelper.AppendFloatField(row, property.FindPropertyRelative("Y"), "Y");
            XVecDrawerHelper.AppendFloatField(row, property.FindPropertyRelative("Z"), "Z");
            XVecDrawerHelper.AppendFloatField(row, property.FindPropertyRelative("W"), "W");
            return row;
        }
    }
}
