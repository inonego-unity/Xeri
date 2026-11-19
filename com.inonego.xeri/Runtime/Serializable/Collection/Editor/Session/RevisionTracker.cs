/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : RevisionTracker.cs
수정일 : 2026-11-19

# 설명
Collection Editor에서 Unity 객체와 프로퍼티 경로별 변경 세대와 마지막 직렬화 상태를 공유한다.
여러 Inspector의 중복 변경 관찰을 하나로 합치고 관련된 상위·하위 경로를 함께 무효화한다.
Undo/Redo는 보수적으로 이전 변경 세대를 무효화한다.

# 특이사항, 제약사항
변경 비교는 SerializeReference가 없으면 SerializedProperty.contentHash만 쓰고, 있으면 값 스냅샷을 쓴다.
SerializeReference 내용은 별도 참조 저장소에 있어 contentHash에 반영되지 않기 때문이다.
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
            // ------------------------------------------------------------
            /// <summary>
            /// 이 경로의 마지막 변경 세대다.
            /// </summary>
            // ------------------------------------------------------------
            public ulong Revision;

            // ------------------------------------------------------------
            /// <summary>
            /// 비교 기준을 한 번이라도 잡았는지 나타낸다.
            /// </summary>
            // ------------------------------------------------------------
            public bool HasBaseline;

            // ----------------------------------------------------------------------
            /// <summary>
            /// SerializeReference가 있어 엔진 해시 대신 값 스냅샷으로 비교하는지 나타낸다.
            /// </summary>
            // ----------------------------------------------------------------------
            public bool UsesSnapshot;

            // ------------------------------------------------------------
            /// <summary>
            /// 해시 비교에 쓰는 마지막 기준 해시다.
            /// </summary>
            // ------------------------------------------------------------
            public uint ContentHash;

            // ------------------------------------------------------------
            /// <summary>
            /// 스냅샷 비교에 쓰는 마지막 기준 값이며 해시 비교면 null이다.
            /// </summary>
            // ------------------------------------------------------------
            public SerializedPropertySnapshot Snapshot;
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

            if (Matches(state, property))
            {
                return false;
            }

            Advance(property.serializedObject, path);
            return true;
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

    #region 경로별 기준 상태

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

            // SerializeReference 내용은 별도 참조 저장소에 있어 contentHash에 반영되지 않는다.
            state.UsesSnapshot = property != null && ManagedReferenceTypeCatalog.ContainsManagedReference(property);
            state.Snapshot = null;
            state.ContentHash = property?.contentHash ?? 0;
            if (state.UsesSnapshot)
            {
                SerializedPropertySnapshot.TryCreate(property, out state.Snapshot, out _);
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 마지막 기준과 현재 직렬화 상태가 같은지 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        private static bool Matches(RevisionState state, SerializedProperty property)
        {
            if (property == null)
            {
                return false;
            }

            return state.UsesSnapshot
                ? state.Snapshot?.Matches(property) == true
                : property.contentHash == state.ContentHash;
        }

    #endregion

    }
}
