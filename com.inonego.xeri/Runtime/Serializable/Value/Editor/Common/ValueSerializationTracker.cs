/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : ValueSerializationTracker.cs
수정일 : 2026-10-30

# 설명
Unity 직렬화 편집·Undo를 위해 객체와 프로퍼티 경로별 이전 값을 보관한다.
Base, Modified, Range의 Runtime 변경은 각각 독립적으로 추적한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime;
using System.Runtime.CompilerServices;

using UnityEngine;

using inonego;
using inonego.Xeri;
using inonego.Xeri.Serializable;
using inonego.Xeri.Primitive;

namespace inonego.Xeri
{
    // ============================================================
    /// <summary>
    /// 직렬화 변경을 Runtime 변경과 혼동하지 않는 Editor 전용 추적기.
    /// </summary>
    // ============================================================
    internal static class ValueSerializationTracker
    {

    #region 내부 관찰 계약

        // ============================================================
        /// <summary>
        /// 인스턴스 교체 및 직렬화 복원을 처리하는 관찰자 계약.
        /// </summary>
        // ============================================================
        private interface IEntry
        {
            // ------------------------------------------------------------
            /// <summary>
            /// 현재 런타임 소유자에 내부 변경 관찰을 연결한다.
            /// </summary>
            // ------------------------------------------------------------
            void Bind(object value);

            // ------------------------------------------------------------
            /// <summary>
            /// 직렬화로 복원된 상태를 이전 관찰 상태와 비교한다.
            /// </summary>
            // ------------------------------------------------------------
            void FinalizeSerializedChange();
        }

        // ============================================================
        /// <summary>
        /// Base 변경만 추적한다.
        /// </summary>
        // ============================================================
        private sealed class BaseEntry<T> : IEntry
        {
            private Value<T> owner;
            private T previousBase;

            // ------------------------------------------------------------
            /// <summary>
            /// 최초 관찰 상태를 생성한다.
            /// </summary>
            // ------------------------------------------------------------
            internal BaseEntry(Value<T> value)
            {
                Bind(value);
            }

            // ------------------------------------------------------------
            /// <summary>
            /// 소유자가 교체되면 이전 구독을 해제하고 새 Base를 기준으로 삼는다.
            /// </summary>
            // ------------------------------------------------------------
            public void Bind(object value)
            {
                var current = (Value<T>)value;
                if (ReferenceEquals(owner, current))
                {
                    return;
                }

                if (owner != null)
                {
                    owner.OnBaseApplied -= OnBaseApplied;
                }

                owner = current;
                previousBase = current.Base;
                owner.OnBaseApplied += OnBaseApplied;
            }

            // ------------------------------------------------------------
            /// <summary>
            /// Runtime에서 바뀐 Base만 관찰 기준으로 반영한다.
            /// </summary>
            // ------------------------------------------------------------
            private void OnBaseApplied(T previous, T current, bool invokeEvent)
            {
                previousBase = current;
            }

            // ------------------------------------------------------------
            /// <summary>
            /// Setter를 통과하지 않은 Base 변경만 복구한다.
            /// </summary>
            // ------------------------------------------------------------
            public void FinalizeSerializedChange()
            {
                var before = previousBase;
                previousBase = owner.Base;
                owner.FinalizeSerializedChange(before);
                previousBase = owner.Base;
            }
        }

        // ============================================================
        /// <summary>
        /// Base와 Modified를 서로 독립적으로 추적한다.
        /// </summary>
        // ============================================================
        private sealed class ModifierEntry<T> : IEntry
        {
            private MValue<T> owner;
            private T previousBase;
            private T previousModified;

            // ------------------------------------------------------------
            /// <summary>
            /// 직렬화 변경 전 기준 상태를 초기화한다.
            /// </summary>
            // ------------------------------------------------------------
            internal ModifierEntry(MValue<T> value)
            {
                Bind(value);
            }

            // ------------------------------------------------------------
            /// <summary>
            /// 인스턴스 교체 시 Base·Modified 기준과 변경 구독을 재설정한다.
            /// </summary>
            // ------------------------------------------------------------
            public void Bind(object value)
            {
                var current = (MValue<T>)value;
                if (ReferenceEquals(owner, current)) return;

                if (owner != null)
                {
                    owner.OnBaseApplied -= OnBaseApplied;
                    owner.OnModifiedApplied -= OnModifiedApplied;
                }

                owner = current;
                previousBase = current.Base;
                previousModified = current.Modified;
                owner.OnBaseApplied += OnBaseApplied;
                owner.OnModifiedApplied += OnModifiedApplied;
            }

            // ------------------------------------------------------------
            /// <summary>
            /// Runtime Base 변경만 관찰 기준으로 반영한다.
            /// </summary>
            // ------------------------------------------------------------
            private void OnBaseApplied(T previous, T current, bool invokeEvent)
            {
                previousBase = current;
            }

            // ------------------------------------------------------------
            /// <summary>
            /// Runtime Modified 변경만 관찰 기준으로 반영한다.
            /// </summary>
            // ------------------------------------------------------------
            private void OnModifiedApplied(T previous, T current)
            {
                previousModified = current;
            }

            // ------------------------------------------------------------
            /// <summary>
            /// Base 및 Modified 직렬화 차이를 한 번만 복원한다.
            /// </summary>
            // ------------------------------------------------------------
            public void FinalizeSerializedChange()
            {
                var baseBefore = previousBase;
                var modifiedBefore = previousModified;
                previousBase = owner.Base;
                previousModified = owner.Modified;

                owner.FinalizeSerializedChange(baseBefore, modifiedBefore);
                previousBase = owner.Base;
                previousModified = owner.Modified;
            }
        }

