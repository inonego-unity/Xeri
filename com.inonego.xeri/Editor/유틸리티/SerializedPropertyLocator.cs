/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : SerializedPropertyLocator.cs
수정일 : 2026-10-06

# 설명
지연된 Picker와 Detail 편집의 target/path를 현재 SerializedObject에서 다시 해석한다.
배열 요소와 managed-reference identity가 바뀐 경로는 재사용하지 않는다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEditor;

namespace inonego.Xeri
{
    // ============================================================
    /// <summary>
    /// 지연된 편집 명령의 property 대상과 배열 요소 identity를 보관한다.
    /// </summary>
    // ============================================================
    internal sealed class SerializedPropertyLocator
    {

    #region 필드

        private readonly UnityEngine.Object target;
        private readonly string propertyPath;
        private readonly List<(string Path, object Value)> ancestors = new();
        private readonly bool trackReference;
        private long referenceID;

    #endregion

    #region 캡처와 해석

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 property와 배열 요소의 identity를 캡처한다.
        /// </summary>
        // ------------------------------------------------------------
        public SerializedPropertyLocator(SerializedProperty property)
        {
            target = property.serializedObject.targetObject;
            propertyPath = property.propertyPath;
            trackReference = property.propertyType == SerializedPropertyType.ManagedReference;
            Refresh(property);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 같은 항목의 자체 편집을 확정한 뒤 identity를 갱신한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Refresh(SerializedProperty property)
        {
            ancestors.Clear();
            var offset = 0;
            while ((offset = propertyPath.IndexOf(".Array.data[", offset, StringComparison.Ordinal)) >= 0)
            {
                var end = propertyPath.IndexOf(']', offset);
                var path = propertyPath[..(end + 1)];
                var ancestor = property.serializedObject.FindProperty(path);
                ancestors.Add((path, SerializedPropertyHelper.GetTargetObject(ancestor)));
                offset = end + 1;
            }

            if (trackReference)
            {
                referenceID = property.managedReferenceId;
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 target에 원본 요소가 유지될 때만 property를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public bool TryResolve
        (
            SerializedObject serializedObject,
            out SerializedProperty property,
            out string reason
        )
        {
            property = null;
            reason = "편집 대상이 이동하거나 교체되었습니다. 현재 항목에서 다시 여세요.";
            if (target == null || serializedObject.targetObject != target)
            {
                return false;
            }

            serializedObject.UpdateIfRequiredOrScript();
            foreach (var ancestor in ancestors)
            {
                var current = serializedObject.FindProperty(ancestor.Path);
                if (current == null)
                {
                    return false;
                }

                // 같은 경로가 존재하더라도 다른 row로 바뀌었으면 명령을 실행하지 않는다.
                var value = SerializedPropertyHelper.GetTargetObject(current);
                var matches = ancestor.Value == null
                    ? value == null
                    : ancestor.Value.GetType().IsValueType || ancestor.Value is string
                        ? Equals(ancestor.Value, value)
                        : ReferenceEquals(ancestor.Value, value);
                if (!matches)
                {
                    return false;
                }
            }

            property = serializedObject.FindProperty(propertyPath);
            if (property == null || trackReference
                && (property.propertyType != SerializedPropertyType.ManagedReference
                    || property.managedReferenceId != referenceID))
            {
                property = null;
                return false;
            }

            reason = string.Empty;
            return true;
        }

    #endregion

    }
}
