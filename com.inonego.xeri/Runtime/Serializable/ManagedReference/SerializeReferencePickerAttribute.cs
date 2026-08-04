/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : SerializeReferencePickerAttribute.cs
수정일 : 2026-08-04

# 설명
SerializeReference 필드에 Xeri managed-reference picker UI를 opt-in으로 적용하는 attribute.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEngine;

namespace inonego.Xeri.Serializable
{
    // ============================================================
    /// <summary>
    /// managed-reference 생성과 clipboard UI를 활성화한다.
    /// </summary>
    // ============================================================
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class SerializeReferencePickerAttribute : PropertyAttribute
    {
        // NONE
    }
}
