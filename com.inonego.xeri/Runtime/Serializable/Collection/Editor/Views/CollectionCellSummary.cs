/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : CollectionCellSummary.cs
수정일 : 2026-10-31

# 설명
Editor의 SerializedProperty에서 Collection Cell 요약과 검색 인덱스 문자열을 생성한다.
Runtime 표시용 계약을 추가하지 않고 작은 복합 타입은 직접 필드 1~2개로 요약한다.

# 특이사항, 제약사항
중첩·복합 필드나 세 개 이상의 직접 필드는 임의로 일부를 생략하지 않고 타입 이름으로 되돌린다.
출력 문자열은 Dictionary Key 동등성이나 SerializeReference 수명 판단에 사용하지 않는다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

using UnityEditor;

using inonego;
using inonego.Xeri;
using inonego.Xeri.UI;

namespace inonego.Xeri.Serializable.Editor
{
    // ============================================================
    /// <summary>
    /// 기본 값·자동 요약·타입 Fallback 순서를 중앙에서 결정한다.
    /// </summary>
    // ============================================================
    internal static class CollectionCellSummary
    {

    #region 요약

        // ------------------------------------------------------------
        /// <summary>
        /// 실제 셀 표시와 타입 표시를 함께 사용해 검색 인덱스를 구성한다.
        /// </summary>
        // ------------------------------------------------------------
        public static string GetSearchText(ICollectionEditorModel model, int sourceIndex)
        {
            var builder = new StringBuilder();
            foreach (var column in model.Columns)
            {
                var property = model.GetCellProperty(sourceIndex, column.ID);
                SearchTextUtility.Append(builder, GetText(property));
                SearchTextUtility.Append(builder, GetDisplayText(property));
            }

            return builder.ToString();
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// compact UI와 search index에서 사용할 property 표시 문자열을 반환한다.
        /// </summary>
        // ----------------------------------------------------------------------
        public static string GetDisplayText(SerializedProperty property)
        {
            if (property == null)
            {
                return string.Empty;
            }

            if (property.isArray
                && property.propertyType != SerializedPropertyType.String)
            {
                return $"{property.arraySize} Items";
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

                case SerializedPropertyType.ManagedReference:
                    return property.managedReferenceValue != null
                        ? ObjectNames.NicifyVariableName(property.managedReferenceValue.GetType().Name)
                        : "None";

                case SerializedPropertyType.Generic:
                    return ObjectNames.NicifyVariableName(property.type);

                default:
                    return property.displayName;
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Key와 Value에 동일한 정책을 사용하여 셀 표시 텍스트를 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        public static string GetText(SerializedProperty property)
        {
            if (property == null)
            {
                return "Missing";
            }

            if (property.isArray && property.propertyType != SerializedPropertyType.String)
            {
                return property.arraySize + " items";
            }

            var type = property.propertyType;
            if (type == SerializedPropertyType.ManagedReference
                && property.managedReferenceValue == null)
            {
                return "None";
            }

            if (type != SerializedPropertyType.ManagedReference
                && type != SerializedPropertyType.Generic)
            {
                return GetDisplayText(property);
            }

            if (TryBuildAutomaticSummary(property, out var summary))
            {
                return summary;
            }

            var name = type == SerializedPropertyType.ManagedReference
                ? property.managedReferenceValue?.GetType().Name
                : property.type;
            if (string.IsNullOrEmpty(name))
            {
                return "None";
            }

            var genericIndex = name.IndexOf((char)96);
            return genericIndex < 0
                ? name
                : name.Substring(0, genericIndex) + "<…>";
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 직접 자식이 모두 단순 타입이고 최대 두 개인 경우에만 값을 요약한다.
        /// </summary>
        // ------------------------------------------------------------
        private static bool TryBuildAutomaticSummary(SerializedProperty property, out string summary)
        {
            summary = null;
            var iterator = property.Copy();
            var parentDepth = iterator.depth;
            if (!iterator.NextVisible(true))
            {
                return false;
            }

            var parts = new List<string>(2);
            do
            {
                if (iterator.depth <= parentDepth)
                {
                    break;
                }

                if (iterator.depth != parentDepth + 1)
                {
                    continue;
                }

                if (!IsSimple(iterator) || parts.Count >= 2)
                {
                    return false;
                }

                parts.Add(GetDisplayText(iterator));
            }
            while (iterator.NextVisible(false));

            if (parts.Count == 0)
            {
                return false;
            }

            summary = string.Join(" · ", parts);
            return true;
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
