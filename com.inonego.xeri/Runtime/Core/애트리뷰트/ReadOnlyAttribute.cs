/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : ReadOnlyAttribute.cs
수정일 : 2026-04-30

# 설명
Inspector에서 필드를 읽기 전용으로 표시하는 PropertyAttribute.
PropertyDrawer는 Editor/ReadOnlyDrawer.cs에서 별도로 구현한다.
========================================================================= BLOCK_HEADER_END */

using UnityEngine;

namespace inonego.Xeri
{
    // ============================================================
    /// <summary>
    /// Inspector에서 필드를 읽기 전용으로 표시하는 애트리뷰트.
    /// </summary>
    // ============================================================
    public class ReadOnlyAttribute : PropertyAttribute { }
}