        // ============================================================
        /// <summary>
        /// Base와 자식 Range 값을 독립적으로 추적한다.
        /// </summary>
        // ============================================================
        private sealed class RangeEntry<T> : IEntry
        where T : struct, IComparable<T>
        {
            private RangeValue<T> owner;
            private Value<Range<T>> child;
            private T previousBase;
            private Range<T> previousRange;

            // ------------------------------------------------------------
            /// <summary>
            /// 현재 범위와 Base를 이전 상태로 등록한다.
            /// </summary>
            // ------------------------------------------------------------
            internal RangeEntry(RangeValue<T> value)
            {
                Bind(value);
            }

            // ------------------------------------------------------------
            /// <summary>
            /// 부모와 Range 자식별 인스턴스 교체 시 해당 관찰 기준을 재설정한다.
            /// </summary>
            // ------------------------------------------------------------
            public void Bind(object value)
            {
                var current = (RangeValue<T>)value;
                if (!ReferenceEquals(owner, current))
                {
                    if (owner != null)
                    {
                        owner.OnBaseApplied -= OnBaseApplied;
                    }

                    owner = current;
                    previousBase = current.Base;
                    owner.OnBaseApplied += OnBaseApplied;
                }

                if (!ReferenceEquals(child, owner.Range))
                {
                    if (child != null)
                    {
                        child.OnBaseApplied -= OnRangeApplied;
                    }

                    child = owner.Range;
                    previousRange = child.Base;
                    child.OnBaseApplied += OnRangeApplied;
                }
            }

            // ------------------------------------------------------------
            /// <summary>
            /// Runtime Base 변경의 기준값만 갱신한다.
            /// </summary>
            // ------------------------------------------------------------
            private void OnBaseApplied(T previous, T current, bool invokeEvent)
            {
                previousBase = current;
            }

            // ------------------------------------------------------------
            /// <summary>
            /// Runtime Range 변경의 기준값만 갱신한다.
            /// </summary>
            // ------------------------------------------------------------
            private void OnRangeApplied(Range<T> previous, Range<T> current, bool invokeEvent)
            {
                previousRange = current;
            }

            // ------------------------------------------------------------
            /// <summary>
            /// Base와 Range의 직렬화 차이를 독립적으로 복구한다.
            /// </summary>
            // ------------------------------------------------------------
            public void FinalizeSerializedChange()
            {
                var baseBefore = previousBase;
                var rangeBefore = previousRange;
                previousBase = owner.Base;
                previousRange = owner.Range.Base;

                owner.FinalizeSerializedChange(baseBefore, rangeBefore);
                previousBase = owner.Base;
                previousRange = owner.Range.Base;
            }
        }

        private static readonly ConditionalWeakTable<UnityEngine.Object, Dictionary<string, IEntry>>
            entries = new();

    #endregion

    #region 관찰 및 변경 확정

        // ------------------------------------------------------------
        /// <summary>
        /// 기본 Value의 직렬화 기준을 관찰한다.
        /// </summary>
        // ------------------------------------------------------------
        internal static void Observe<T>(UnityEngine.Object target, string path, Value<T> value)
        {
            GetEntry(target, path, value, () => new BaseEntry<T>(value));
        }

        // ------------------------------------------------------------
        /// <summary>
        /// MValue의 직렬화 기준을 관찰한다.
        /// </summary>
        // ------------------------------------------------------------
        internal static void Observe<T>(UnityEngine.Object target, string path, MValue<T> value)
        {
            GetEntry(target, path, value, () => new ModifierEntry<T>(value));
        }

        // ------------------------------------------------------------
        /// <summary>
        /// RangeValue의 직렬화 기준을 관찰한다.
        /// </summary>
        // ------------------------------------------------------------
        internal static void Observe<T>
        (
            UnityEngine.Object target, string path, RangeValue<T> value
        )
        where T : struct, IComparable<T>
        {
            GetEntry(target, path, value, () => new RangeEntry<T>(value));
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 기본 Value의 직렬화 변경을 확정한다.
        /// </summary>
        // ------------------------------------------------------------
        internal static void FinalizeSerializedChange<T>(UnityEngine.Object target, string path, Value<T> value)
        {
            GetEntry(target, path, value, () => new BaseEntry<T>(value))?.FinalizeSerializedChange();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// MValue의 직렬화 변경을 확정한다.
        /// </summary>
        // ------------------------------------------------------------
        internal static void FinalizeSerializedChange<T>(UnityEngine.Object target, string path, MValue<T> value)
        {
            GetEntry(target, path, value, () => new ModifierEntry<T>(value))?.FinalizeSerializedChange();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// RangeValue의 직렬화 변경을 확정한다.
        /// </summary>
        // ------------------------------------------------------------
        internal static void FinalizeSerializedChange<T>
        (
            UnityEngine.Object target, string path, RangeValue<T> value
        )
        where T : struct, IComparable<T>
        {
            GetEntry(target, path, value, () => new RangeEntry<T>(value))?.FinalizeSerializedChange();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 한 소유자·경로의 관찰자를 공유하되 타입과 수명을 유지한다.
        /// </summary>
        // ------------------------------------------------------------
        private static IEntry GetEntry
        (
            UnityEngine.Object target, string path, object owner, Func<IEntry> factory
        )
        {
            if (target == null || owner == null || string.IsNullOrEmpty(path))
            {
                return null;
            }

            var properties = entries.GetOrCreateValue(target);
            if (!properties.TryGetValue(path, out var entry))
            {
                entry = factory();
                properties.Add(path, entry);
            }
            else
            {
                entry.Bind(owner);
            }

            return entry;
        }

    #endregion

    }
}
