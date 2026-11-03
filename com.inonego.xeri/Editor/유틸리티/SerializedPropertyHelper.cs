/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : SerializedPropertyHelper.cs
수정일 : 2026-11-03

# 설명
SerializedProperty의 현재 값과 실제 객체 인스턴스를 구분하여 조회한다.
배열/리스트 요소 경로, 경로 포함 관계, UI Toolkit 직계 자식 필드 생성과 기본값 초기화를 지원한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Reflection;

using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace inonego.Xeri
{
    // ============================================================
    /// <summary>
    /// SerializedProperty 값과 객체 조회 헬퍼.
    /// </summary>
    // ============================================================
    public static class SerializedPropertyHelper
    {

    #region 값과 객체 조회

        // ------------------------------------------------------------
        /// <summary>
        /// SerializedProperty 경로로부터 실제 객체를 T로 캐스팅해 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public static T GetTargetObject<T>(SerializedProperty property) where T : class
        {
            return GetTargetObject(property) as T;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> SerializedProperty 경로로부터 실제 객체를 반환한다.
        /// <br/> 배열 요소 경로(Array.data[n])도 지원한다.
        /// </summary>
        // ------------------------------------------------------------
        public static object GetTargetObject(SerializedProperty property)
        {
            var path = property.propertyPath.Replace(".Array.data[", "[");
            object obj = property.serializedObject.targetObject;

            foreach (var element in path.Split('.'))
            {
                if (element.Contains("["))
                {
                    var bracketIndex = element.IndexOf('[');
                    var fieldName    = element[..bracketIndex];
                    var index        = int.Parse(element[(bracketIndex + 1)..element.IndexOf(']')]);

                    var listField = FindField(obj.GetType(), fieldName);
                    if (listField == null) return null;

                    obj = ((IList)listField.GetValue(obj))[index];
                }
                else
                {
                    var field = FindField(obj.GetType(), element);
                    if (field == null) return null;
                    obj = field.GetValue(obj);
                }
            }

            return obj;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 현재 직렬화된 값을 반환한다.
        /// <br/> 복합 행의 identity는 원본 객체에서 조회한다.
        /// </summary>
        // ------------------------------------------------------------
        public static object GetValue(SerializedProperty property)
        {
            if (property == null)
            {
                return null;
            }

            if (property.propertyType == SerializedPropertyType.ManagedReference)
            {
                return property.managedReferenceValue;
            }

            if (property.propertyType == SerializedPropertyType.ObjectReference)
            {
                return property.objectReferenceValue;
            }

            if (property.propertyType == SerializedPropertyType.Generic
                || property.isArray && property.propertyType != SerializedPropertyType.String)
            {
                return GetTargetObject(property);
            }

            return property.boxedValue;
        }

    #endregion

    #region 경로 관계

        // ------------------------------------------------------------
        /// <summary>
        /// 자신을 제외하고 지정한 경로의 하위 프로퍼티인지 판정한다.
        /// </summary>
        // ------------------------------------------------------------
        public static bool IsDescendantPath(string path, string ancestorPath)
        {
            return !string.IsNullOrEmpty(path)
                && !string.IsNullOrEmpty(ancestorPath)
                && path.Length > ancestorPath.Length
                && path[ancestorPath.Length] == '.'
                && path.StartsWith(ancestorPath, StringComparison.Ordinal);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 동일하거나 하위 프로퍼티인지 판정한다.
        /// </summary>
        // ------------------------------------------------------------
        public static bool IsSameOrDescendantPath(string path, string ancestorPath)
        {
            return !string.IsNullOrEmpty(path)
                && (path == ancestorPath || IsDescendantPath(path, ancestorPath));
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 자신을 제외하고 지정한 경로의 상위 프로퍼티인지 판정한다.
        /// </summary>
        // ------------------------------------------------------------
        public static bool IsAncestorPath(string path, string descendantPath)
        {
            return IsDescendantPath(descendantPath, path);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 동일하거나 상위 프로퍼티인지 판정한다.
        /// </summary>
        // ------------------------------------------------------------
        public static bool IsSameOrAncestorPath(string path, string descendantPath)
        {
            return IsSameOrDescendantPath(descendantPath, path);
        }

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> 지정한 경로의 직계 자식 프로퍼티인지 판정한다.
        /// <br/> Unity의 Array.data[n]과 Array.size를 하나의 논리적 자식으로 취급한다.
        /// </summary>
        // --------------------------------------------------------------------------------
        public static bool IsDirectChildPath(string path, string parentPath)
        {
            if (!IsDescendantPath(path, parentPath))
            {
                return false;
            }

            var childStart = parentPath.Length + 1;
            var separatorIndex = path.IndexOf('.', childStart);
            if (separatorIndex < 0)
            {
                return true;
            }

            // Array는 실제 필드가 아닌 Unity 직렬화 경로의 중간 표기다.
            if (separatorIndex != childStart + "Array".Length)
            {
                return false;
            }

            const string sizePath = "Array.size";
            if (path.Length == childStart + sizePath.Length
                && string.CompareOrdinal(path, childStart, sizePath, 0, sizePath.Length) == 0)
            {
                return true;
            }

            const string elementPrefix = "Array.data[";
            if (path.Length <= childStart + elementPrefix.Length + 1
                || path[path.Length - 1] != ']'
                || string.CompareOrdinal
                (
                    path, childStart, elementPrefix, 0, elementPrefix.Length
                ) != 0)
            {
                return false;
            }

            for (var index = childStart + elementPrefix.Length; index < path.Length - 1; index++)
            {
                if (path[index] < '0' || path[index] > '9')
                {
                    return false;
                }
            }

            return true;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 지정한 경로의 직계 부모 프로퍼티인지 판정한다.
        /// </summary>
        // ------------------------------------------------------------
        public static bool IsDirectParentPath(string path, string childPath)
        {
            return IsDirectChildPath(childPath, path);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 두 경로가 동일하거나 상위·하위 관계인지 판정한다.
        /// </summary>
        // ------------------------------------------------------------
        public static bool IsRelatedPath(string path, string otherPath)
        {
            return IsSameOrDescendantPath(path, otherPath)
                || IsSameOrAncestorPath(path, otherPath);
        }

    #endregion

    #region UI 표시

        // ----------------------------------------------------------------------
        /// <summary>
        /// 표시 가능한 직계 자식 필드를 container에 PropertyField로 추가한다.
        /// </summary>
        // ----------------------------------------------------------------------
        public static void AppendVisibleChildren(SerializedProperty property, VisualElement container)
        {
            if (property == null || container == null)
            {
                return;
            }

            var depth    = property.depth;
            var iterator = property.Copy();

            // 현재 property의 drawer를 재호출하지 않고 실제 데이터 자식부터 기본 PropertyField에 위임한다.
            if (!iterator.NextVisible(enterChildren: true))
            {
                return;
            }

            do
            {
                // 다음 sibling으로 넘어가면 현재 property의 자식 범위를 모두 처리한 상태다.
                if (iterator.depth <= depth)
                {
                    break;
                }

                container.Add(new PropertyField(iterator.Copy()));
            }
            while (iterator.NextVisible(enterChildren: false));
        }

    #endregion

    #region 기본값 초기화

        // ------------------------------------------------------------
        /// <summary>
        /// property를 타입 기본값으로 초기화한다.
        /// </summary>
        // ------------------------------------------------------------
        public static void SetDefaultValue(SerializedProperty property)
        {
            if (property == null)
            {
                throw new ArgumentNullException(nameof(property));
            }

            if (property.isFixedBuffer)
            {
                for (var index = 0; index < property.arraySize; index++)
                {
                    SetDefaultValue(property.GetArrayElementAtIndex(index));
                }

                return;
            }

            if (property.isArray && property.propertyType != SerializedPropertyType.String)
            {
                property.arraySize = 0;
                return;
            }

            switch (property.propertyType)
            {
                case SerializedPropertyType.Integer:
                    property.longValue = 0;
                    return;

                case SerializedPropertyType.Boolean:
                    property.boolValue = false;
                    return;

                case SerializedPropertyType.Float:
                    property.doubleValue = 0d;
                    return;

                case SerializedPropertyType.String:
                    property.stringValue = string.Empty;
                    return;

                case SerializedPropertyType.Color:
                    property.colorValue = default;
                    return;

                case SerializedPropertyType.ObjectReference:
                    property.objectReferenceValue = null;
                    return;

                case SerializedPropertyType.LayerMask:
                    property.intValue = 0;
                    return;

                case SerializedPropertyType.Enum:
                    property.enumValueIndex = 0;
                    return;

                case SerializedPropertyType.Vector2:
                    property.vector2Value = default;
                    return;

                case SerializedPropertyType.Vector3:
                    property.vector3Value = default;
                    return;

                case SerializedPropertyType.Vector4:
                    property.vector4Value = default;
                    return;

                case SerializedPropertyType.Rect:
                    property.rectValue = default;
                    return;

                case SerializedPropertyType.ArraySize:
                    property.intValue = 0;
                    return;

                case SerializedPropertyType.Character:
                    property.intValue = 0;
                    return;

                case SerializedPropertyType.AnimationCurve:
                    property.animationCurveValue = new AnimationCurve();
                    return;

                case SerializedPropertyType.Gradient:
                    property.gradientValue = new Gradient();
                    return;

                case SerializedPropertyType.Bounds:
                    property.boundsValue = default;
                    return;

                case SerializedPropertyType.Quaternion:
                    property.quaternionValue = Quaternion.identity;
                    return;

                case SerializedPropertyType.ExposedReference:
                    property.exposedReferenceValue = null;
                    return;

                case SerializedPropertyType.Vector2Int:
                    property.vector2IntValue = default;
                    return;

                case SerializedPropertyType.Vector3Int:
                    property.vector3IntValue = default;
                    return;

                case SerializedPropertyType.RectInt:
                    property.rectIntValue = default;
                    return;

                case SerializedPropertyType.BoundsInt:
                    property.boundsIntValue = default;
                    return;

                case SerializedPropertyType.ManagedReference:
                    property.managedReferenceValue = null;
                    return;

                case SerializedPropertyType.Hash128:
                    property.hash128Value = default;
                    return;

                case SerializedPropertyType.RenderingLayerMask:
                    property.uintValue = 0u;
                    return;

                case SerializedPropertyType.EntityId:
                    property.entityIdValue = default;
                    return;

                case SerializedPropertyType.GUID:
                    property.guidValue = default;
                    return;

                case SerializedPropertyType.Generic:
                    ResetSerializedChildren(property);
                    return;

                default:
                    throw new NotSupportedException
                    (
                        $"초기화를 지원하지 않는 SerializedProperty 타입: {property.propertyType}"
                    );
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 숨겨진 serialized child까지 재귀적으로 기본값으로 초기화한다.
        /// </summary>
        // ------------------------------------------------------------
        private static void ResetSerializedChildren(SerializedProperty property)
        {
            var depth = property.depth;
            var iterator = property.Copy();

            // Add는 이전 행의 숨겨진 authoring 데이터를 상속하지 않는다.
            if (!iterator.Next(enterChildren: true))
            {
                return;
            }

            do
            {
                if (iterator.depth <= depth)
                {
                    break;
                }

                SetDefaultValue(iterator);
            }
            while (iterator.Next(enterChildren: false));
        }

    #endregion

    #region 필드 탐색

        // ------------------------------------------------------------
        /// <summary>
        /// 상속 계층을 순회하며 필드를 검색한다.
        /// </summary>
        // ------------------------------------------------------------
        private static FieldInfo FindField(Type type, string name)
        {
            const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;

            while (type != null)
            {
                var field = type.GetField(name, flags);
                if (field != null) return field;
                type = type.BaseType;
            }

            return null;
        }

    #endregion

    }
}
