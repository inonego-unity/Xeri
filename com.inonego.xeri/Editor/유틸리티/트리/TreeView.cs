/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TreeView.cs
수정일 : 2026-08-05

# 설명
TreeNode를 소비자 제공 행 VisualElement로 재귀 배치하는 Editor UI Toolkit view.

# 특이사항
값 편집과 자식 구성 규칙은 rowFactory가 소유한다.
이 view는 표시 트리의 계층과 들여쓰기만 관리한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections.Generic;

using UnityEngine.UIElements;

namespace inonego.Xeri
{
    // ============================================================
    /// <summary>
    /// 재귀 TreeNode를 계층 구조로 표시하는 UI Toolkit view.
    /// </summary>
    // ============================================================
    public sealed class TreeView<TValue> : VisualElement
    {

    #region 상수

        private const string ClassRoot     = "xeri-tree";
        private const string ClassNode     = "xeri-tree-node";
        private const string ClassChildren = "xeri-tree-children";
        private const float DefaultIndent  = 20f;

    #endregion

    #region 필드

        private readonly Func<TreeNode<TValue>, VisualElement> rowFactory;
        private readonly List<TreeNode<TValue>> roots = new();

        // ------------------------------------------------------------
        /// <summary>
        /// 자식 컨테이너에 적용할 들여쓰기 폭.
        /// </summary>
        // ------------------------------------------------------------
        public float Indent
        {
            get => indent;
            set
            {
                if (value < 0f)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                indent = value;
                Refresh();
            }
        }
        private float indent = DefaultIndent;

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 view가 표시하는 최상위 노드 목록.
        /// </summary>
        // ------------------------------------------------------------
        public IReadOnlyList<TreeNode<TValue>> Roots => roots;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// 노드별 행 VisualElement를 만드는 factory로 tree view를 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        public TreeView(Func<TreeNode<TValue>, VisualElement> rowFactory) : base()
        {
            this.rowFactory = rowFactory ?? throw new ArgumentNullException(nameof(rowFactory));

            AddToClassList(ClassRoot);
        }

    #endregion

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// 표시할 최상위 노드를 교체하고 tree view를 다시 구성한다.
        /// </summary>
        // ------------------------------------------------------------
        public void SetRoots(IEnumerable<TreeNode<TValue>> roots)
        {
            if (roots == null)
            {
                throw new ArgumentNullException(nameof(roots));
            }

            // 최상위 목록은 parent가 없는 독립 트리만 수용해 재귀 표시 시작점을 보장한다.
            this.roots.Clear();
            foreach (var root in roots)
            {
                if (root == null)
                {
                    throw new ArgumentException("최상위 트리 노드에 null을 포함할 수 없습니다.", nameof(roots));
                }

                if (root.Parent != null)
                {
                    throw new ArgumentException("부모를 가진 노드는 최상위 트리 노드가 될 수 없습니다.", nameof(roots));
                }

                this.roots.Add(root);
            }

            Refresh();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 노드 구조와 값을 기준으로 모든 행을 다시 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public void Refresh()
        {
            Clear();
            foreach (var root in roots)
            {
                AppendNode(this, root);
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// node 행과 재귀 child container를 parent 아래에 추가한다.
        /// </summary>
        // ------------------------------------------------------------
        private void AppendNode(VisualElement parent, TreeNode<TValue> node)
        {
            var nodeElement = new VisualElement();
            nodeElement.AddToClassList(ClassNode);

            var row = rowFactory.Invoke(node);
            if (row == null)
            {
                throw new InvalidOperationException("트리 행 factory는 null을 반환할 수 없습니다.");
            }

            nodeElement.Add(row);
            parent.Add(nodeElement);

            if (node.Children.Count == 0)
            {
                return;
            }

            var childrenElement = new VisualElement();
            childrenElement.AddToClassList(ClassChildren);
            childrenElement.style.marginLeft = indent;
            nodeElement.Add(childrenElement);

            foreach (var child in node.Children)
            {
                AppendNode(childrenElement, child);
            }
        }

    #endregion

    }
}
