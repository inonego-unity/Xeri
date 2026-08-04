/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TreeNode.cs
수정일 : 2026-08-05

# 설명
에디터 UI에서 재귀적으로 표시할 값 노드와 부모-자식 관계를 표현한다.

# 특이사항
값의 의미와 행 UI는 소비자가 결정한다.
이 타입은 노드 계층과 자식 소유 관계만 관리한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections.Generic;

namespace inonego.Xeri
{
    // ============================================================
    /// <summary>
    /// 재귀 편집 트리의 값 노드.
    /// </summary>
    // ============================================================
    public sealed class TreeNode<TValue>
    {

    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// 소비자가 해석하는 현재 노드 값.
        /// </summary>
        // ------------------------------------------------------------
        public TValue Value
        {
            get => value;
            set => this.value = value;
        }
        private TValue value;

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 노드를 소유하는 부모 노드.
        /// </summary>
        // ------------------------------------------------------------
        public TreeNode<TValue> Parent => parent;
        private TreeNode<TValue> parent = null;

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 노드가 소유하는 직접 자식 노드.
        /// </summary>
        // ------------------------------------------------------------
        public IReadOnlyList<TreeNode<TValue>> Children => children;
        private readonly List<TreeNode<TValue>> children = new();

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// 지정한 값으로 트리 노드를 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        public TreeNode(TValue value) : base()
        {
            this.value = value;
        }

    #endregion

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// 아직 소유자가 없는 child를 현재 노드에 추가한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Add(TreeNode<TValue> child)
        {
            if (child == null)
            {
                throw new ArgumentNullException(nameof(child));
            }

            // 순환 참조는 재귀 표시와 자식 수명 계약을 모두 깨뜨린다.
            if (child == this)
            {
                throw new ArgumentException("자기 자신을 자식 노드로 추가할 수 없습니다.", nameof(child));
            }

            if (child.parent != null)
            {
                throw new InvalidOperationException("이미 부모에 속한 트리 노드는 추가할 수 없습니다.");
            }

            child.parent = this;
            children.Add(child);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 노드가 소유한 모든 직접 자식을 분리한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Clear()
        {
            // 분리된 자식도 독립된 트리로 다시 사용할 수 있도록 부모 연결을 먼저 해제한다.
            foreach (var child in children)
            {
                child.parent = null;
            }

            children.Clear();
        }

    #endregion

    }
}
