/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : RevisionTracker.cs
수정일 : 2026-11-01

# 설명
Unity Editor에서 객체 및 프로퍼티 경로별 공유 변경 세대를 보관하고 갱신한다.
관련 경로의 변경과 Undo/Redo는 이전 세대를 무효화한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime;
using System.Runtime.CompilerServices;

using UnityEditor;
using UnityEngine;

namespace inonego.Xeri
{
    // ============================================================
    /// <summary>
    /// Editor 객체의 프로퍼티별 변경 세대를 공유한다.
    /// </summary>
    // ============================================================
    internal static class RevisionTracker
    {

    #region 내부 데이터

        // ============================================================
        /// <summary>
        /// 객체의 프로퍼티 경로와 변경 세대를 연결한다.
        /// </summary>
        // ============================================================
        private sealed class RevisionMap : Dictionary<string, ulong>
        {
            // ------------------------------------------------------------
            /// <summary>
            /// ConditionalWeakTable에서 생성할 수 있는 기본 생성자.
            /// </summary>
            // ------------------------------------------------------------
            public RevisionMap()
            {
                // NONE
            }
        }

    #endregion

    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// 대상 객체의 GC를 방해하지 않는 경로별 변경 세대 저장소.
        /// </summary>
        // ------------------------------------------------------------
        private static readonly ConditionalWeakTable<UnityEngine.Object, RevisionMap> revisions = new();
        private static ulong nextRevision;
        private static ulong undoRevision;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// Undo/Redo 발생 시 전역 변경 세대를 갱신하도록 구독한다.
        /// </summary>
        // ------------------------------------------------------------
        static RevisionTracker()
        {
            Undo.undoRedoPerformed += InvalidateAll;
        }

    #endregion

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// 대상의 프로퍼티 경로에 해당하는 현재 변경 세대를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public static ulong Get(UnityEngine.Object target, string path)
        {
            var byPath = revisions.GetOrCreateValue(target);
            if (!byPath.TryGetValue(path, out var revision))
            {
                revision = undoRevision;
                byPath.Add(path, revision);
            }

            return Math.Max(revision, undoRevision);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 변경된 경로와 관련된 상위·하위 경로의 세대를 갱신한다.
        /// </summary>
        // ------------------------------------------------------------
        public static void Advance(UnityEngine.Object target, string changedPath)
        {
            var byPath = revisions.GetOrCreateValue(target);
            var revision = ++nextRevision;
            byPath[changedPath] = revision;

            foreach (var path in byPath.Keys)
            {
                if (path == changedPath
                    || path.StartsWith(changedPath + ".", StringComparison.Ordinal)
                    || changedPath.StartsWith(path + ".", StringComparison.Ordinal))
                {
                    byPath[path] = revision;
                }
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Undo/Redo 후 기존에 캡처한 모든 Revision을 무효화한다.
        /// </summary>
        // ------------------------------------------------------------
        public static void InvalidateAll()
        {
            undoRevision = ++nextRevision;
        }

    #endregion

    }
}
