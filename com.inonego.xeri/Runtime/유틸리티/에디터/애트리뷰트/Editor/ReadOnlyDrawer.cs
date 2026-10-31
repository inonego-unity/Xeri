/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : ReadOnlyDrawer.cs
수정일 : 2026-10-31

# 설명
일반 필드를 비활성화하되 모듈이 읽기 전용 View를 제공하면 해당 표시 방식을 사용한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Reflection;

using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace inonego.Xeri
{
    // ============================================================
    /// <summary>
    /// ReadOnlyAttribute 전용 PropertyDrawer.
    /// </summary>
    // ============================================================
    [CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
    public class ReadOnlyDrawer : PropertyDrawer
    {

    #region 연결

        // ------------------------------------------------------------
        /// <summary>
        /// 특수 필드의 읽기 전용 View 생성 함수를 제공받는다.
        /// </summary>
        // ------------------------------------------------------------
        internal static Func<SerializedProperty, FieldInfo, VisualElement> CreateReadOnlyView { get; set; }

    #endregion

    #region GUI

        // ------------------------------------------------------------
        /// <summary>
        /// UI Toolkit Inspector용 읽기 전용 필드를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var view = CreateReadOnlyView?.Invoke(property, fieldInfo);
            if (view != null)
            {
                return view;
            }

            var field = new PropertyField(property);
            field.SetEnabled(false);
            return field;
        }

    #endregion

    }
}
