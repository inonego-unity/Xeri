/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : CollectionCellSummary.cs
수정일 : 2026-11-19

# 설명
SerializedProperty에서 셀 요약 문자열과 표시 문자열을 만든다.
Runtime 표시용 계약을 추가하지 않고 작은 복합 타입은 직접 필드 1~2개로 요약한다.

# 특이사항, 제약사항
중첩·복합 필드나 세 개 이상의 직접 필드는 일부를 생략하지 않고 타입 이름으로 되돌린다.
출력 문자열은 키 동등성이나 SerializeReference 수명 판단에 사용하지 않는다.
SerializeReference 셀의 요약은 해당 셀 drawer가 이 도구를 조합해 만든다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEditor;

using inonego;
using inonego.Xeri;

namespace inonego.Xeri.Serializable.Editor
{
    // ============================================================
    /// <summary>
    /// 셀 요약 문자열 생성 도구.
    /// </summary>
    // ============================================================
    internal static class CollectionCellSummary
    {

    #region 요약

        // ------------------------------------------------------------
        /// <summary>
        /// 값 하나를 짧게 표시하는 문자열을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public static string GetDisplayText(SerializedProperty property)
        {
            if (property == null)
            {
                return string.Empty;
            }

            if (property.isArray && property.propertyType != SerializedPropertyType.String)
            {
                return $"{property.arraySize} items";
            }

            switch (property.propertyType)
            {
                case SerializedPropertyType.Integer:
                    return property.longValue.ToString();

                case SerializedPropertyType.Boolean:
                    return property.boolValue.ToString();

                case SerializedPropertyType.Float:
                    return property.doubleValue.ToString("G");

                case SerializedPropertyType.String:
                    return property.stringValue ?? string.Empty;

                case SerializedPropertyType.Enum:
                    return property.enumDisplayNames.Length > property.enumValueIndex
                        && property.enumValueIndex >= 0
                            ? property.enumDisplayNames[property.enumValueIndex]
                            : property.enumValueIndex.ToString();

                case SerializedPropertyType.ObjectReference:
                    return property.objectReferenceValue != null
                        ? property.objectReferenceValue.name
                        : "None";

                case SerializedPropertyType.Generic:
                    return ObjectNames.NicifyVariableName(property.type);

                default:
                    return property.displayName;
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 셀에 표시할 요약 문자열을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public static string GetText(SerializedProperty property)
        {
            if (property == null)
            {
                return "Missing";
            }

            // 배열·리스트와 단순 값은 값 하나의 표시 문자열과 같다.
            var isArray = property.isArray && property.propertyType != SerializedPropertyType.String;
            if (isArray || property.propertyType != SerializedPropertyType.Generic)
            {
                return GetDisplayText(property);
            }

            return TryBuildAutomaticSummary(property, out var summary) ? summary : FormatTypeName(property.type);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 직접 자식이 모두 단순 타입이고 최대 두 개일 때만 요약한다.
        /// </summary>
        // ------------------------------------------------------------
        public static bool TryBuildAutomaticSummary(SerializedProperty property, out string summary)
        {
            summary = null;
            var parentDepth = property.depth;
            var parts = new List<string>(2);
            foreach (var child in SerializedPropertyHelper.EnumerateVisibleChildren(property))
            {
                if (child.depth != parentDepth + 1)
                {
                    continue;
                }

                if (!IsSimple(child) || parts.Count >= 2)
                {
                    return false;
                }

                parts.Add(GetDisplayText(child));
            }

            if (parts.Count == 0)
            {
                return false;
            }

            summary = string.Join(" · ", parts);
            return true;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 제네릭 인자 표기를 줄인 타입 이름을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public static string FormatTypeName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return "None";
            }

            var genericIndex = name.IndexOf((char)96);
            return genericIndex < 0 ? name : name.Substring(0, genericIndex) + "<…>";
        }

        private static bool IsSimple(SerializedProperty property)
        {
            switch (property.propertyType)
            {
                case SerializedPropertyType.Integer:
                case SerializedPropertyType.Boolean:
                case SerializedPropertyType.Float:
                case SerializedPropertyType.String:
                case SerializedPropertyType.Enum:
                case SerializedPropertyType.ObjectReference:
                    return true;

                default:
                    return false;
            }
        }

    #endregion

    }
}
