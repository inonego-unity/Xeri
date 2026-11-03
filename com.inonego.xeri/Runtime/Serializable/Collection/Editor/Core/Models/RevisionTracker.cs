/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : RevisionTracker.cs
수정일 : 2026-11-03

# 설명
Collection Editor에서 Unity 객체와 프로퍼티 경로별 변경 세대와 마지막 직렬화 상태를 공유한다.
여러 Inspector의 중복 변경 관찰을 하나로 합치고 관련된 상위·하위 경로를 함께 무효화한다.
Undo/Redo는 보수적으로 이전 변경 세대를 무효화한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime;
using System.Runtime.CompilerServices;

using UnityEditor;
using UnityEngine;
using UnityEngine.Pool;

using inonego;
using inonego.Xeri;

namespace inonego.Xeri.Serializable.Editor
{
    // ============================================================
    /// <summary>
    /// 객체·컬렉션 경로별 직렬화 상태와 변경 세대를 공유한다.
    /// </summary>
    // ============================================================
    internal static class RevisionTracker
    {

    #region 내부 데이터

        // ============================================================
        /// <summary>
        /// 하나의 경로에서 마지막으로 확인한 값과 변경 세대.
        /// </summary>
        // ============================================================
        private sealed class RevisionState
        {
            public ulong Revision;
            public SerializedPropertySnapshot Snapshot;
            public bool HasBaseline;
        }

        // ============================================================
        /// <summary>
        /// 한 Unity 객체에 속한 프로퍼티별 변경 상태.
        /// </summary>
        // ============================================================
        private sealed class RevisionMap : Dictionary<string, RevisionState>
        {
            // ------------------------------------------------------------
            /// <summary>
            /// ConditionalWeakTable에서 호출할 기본 생성자.
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
        /// 대상 객체가 수거되면 연관된 경로 상태도 수거된다.
        /// </summary>
        // ------------------------------------------------------------
        private static readonly ConditionalWeakTable<UnityEngine.Object, RevisionMap> revisions = new();
        private static ulong nextRevision;
        private static ulong undoRevision;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// Undo/Redo 후 기존 편집 명령을 보수적으로 무효화한다.
        /// </summary>
        // ------------------------------------------------------------
        static RevisionTracker()
        {
            Undo.undoRedoPerformed += InvalidateAll;
        }

    #endregion

    #region 조회와 관찰

        // ------------------------------------------------------------
        /// <summary>
        /// 대상 경로의 공유 변경 세대를 조회한다.
        /// </summary>
        // ------------------------------------------------------------
        public static ulong Get(UnityEngine.Object target, string path)
        {
            var byPath = revisions.GetOrCreateValue(target);
            var state = GetOrCreate(byPath, path);
            return Math.Max(state.Revision, undoRevision);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 다른 Inspector에서도 동일한 직렬화 변경을 한 번만 기록한다.
        /// </summary>
        // ------------------------------------------------------------
        public static bool Observe(SerializedProperty property)
        {
            var target = property.serializedObject.targetObject;
            var path = property.propertyPath;
            var byPath = revisions.GetOrCreateValue(target);
            var state = GetOrCreate(byPath, path);
            if (!state.HasBaseline)
            {
                // 세대 조회만으로 등록된 경로는 첫 관찰을 기준 상태로 삼는다.
                CaptureSnapshot(state, property);
                return false;
            }

            if (state.Snapshot?.Matches(property) == true)
            {
                return false;
            }

            Advance(property.serializedObject, path);
            return true;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 등록되지 않은 경로를 현재 Undo 세대에서 시작한다.
        /// </summary>
        // ------------------------------------------------------------
        private static RevisionState GetOrCreate(RevisionMap byPath, string path)
        {
            if (!byPath.TryGetValue(path, out var state))
            {
                state = new RevisionState
                {
                    Revision = undoRevision,
                };
                byPath.Add(path, state);
            }

            return state;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 이후 관찰과 비교할 직렬화 상태를 갱신한다.
        /// </summary>
        // ------------------------------------------------------------
        private static void CaptureSnapshot(RevisionState state, SerializedProperty property)
        {
            // 캡처 실패는 미관찰 상태와 구분해 이후 변경 검증을 보수적으로 처리한다.
            state.HasBaseline = true;
            SerializedPropertySnapshot.TryCreate(property, out state.Snapshot, out _);
        }

    #endregion

    #region 변경 세대

        // ------------------------------------------------------------
        /// <summary>
        /// 관련 경로의 변경 세대와 관찰 기준을 함께 갱신한다.
        /// </summary>
        // ------------------------------------------------------------
        public static void Advance(SerializedObject serializedObject, string changedPath)
        {
            var byPath = revisions.GetOrCreateValue(serializedObject.targetObject);
            GetOrCreate(byPath, changedPath);
            var revision = ++nextRevision;

            // Dictionary 순회와 수정 단계를 분리하고 임시 목록은 Pool에 반환한다.
            var paths = ListPool<string>.Get();
            try
            {
                foreach (var path in byPath.Keys)
                {
                    if (SerializedPropertyHelper.IsRelatedPath(path, changedPath))
                    {
                        paths.Add(path);
                    }
                }

                foreach (var path in paths)
                {
                    var state = byPath[path];
                    state.Revision = revision;
                    CaptureSnapshot(state, serializedObject.FindProperty(path));
                }
            }
            finally
            {
                ListPool<string>.Release(paths);
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Undo/Redo 후 모든 지연 명령의 이전 세대를 무효화한다.
        /// </summary>
        // ------------------------------------------------------------
        public static void InvalidateAll()
        {
            undoRevision = ++nextRevision;
        }

    #endregion

    }
}
