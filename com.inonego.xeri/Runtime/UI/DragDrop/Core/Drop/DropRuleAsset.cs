/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : DropRuleAsset.cs
수정일 : 2026-05-22

# 설명
Inspector에서 참조 가능한 ScriptableObject 기반 드롭 규칙 베이스.
========================================================================= BLOCK_HEADER_END */

using UnityEngine;

namespace inonego.Xeri.UI.DragDrop
{
    // ============================================================
    /// <summary>
    /// ScriptableObject 기반 드롭 규칙.
    /// </summary>
    // ============================================================
    public abstract class DropRuleAsset : ScriptableObject, IDropRule
    {

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// 드롭 가능 여부를 확인한다.
        /// </summary>
        // ------------------------------------------------------------
        public abstract bool CanDrop(Draggable draggable, DropZone dropZone);

    #endregion

    }
}
