/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : CollectionAddFlow.cs
수정일 : 2026-11-19

# 설명
컬렉션에 새 행을 추가하는 절차를 담당한다.
추가 전에 값을 만들어야 하는 열은 해당 셀 drawer의 값 생성 능력으로 형식을 차례로 고른 뒤 값을 만든다.
초안 열이 있으면 초안 창에서 검증해 추가하고, 없으면 바로 AddRow 편집을 적용한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UIElements;

using inonego;
using inonego.Xeri;
using inonego.Xeri.Editor;

namespace inonego.Xeri.Serializable.Editor
{
    // ============================================================
    /// <summary>
    /// 컬렉션 행 추가 절차.
    /// </summary>
    // ============================================================
    internal sealed class CollectionAddFlow
    {

    #region 필드

        private readonly CollectionSession session;
        private readonly Func<bool> isAlive;
        private readonly Action<string> onFailed;
        private readonly Action<CollectionEditResult> onAdded;
        private readonly Action<int> onDraftAdded;
        private readonly Action beforeAdd;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// 세션과 결과 처리 콜백으로 추가 절차를 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public CollectionAddFlow
        (
            CollectionSession session,
            Func<bool> isAlive,
            Action<string> onFailed,
            Action<CollectionEditResult> onAdded,
            Action<int> onDraftAdded,
            Action beforeAdd
        )
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
            this.isAlive = isAlive ?? throw new ArgumentNullException(nameof(isAlive));
            this.onFailed = onFailed ?? throw new ArgumentNullException(nameof(onFailed));
            this.onAdded = onAdded ?? throw new ArgumentNullException(nameof(onAdded));
            this.onDraftAdded = onDraftAdded ?? throw new ArgumentNullException(nameof(onDraftAdded));
            this.beforeAdd = beforeAdd ?? throw new ArgumentNullException(nameof(beforeAdd));
        }

    #endregion

    #region 절차

        // ------------------------------------------------------------
        /// <summary>
        /// 값을 먼저 만들어야 하는 열이 있으면 열마다 형식을 고른 뒤 추가한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Start(VisualElement anchor)
        {
            var columns = new List<CollectionColumn>();
            foreach (var column in session.Kind.Columns)
            {
                if (column.CreatesValueOnAdd)
                {
                    columns.Add(column);
                }
            }

            if (columns.Count == 0)
            {
                beforeAdd();
                onAdded(session.Apply(CollectionEdit.AddRow(CollectionDraft.ForNewRow())));
                return;
            }

            var creators = new List<ICollectionCellValueCreator>(columns.Count);
            foreach (var column in columns)
            {
                var creator = CollectionCellDrawers.Resolve(column).ValueCreator;
                if (creator == null)
                {
                    onFailed("값을 만드는 셀 drawer가 없어 행을 추가하지 않았습니다.");
                    return;
                }

                creators.Add(creator);
            }

            // 선택기를 연 뒤 상위 배열 요소가 바뀌면 다른 컬렉션에 추가하지 않는다.
            var locator = new SerializedPropertyLocator(session.Source.GetProperty());
            ChooseTypes(columns, creators, anchor, 0, new List<Type>(columns.Count), locator);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 대상 열의 형식을 차례로 고르고 모두 고르면 값을 만든다.
        /// </summary>
        // ------------------------------------------------------------
        private void ChooseTypes
        (
            IReadOnlyList<CollectionColumn> columns,
            IReadOnlyList<ICollectionCellValueCreator> creators,
            VisualElement anchor,
            int index,
            List<Type> chosen,
            SerializedPropertyLocator locator
        )
        {
            if (index == columns.Count)
            {
                AddWithValues(columns, creators, chosen);
                return;
            }

            creators[index].PickTypeForNew
            (
                columns[index].ValueType, anchor,
                type =>
                {
                    if (!isAlive())
                    {
                        return;
                    }

                    if (!locator.TryResolve(session.SerializedObject, out _, out var reason))
                    {
                        onFailed(reason);
                        return;
                    }

                    chosen.Add(type);
                    ChooseTypes(columns, creators, anchor, index + 1, chosen, locator);
                },
                onFailed
            );
        }

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 고른 형식으로 값을 만들어 새 행 초안에 넣는다.
        /// <br/> 초안 열이 있으면 초안 창에서 확정한다.
        /// </summary>
        // ------------------------------------------------------------
        private void AddWithValues
        (
            IReadOnlyList<CollectionColumn> columns,
            IReadOnlyList<ICollectionCellValueCreator> creators,
            IReadOnlyList<Type> types
        )
        {
            var draft = CollectionDraft.ForNewRow();
            CollectionColumn draftColumn = null;
            object draftValue = null;
            for (var index = 0; index < columns.Count; index++)
            {
                if (!creators[index].TryCreate(types[index], out var value, out var reason))
                {
                    onFailed(reason);
                    return;
                }

                if (columns[index].Edit == CollectionCellEdit.Draft)
                {
                    draftColumn = columns[index];
                    draftValue = value;
                    continue;
                }

                draft.Set(columns[index].ID, value);
            }

            if (draftColumn != null)
            {
                CollectionDraftWindow.OpenForAdd(session, draftColumn, draftValue, draft, onDraftAdded);
                return;
            }

            beforeAdd();
            onAdded(session.Apply(CollectionEdit.AddRow(draft)));
        }

    #endregion

    }
}
