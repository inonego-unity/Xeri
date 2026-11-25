/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : SerializedChangeTracker.cs
수정일 : 2026-11-25

# 설명
Collection Editor에서 Unity 객체와 프로퍼티 경로별 직렬화 변경을 관찰해 공유 변경 세대를 붙인다.
여러 Inspector의 중복 변경 관찰을 하나로 합치고 관련된 상·하위 경로를 함께 무효화한다.
Undo/Redo는 보수적으로 이전 변경 세대를 무효화한다.

# 특이사항, 제약사항
이 세대는 상태 소유자가 올리는 번호가 아니라 Unity가 가진 직렬화 데이터에 대한 관찰 번호다.
관찰(Observe), 쓴 쪽의 보고(ReportWrite), Undo 무효화는 모두 같은 사건의 관찰이라 하나의 전역 수열을 공유한다.
경로마다 번호와 비교 기준(SerializedPropertyBaseline)을 한 항목이 가지며, 기준 갱신과 번호 증가는 항상 함께 일어난다.
변경 비교 방법(해시 또는 값 스냅샷)은 SerializedPropertyBaseline이 맡는다.
프로퍼티를 찾지 못한 경로는 "없음" 상태로 비교한다. 있다가 없어지거나 없다가 생기면 한 번 바뀐 것으로 본다.
기준이 아직 없으면 기준만 잡고 번호는 올리지 않는다. 대상이 없거나 경로가 null이면 관찰하지 않는다.
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
using inonego.Xeri.Editor;

namespace inonego.Xeri.Serializable.Editor
{
    // ============================================================
    /// <summary>
    /// 객체·컬렉션 경로별 직렬화 변경을 관찰해 변경 세대를 공유한다.
    /// </summary>
    // ============================================================
    internal static class SerializedChangeTracker
    {

    #region 내부 데이터

        // ============================================================
        /// <summary>
        /// 하나의 경로에서 마지막 변경 세대와 마지막으로 확인한 값.
        /// </summary>
        // ============================================================
        private sealed class TrackedPath : IRevisioned
        {
            // ----------------------------------------------------------------------
            /// <summary>
            /// 이 경로를 마지막으로 보고하거나 발견한 변경 세대다. Undo 바닥값은 포함하지 않는다.
            /// </summary>
            // ----------------------------------------------------------------------
            public Revision ChangedRevision;

            // ----------------------------------------------------------------------
            /// <summary>
            /// <br/> 이 경로가 마지막으로 바뀌었을 수 있는 변경 세대를 반환한다.
            /// <br/> Undo/Redo 이후에는 모든 경로가 그 시점 이후로 바뀌었을 수 있다고 본다.
            /// </summary>
            // ----------------------------------------------------------------------
            Revision IRevisioned.Revision => ChangedRevision > undoRevision ? ChangedRevision : undoRevision;

            // ------------------------------------------------------------
            /// <summary>
            /// 이 경로의 마지막 직렬화 상태와 그 비교다.
            /// </summary>
            // ------------------------------------------------------------
            public readonly SerializedPropertyBaseline Baseline = new();
        }

        // ============================================================
        /// <summary>
        /// 한 Unity 객체에 속한 프로퍼티별 추적 항목.
        /// </summary>
        // ============================================================
        private sealed class TrackedPathMap : Dictionary<string, TrackedPath>
        {
            // ------------------------------------------------------------
            /// <summary>
            /// ConditionalWeakTable에서 호출할 기본 생성자.
            /// </summary>
            // ------------------------------------------------------------
            public TrackedPathMap()
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
        private static readonly ConditionalWeakTable<UnityEngine.Object, TrackedPathMap> tracked = new();
        private static Revision nextRevision;
        private static Revision undoRevision;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// Undo/Redo 후 기존 편집 명령을 보수적으로 무효화한다.
        /// </summary>
        // ------------------------------------------------------------
        static SerializedChangeTracker()
        {
            Undo.undoRedoPerformed += InvalidateAll;
        }

    #endregion

    #region 조회와 관찰

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 대상 경로가 마지막으로 바뀌었을 수 있는 변경 세대를 조회한다.
        /// <br/> Undo/Redo 이후에는 모든 경로가 그 시점 이후로 바뀌었을 수 있다고 본다.
        /// </summary>
        // ----------------------------------------------------------------------
        public static Revision Get(UnityEngine.Object target, string path)
        {
            return Track(target, path).Revision;
        }

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> 대상 경로의 변경 세대 소유자를 반환한다. 같은 대상과 경로는 항상 같은 소유자다.
        /// <br/> RevisionMark가 이 소유자를 캡처하면 같은 경로를 다시 연 Source와도 대응한다.
        /// </summary>
        // --------------------------------------------------------------------------------
        public static IRevisioned Track(UnityEngine.Object target, string path)
        {
            return GetOrCreate(tracked.GetOrCreateValue(target), path);
        }

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> 다른 Inspector에서도 동일한 직렬화 변경을 한 번만 기록한다.
        /// <br/> 프로퍼티를 찾지 못하면 "없음" 상태로 비교하며, 대상이 없으면 false를 반환한다.
        /// </summary>
        // --------------------------------------------------------------------------------
        public static bool Observe(SerializedObject serializedObject, string path)
        {
            var target = serializedObject?.targetObject;
            if (target == null || path == null)
            {
                return false;
            }

            var property = serializedObject.FindProperty(path);
            var state = GetOrCreate(tracked.GetOrCreateValue(target), path);
            if (!state.Baseline.IsCaptured)
            {
                // 세대 조회만으로 등록된 경로는 첫 관찰을 기준 상태로 삼는다.
                state.Baseline.Capture(property);
                return false;
            }

            if (state.Baseline.Matches(property))
            {
                return false;
            }

            ReportWrite(serializedObject, path);
            return true;
        }

    #endregion

    #region 변경 세대

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 쓴 쪽이 변경을 보고한다. 관련 경로의 변경 세대와 관찰 기준을 함께 갱신해
        /// <br/> 같은 변경이 다음 Observe에서 다시 발견되지 않게 한다.
        /// </summary>
        // ----------------------------------------------------------------------
        public static void ReportWrite(SerializedObject serializedObject, string changedPath)
        {
            var byPath = tracked.GetOrCreateValue(serializedObject.targetObject);
            GetOrCreate(byPath, changedPath);
            nextRevision = nextRevision.Inc();
            var revision = nextRevision;

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
                    state.ChangedRevision = revision;
                    state.Baseline.Capture(serializedObject.FindProperty(path));
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
            nextRevision = nextRevision.Inc();
            undoRevision = nextRevision;
        }

    #endregion

    #region 경로별 추적 항목

        // ------------------------------------------------------------
        /// <summary>
        /// 등록되지 않은 경로를 현재 Undo 세대에서 시작한다.
        /// </summary>
        // ------------------------------------------------------------
        private static TrackedPath GetOrCreate(TrackedPathMap byPath, string path)
        {
            if (!byPath.TryGetValue(path, out var state))
            {
                state = new TrackedPath
                {
                    ChangedRevision = undoRevision,
                };
                byPath.Add(path, state);
            }

            return state;
        }

    #endregion

    }
}
