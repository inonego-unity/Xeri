/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : XPriorityQueueKindProvider.cs
수정일 : 2026-11-22

# 설명
XPriorityQueue<TElement, TPriority>의 종류 정의를 만든다.
표에는 우선순위가 높은 행부터 보이지만 직렬화된 힙 순서는 바꾸지 않는다. 우선순위는 입력이 끝났을 때 확정한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

using UnityEditor;

using inonego;
using inonego.Xeri;
using inonego.Xeri.UI;
using inonego.Xeri.Editor;
using inonego.Xeri.UI.TableGrid;

namespace inonego.Xeri.Serializable.Editor
{
    // ============================================================
    /// <summary>
    /// XPriorityQueue 종류 Provider.
    /// </summary>
    // ============================================================
    internal sealed class XPriorityQueueKindProvider : ICollectionKindProvider
    {

    #region 필드

        private const string elementColumnID = "Element";
        private const string priorityColumnID = "Priority";

    #endregion

    #region 종류 생성

        // ------------------------------------------------------------
        /// <summary>
        /// XPriorityQueue 속성이면 종류 정의를 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public bool TryCreate(SerializedProperty property, FieldInfo field, out CollectionKind kind)
        {
            var instance = SerializedPropertyHelper.GetTargetObject(property);
            var type = CollectionKinds.FindGenericType(instance?.GetType(), typeof(XPriorityQueue<,>));
            kind = type != null ? CreateFromType(type) : null;
            return kind != null;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 찾은 XPriorityQueue 형식으로 종류 정의를 만든다.
        /// </summary>
        // ------------------------------------------------------------
        private static CollectionKind CreateFromType(Type type)
        {
            var arguments = type.GetGenericArguments();
            var element = new CollectionColumn
            (
                elementColumnID, "항목", TableGridColumnLayout.Flexible(220f, 100f, 0f, 1f), elementColumnID,
                arguments[0], true, CollectionCellEdit.Inline, true
            );
            var priority = new CollectionColumn
            (
                priorityColumnID, "우선순위", TableGridColumnLayout.Flexible(100f, 100f, 0f, 1f), priorityColumnID,
                arguments[1], false, CollectionCellEdit.Delayed
            );

            var compare = typeof(XPriorityQueueKindProvider)
                .GetMethod(nameof(CreateDescending), BindingFlags.NonPublic | BindingFlags.Static)
                .MakeGenericMethod(arguments[1]);
            return new CollectionKind
            (
                "XPriorityQueue", "우선순위가 높은 항목부터 표시하며, 내부 저장 순서와 다릅니다.",
                new[]
                {
                    element, priority
                }, "heap",
                CollectionEdits.Add | CollectionEdits.Remove | CollectionEdits.Duplicate | CollectionEdits.Paste,
                CollectionSync.Normalize,
                displayOrderColumn: priority,
                displayOrderComparison: (Comparison<object>)compare.Invoke(null, null)
            );
        }

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> 우선순위가 높은 값이 앞에 오는 비교를 만든다.
        /// <br/> 우선순위는 제네릭 IComparable만 구현할 수 있어 비제네릭 Comparer로 대신하지 않는다.
        /// </summary>
        // --------------------------------------------------------------------------------
        private static Comparison<object> CreateDescending<T>()
        {
            return (left, right) => Comparer<T>.Default.Compare((T)right, (T)left);
        }

    #endregion

    }
}
