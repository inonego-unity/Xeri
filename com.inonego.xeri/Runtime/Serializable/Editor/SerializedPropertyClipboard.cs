/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : SerializedPropertyClipboard.cs
수정일 : 2026-10-31

# 설명
Editor 세션에서 SerializedProperty 값 또는 동일 직렬화 루트의 관리 참조 링크 하나를 보관한다.
값 캡처·타입 호환성은 SerializedPropertySnapshot이 처리하며 Undo와 Apply는 호출자가 소유한다.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace inonego.Xeri.Serializable.Editor
{
    // ============================================================
    /// <summary>
    /// 직렬화된 값과 관리 참조 링크를 단일 복사 슬롯으로 제공한다.
    /// </summary>
    // ============================================================
    internal static class SerializedPropertyClipboard
    {

    #region 복사 상태

        private static SerializedPropertySnapshot copiedValue = null;
        private static LinkCopy copiedLink = null;

    #endregion

    #region 값 복사

        // ------------------------------------------------------------
        /// <summary>
        /// 직렬화된 값을 독립적으로 보관하고 이전 복사 내용을 교체한다.
        /// </summary>
        // ------------------------------------------------------------
        public static bool TryCopyValue(SerializedProperty source, out string reason)
        {
            if (!SerializedPropertySnapshot.TryCreate(source, out var snapshot, out reason))
            {
                return false;
            }

            copiedValue = snapshot;
            copiedLink = null;
            return true;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 값이 대상 프로퍼티에 적용 가능한지 확인한다.
        /// </summary>
        // ------------------------------------------------------------
        public static bool CanPasteValue
        (
            SerializedProperty destination,
            out string reason,
            Type declaredType = null
        )
        {
            if (copiedValue == null)
            {
                reason = "복사한 값이 없습니다.";
                return false;
            }

            if (destination == null)
            {
                reason = "대상 프로퍼티가 없습니다.";
                return false;
            }

            if (destination.propertyType == SerializedPropertyType.ManagedReference)
            {
                var type = declaredType ?? ManagedReferenceTypeCatalog.GetDeclaredReferenceType(destination);
                if (!copiedValue.IsManagedReference
                    || type == null
                    || copiedValue.ManagedReferenceType != null
                        && !type.IsAssignableFrom(copiedValue.ManagedReferenceType))
                {
                    reason = "복사한 관리 참조 형식을 대상에 사용할 수 없습니다.";
                    return false;
                }
            }

            return copiedValue.CanApply(destination, out reason, declaredType);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 캡처한 값을 적용한다. 변경 확정과 실패 시 취소는 호출자가 처리한다.
        /// </summary>
        // ------------------------------------------------------------
        public static bool TryPasteValue
        (
            SerializedProperty destination,
            out string reason,
            Type declaredType = null
        )
        {
            if (!CanPasteValue(destination, out reason, declaredType))
            {
                return false;
            }

            return copiedValue.TryApply(destination, out reason, declaredType);
        }

    #endregion

    #region 참조 링크 복사

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 관리 참조를 같은 직렬화 루트의 링크로 보관할 수 있는지 확인한다.
        /// </summary>
        // ------------------------------------------------------------
        public static bool CanCopyLink(SerializedProperty property, out string reason)
        {
            if (!TryGetManagedReference(property, out var value, out reason))
            {
                return false;
            }

            if (value == null)
            {
                reason = "null 참조는 링크로 복사할 수 없습니다.";
                return false;
            }

            if (property.serializedObject.isEditingMultipleObjects)
            {
                reason = "여러 객체의 관리 참조 링크를 한 번에 복사할 수 없습니다.";
                return false;
            }

            var root = property.serializedObject.targetObject;
            var id = property.managedReferenceId;
            var registered = id == 0
                ? null
                : ManagedReferenceUtility.GetManagedReference(root, id);
            if (id == 0 || !ReferenceEquals(registered, value))
            {
                reason = "현재 직렬화 루트에 등록된 관리 참조가 아닙니다.";
                return false;
            }

            reason = null;
            return true;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 관리 참조의 동일 루트 링크를 보관하고 이전 복사 내용을 교체한다.
        /// </summary>
        // ------------------------------------------------------------
        public static bool TryCopyLink(SerializedProperty property, out string reason)
        {
            if (!CanCopyLink(property, out reason))
            {
                return false;
            }

            copiedLink = new LinkCopy
            (
                property.serializedObject.targetObject,
                property.managedReferenceId,
                property.managedReferenceValue
            );
            copiedValue = null;
            reason = null;
            return true;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 복사한 링크를 같은 직렬화 루트에 연결할 수 있는지 검사한다.
        /// </summary>
        // ------------------------------------------------------------
        public static bool CanPasteLink
        (
            SerializedProperty destination,
            Type declaredType,
            out string reason
        )
        {
            if (!TryGetManagedReference(destination, out _, out reason))
            {
                return false;
            }

            if (copiedLink == null)
            {
                reason = "복사한 관리 참조 링크가 없습니다.";
                return false;
            }

            if (destination.serializedObject.isEditingMultipleObjects)
            {
                reason = "여러 객체에 링크를 붙여넣을 수 없습니다.";
                return false;
            }

            var root = destination.serializedObject.targetObject;
            if (!ReferenceEquals(root, copiedLink.Root))
            {
                reason = "같은 직렬화 루트에만 링크를 붙여넣을 수 있습니다.";
                return false;
            }

            var registered = copiedLink.ID == 0
                ? null
                : ManagedReferenceUtility.GetManagedReference(root, copiedLink.ID);
            if (copiedLink.Reference == null
                || !ReferenceEquals(registered, copiedLink.Reference))
            {
                reason = "원본 관리 참조가 현재 직렬화 루트에 없습니다.";
                return false;
            }

            var type = copiedLink.Reference.GetType();
            if (declaredType == null || !declaredType.IsAssignableFrom(type))
            {
                reason = "관리 참조 형식이 대상과 호환되지 않습니다.";
                return false;
            }

            reason = null;
            return true;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 같은 루트의 원본 인스턴스를 연결한다. Apply는 호출자가 처리한다.
        /// </summary>
        // ------------------------------------------------------------
        public static bool TryPasteLink
        (
            SerializedProperty destination,
            Type declaredType,
            out string reason
        )
        {
            if (!CanPasteLink(destination, declaredType, out reason))
            {
                return false;
            }

            destination.managedReferenceValue = copiedLink.Reference;
            return true;
        }

    #endregion

    #region 수명

        // ------------------------------------------------------------
        /// <summary>
        /// Editor 세션에 보관된 복사 값을 비운다.
        /// </summary>
        // ------------------------------------------------------------
        public static void Clear()
        {
            copiedValue = null;
            copiedLink = null;
        }

    #endregion

    #region 내부 구현

        private static bool TryGetManagedReference
        (
            SerializedProperty property, out object value, out string reason
        )
        {
            value = null;
            reason = null;
            if (property == null || property.propertyType != SerializedPropertyType.ManagedReference)
            {
                reason = "관리 참조 프로퍼티가 아닙니다.";
                return false;
            }

            value = property.managedReferenceValue;
            return true;
        }

        // ============================================================
        /// <summary>
        /// 링크의 원본 루트·ID·인스턴스를 함께 보관한다.
        /// </summary>
        // ============================================================
        private sealed class LinkCopy
        {
            public UnityEngine.Object Root { get; }
            public long ID { get; }
            public object Reference { get; }

            public LinkCopy(UnityEngine.Object root, long id, object reference)
            {
                Root = root;
                ID = id;
                Reference = reference;
            }
        }

    #endregion

    }
}
