/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : SerializedPropertyBaseline.cs
수정일 : 2026-11-25

# 설명
한 직렬화 프로퍼티의 마지막으로 확인한 상태를 보관하고, 현재 상태가 그 기준과 같은지 비교한다.
번호는 올리지 않으며 "바뀌었는가"의 판단만 맡는다.

# 특이사항, 제약사항
SerializeReference가 없으면 SerializedProperty.contentHash만 쓰고, 있으면 값 스냅샷으로 비교한다.
SerializeReference 내용은 별도 참조 저장소에 있어 contentHash에 반영되지 않기 때문이다.
프로퍼티가 없으면(null) "없음" 상태를 기준으로 잡는다. 있다가 없어지거나 없다가 생기면 바뀐 것으로 보고, 계속 없으면 같은 상태다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEditor;

using inonego;
using inonego.Xeri;
using inonego.Xeri.Editor;

namespace inonego.Xeri.Serializable.Editor
{
    // ============================================================
    /// <summary>
    /// 한 프로퍼티의 마지막 직렬화 상태와 그 비교.
    /// </summary>
    // ============================================================
    internal sealed class SerializedPropertyBaseline
    {

    #region 상태

        // ------------------------------------------------------------
        /// <summary>
        /// 비교 기준을 한 번이라도 잡았는지 나타낸다.
        /// </summary>
        // ------------------------------------------------------------
        public bool IsCaptured { get; private set; }

        // SerializeReference가 있어 엔진 해시 대신 값 스냅샷으로 비교하는지 나타낸다.
        private bool usesSnapshot;

        // 기준을 잡을 때 프로퍼티가 없었는지 나타낸다.
        private bool isMissing;

        // 해시 비교에 쓰는 마지막 기준 해시다.
        private uint contentHash;

        // 스냅샷 비교에 쓰는 마지막 기준 값이며 해시 비교면 null이다.
        private SerializedPropertySnapshot snapshot;

    #endregion

    #region 캡처와 비교

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 이후 비교에 쓸 직렬화 상태를 기준으로 잡는다.
        /// <br/> property가 null이면 "없음" 상태를 기준으로 잡는다.
        /// </summary>
        // ------------------------------------------------------------
        public void Capture(SerializedProperty property)
        {
            // 캡처 실패는 미관찰 상태와 구분해 이후 변경 검증을 보수적으로 처리한다.
            IsCaptured = true;
            isMissing = property == null;

            // SerializeReference 내용은 별도 참조 저장소에 있어 contentHash에 반영되지 않는다.
            usesSnapshot = property != null && ManagedReferenceTypeCatalog.ContainsManagedReference(property);
            snapshot = null;
            contentHash = property?.contentHash ?? 0;

            if (usesSnapshot)
            {
                SerializedPropertySnapshot.TryCreate(property, out snapshot, out _);
            }
        }

        // --------------------------------------------------------------------------------
        /// <summary>
        /// 마지막 기준과 현재 직렬화 상태가 같은지 반환한다. 기준과 현재가 모두 "없음"이면 같다.
        /// </summary>
        // --------------------------------------------------------------------------------
        public bool Matches(SerializedProperty property)
        {
            if (property == null)
            {
                return isMissing;
            }

            if (isMissing)
            {
                return false;
            }

            return usesSnapshot
                ? snapshot?.Matches(property) == true
                : property.contentHash == contentHash;
        }

    #endregion

    }
}
