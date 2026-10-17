/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : CollectionItemClipboard.cs
수정일 : 2026-10-17

# 설명
Collection Editor row 하나의 SerializedProperty snapshot을 Editor session clipboard로 보관한다.
정적 세션 메모리를 사용해 collection view 사이에서 단일 snapshot을 공유한다. Domain Reload 시 보관한 값은 초기화된다.
========================================================================= BLOCK_HEADER_END */

using UnityEditor;

namespace inonego.Xeri.Serializable.Editor
{
    // ============================================================
    /// <summary>
    /// Collection row Editor-session clipboard.
    /// </summary>
    // ============================================================
    internal static class CollectionItemClipboard
    {

    #region 필드

        private static SerializedPropertySnapshot snapshot = null;

    #endregion

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// source row의 serialized payload를 clipboard에 복사한다.
        /// </summary>
        // ------------------------------------------------------------
        public static bool Copy(SerializedProperty source, out string reason)
        {
            var isCreated = SerializedPropertySnapshot.TryCreate
            (
                source,
                out var nextSnapshot,
                out reason
            );
            if (!isCreated)
            {
                return false;
            }

            snapshot = nextSnapshot;
            return true;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 clipboard가 destination row에 적용 가능한지 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public static bool CanPaste(SerializedProperty destination, out string reason)
        {
            if (snapshot == null)
            {
                reason = "복사한 Collection Item이 없습니다.";
                return false;
            }

            return snapshot.CanApply(destination, out reason);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 clipboard payload를 destination row에 붙여 넣는다.
        /// </summary>
        // ------------------------------------------------------------
        public static bool Paste(SerializedProperty destination, out string reason)
        {
            if (snapshot == null)
            {
                reason = "복사한 Collection Item이 없습니다.";
                return false;
            }

            return snapshot.TryApply(destination, out reason);
        }

    #endregion

    }
}
