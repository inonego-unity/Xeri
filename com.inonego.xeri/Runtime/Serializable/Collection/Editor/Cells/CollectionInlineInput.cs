/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : CollectionInlineInput.cs
수정일 : 2026-11-24

# 설명
초안 열(Dictionary 키, XOrdered 순서·키) 가운데 단순 값을 셀 안에서 고치는 입력칸을 TableGrid 셀 뷰로 잇는다.
입력칸 자체(글자·숫자·bool·enum·오브젝트 선택)와 값 변경 요청은 TableGrid의 Inline 셀 뷰(TableGridCellRegistry)가 맡고,
여기서는 Collection에만 있는 일을 한다: 열 값 형식에 맞는 Inline 셀 뷰 찾기, 직렬화 속성에서 값 읽기,
오브젝트 선택 입력칸의 씬 오브젝트 허용 범위 맞추기.

# 특이사항, 제약사항
입력칸은 직렬화 속성에 바인딩하지 않고 값만 보인다. 고친 값은 표 셀 호스트로 가고(Confirm), 표가 확정(Enter·포커스 이탈)할 때
호스트가 세션의 초안 검증을 거쳐 쓴다.
읽을 수 있는 형식은 글자·정수(int·long)·실수(float·double)·bool·enum·UnityEngine.Object뿐이다.
그 밖의 형식과 관리 참조는 기존처럼 요약과 초안 창으로 고친다. enum 값은 초안 창과 같이 intValue로 읽는다.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

using inonego;
using inonego.Xeri;
using inonego.Xeri.UI.TableGrid;

namespace inonego.Xeri.Serializable.Editor
{
    // ============================================================
    /// <summary>
    /// 단순 값 초안 열의 셀 안 입력칸 연결.
    /// </summary>
    // ============================================================
    internal static class CollectionInlineInput
    {

    #region 상수

        // ------------------------------------------------------------
        /// <summary>
        /// 초안 입력칸에 붙는 USS 클래스.
        /// </summary>
        // ------------------------------------------------------------
        public const string ClassName = "xeri-collection__draft-input";

    #endregion

    #region 입력칸 선택

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> 열 값을 셀 안 입력칸으로 고칠 수 있는지 반환한다.
        /// <br/> 속성에서 값을 읽을 수 있는 형식이고 그 형식의 Inline 셀 뷰가 있어야 한다.
        /// </summary>
        // --------------------------------------------------------------------------------
        public static bool Supports(CollectionColumn column)
        {
            return FindView(column) != null;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 열 값 형식의 입력 셀 뷰를 반환한다. 고칠 수 없으면 null이다.
        /// </summary>
        // ------------------------------------------------------------
        public static TableGridCellView FindView(CollectionColumn column)
        {
            var type = column?.ValueType;
            if (type == null || column.IsManagedReference || !CanRead(type))
            {
                return null;
            }

            return TableGridCellRegistry.FindInlineView(type);
        }

        private static bool CanRead(Type type)
        {
            return type == typeof(string) || type == typeof(int) || type == typeof(long)
                || type == typeof(float) || type == typeof(double) || type == typeof(bool)
                || type.IsEnum || typeof(UnityEngine.Object).IsAssignableFrom(type);
        }

    #endregion

    #region 값

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> 셀 속성의 현재 값을 열 값 형식으로 읽는다.
        /// <br/> 속성 종류가 열 값 형식과 맞지 않으면 false다.
        /// </summary>
        // --------------------------------------------------------------------------------
        public static bool TryRead(SerializedProperty property, Type valueType, out object value)
        {
            value = null;
            if (property == null || valueType == null)
            {
                return false;
            }

            switch (property.propertyType)
            {
                case SerializedPropertyType.String when valueType == typeof(string):
                    value = property.stringValue;
                    return true;
                case SerializedPropertyType.Integer when valueType == typeof(int):
                    value = property.intValue;
                    return true;
                case SerializedPropertyType.Integer when valueType == typeof(long):
                    value = property.longValue;
                    return true;
                case SerializedPropertyType.Float when valueType == typeof(float):
                    value = property.floatValue;
                    return true;
                case SerializedPropertyType.Float when valueType == typeof(double):
                    value = property.doubleValue;
                    return true;
                case SerializedPropertyType.Boolean when valueType == typeof(bool):
                    value = property.boolValue;
                    return true;
                case SerializedPropertyType.Enum when valueType.IsEnum:
                    value = Enum.ToObject(valueType, property.intValue);
                    return true;
                case SerializedPropertyType.ObjectReference when typeof(UnityEngine.Object).IsAssignableFrom(valueType):
                    value = property.objectReferenceValue;
                    return true;
                default:
                    return false;
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 오브젝트 선택 입력칸이면 씬 오브젝트 허용 범위를 속성의 대상에 맞춘다.
        /// <br/> 씬 오브젝트 참조는 대상이 씬에 있을 때만 고를 수 있다.
        /// </summary>
        // ------------------------------------------------------------
        public static void FitScope(VisualElement input, SerializedProperty property)
        {
            if (input is ObjectField objectField)
            {
                objectField.allowSceneObjects = !EditorUtility.IsPersistent(property.serializedObject.targetObject);
            }
        }

    #endregion

    }
}
