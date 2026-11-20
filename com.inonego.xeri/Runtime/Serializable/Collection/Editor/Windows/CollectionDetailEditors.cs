/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : CollectionDetailEditors.cs
수정일 : 2026-11-19

# 설명
셀·행의 상세 편집을 어떤 창으로 열지 정한다.
읽기 전용은 상세 보기, 초안 열은 초안 창, 복합 값은 값 상세 창으로 연다.
단순 값은 표 안에서 바로 고치므로 창을 열지 않는다.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEditor;

using inonego;
using inonego.Xeri;
using inonego.Xeri.Editor;

namespace inonego.Xeri.Serializable.Editor
{
    // ============================================================
    /// <summary>
    /// 셀·행 상세 편집 창 선택.
    /// </summary>
    // ============================================================
    internal static class CollectionDetailEditors
    {

    #region 열기

        // ------------------------------------------------------------
        /// <summary>
        /// 셀의 편집 방식에 맞는 상세 창을 열고 열었는지 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public static bool OpenCell
        (
            CollectionSession session, int sourceIndex, CollectionColumn column, Action<int> onApplied = null
        )
        {
            if (session == null || column == null || sourceIndex < 0 || sourceIndex >= session.Source.Count)
            {
                return false;
            }

            var property = session.Source.GetCell(sourceIndex, column);
            if (property == null)
            {
                return false;
            }

            if (session.IsReadOnly || column.Edit == CollectionCellEdit.ReadOnly)
            {
                CollectionValueEditorWindow.Open(session, property, true);
                return true;
            }

            if (column.Edit == CollectionCellEdit.Draft)
            {
                return CollectionDraftWindow.OpenForRow(session, sourceIndex, column, onApplied);
            }

            // 단순 값과 빈 참조는 상세 창에서 고칠 내용이 없다.
            if (!IsComplex(property) || SerializedPropertyHelper.GetValue(property) == null)
            {
                return false;
            }

            CollectionValueEditorWindow.Open(session, property);
            return true;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 행 전체를 값 상세 창으로 연다.
        /// </summary>
        // ------------------------------------------------------------
        public static void OpenRow(CollectionSession session, int sourceIndex)
        {
            if (session != null && sourceIndex >= 0 && sourceIndex < session.Source.Count)
            {
                CollectionValueEditorWindow.Open(session, session.Source.GetRow(sourceIndex), session.IsReadOnly);
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 행 구조를 바꾸기 전에 컬렉션의 값 상세 창을 닫는다.
        /// </summary>
        // ------------------------------------------------------------
        public static void Close(CollectionSession session)
        {
            if (session != null)
            {
                CollectionValueEditorWindow.CloseForCollection(session.SerializedObject.targetObject, session.Source.Path);
            }
        }

        private static bool IsComplex(SerializedProperty property)
        {
            return property.isArray && property.propertyType != SerializedPropertyType.String
                || property.propertyType == SerializedPropertyType.Generic
                || property.propertyType == SerializedPropertyType.ManagedReference;
        }

    #endregion

    }
}
