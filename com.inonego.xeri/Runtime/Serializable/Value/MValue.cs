/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : MValue.cs
수정일 : 2026-11-24

# 설명
Order 순서로 적용되는 IModifier<T> 목록을 가지는 Modifiable Value.
Modifier의 내부 상태 변경을 구독해 Modified 캐시를 자동 갱신한다.

# 특이사항, 제약사항
Modified 캐시는 runtime derived state이며 Base와 Modifier 변경 시 내부에서 즉시 갱신한다(BeginBatch 범위 안은 범위가 닫힐 때까지 미룬다).
Set, AddModifier, RemoveModifier, ClearModifiers는 delegate나 closure를 할당하지 않는다.
동일 Modifier 인스턴스가 여러 Key로 등록되어도 변경 이벤트는 한 번만 구독한다.
invokeEvent는 외부 이벤트만 제어하며 derived type의 Modified 갱신 hook은 항상 실행한다.
CloneFrom은 Base와 Modifier 목록(키·Order·상태)을 외부 이벤트 없이 복사하고 Modified를 한 번 다시 계산한다.
목록 복제는 XOrdered.CloneFrom이 맡고, MValue는 Base 복사 시점과 구독·Modified 재계산을 맡는다.
키·Order 구성과 구체 타입이 순서대로 같으면 목록을 그대로 두고 각 인스턴스에 CloneFrom한다(할당 없음).
구성이 다르면 목록을 다시 만들며, 같은 키·같은 타입 인스턴스는 재사용하고 나머지는 Clone한다(목록 항목 할당 발생).
source가 가진 인스턴스는 재사용하지 않으므로 복제본은 source와 Modifier 인스턴스를 공유하지 않는다.
여러 키·여러 MValue 사이의 인스턴스 공유 관계는 복제되지 않는다.
새 Modifier 복제(Clone)가 실패하면 이 MValue를 바꾸지 않는다. 기존 인스턴스의 CloneFrom이 실패해도 나머지를 끝까지 복사하고
Modifier 목록을 source에 맞춘 뒤(실패한 인스턴스는 이전 상태로 남음) 구독과 Modified 캐시를 그 상태에 맞추고 예외를 전달한다.
BeginBatch 범위 안의 변경은 재계산과 외부 이벤트를 가장 바깥 범위가 닫힐 때 한 번으로 합친다.
범위 안에서 Modified는 범위를 연 시점의 값이며, 범위는 using으로 연 순서의 역순으로 닫는다.
범위(BatchScope)는 ref struct이며 열린 순서대로 발급 번호를 가진다.
이미 닫힌 범위를 다시 닫으면 같은 깊이에 새 범위가 열려 있어도 번호로 구별해 상태를 바꾸지 않고 예외만 던진다.
바깥 범위를 먼저 닫으면 안쪽 범위까지 닫아 처리한 뒤 예외를 던진다.
범위가 닫힐 때의 이벤트는 범위를 연 시점의 값과 비교하며, 이벤트를 요청한 변경 종류(Base·Modified)만 통지한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using inonego;
using inonego.Xeri;

namespace inonego.Xeri.Serializable
{
    // ============================================================
    /// <summary>
    /// 수정자가 적용되는 Value.
    /// </summary>
    // ============================================================
    [Serializable]
    public class MValue<T> :
        Value<T>,
        IMValue<T>,
        ISerializationCallbackReceiver
    {

    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// Order 오름차순의 수정자 등록 목록을 읽기 전용으로 노출한다.
        /// </summary>
        // ------------------------------------------------------------
        public IReadOnlyXOrdered<int, string, IModifier<T>> Modifiers => modifiers;
        [SerializeField]
        private XOrdered<int, string, IModifier<T>> modifiers = new();

        // ------------------------------------------------------------
        /// <summary>
        /// 수정자가 적용된 현재 값.
        /// </summary>
        // ------------------------------------------------------------
        public T Modified => cached;
        [NonSerialized]
        private T cached;

    #endregion

    #region 이벤트

        // ------------------------------------------------------------
        /// <summary>
        /// Modified 가 변경될 때 발생하는 이벤트.
        /// </summary>
        // ------------------------------------------------------------
        [field: NonSerialized]
        public event ValueChangeEventHandler<T> OnModifiedChange = null;

        // ------------------------------------------------------------
        /// <summary>
        /// 외부 이벤트와 관계없이 Modified 캐시의 실제 변경을 전달한다.
        /// </summary>
        // ------------------------------------------------------------
        [field: NonSerialized]
        internal event Action<T, T> OnModifiedApplied;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// MValue 인스턴스를 초기화한다.
        /// </summary>
        // ------------------------------------------------------------
        public MValue() : this(default)
        {
            // NONE
        }

        // ------------------------------------------------------------
        /// <summary>
        /// MValue 인스턴스를 초기화한다.
        /// </summary>
        // ------------------------------------------------------------
        public MValue(T value) : base(value)
        {
            UpdateModified(cached, invokeHook: false);
        }

    #endregion

    #region 복제

        // ------------------------------------------------------------
        /// <summary>
        /// 복제에 사용할 빈 MValue 를 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public override Value<T> @new() => new MValue<T>();

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> Base와 Modifier 목록을 복사하고 Modified를 한 번 다시 계산한다.
        /// <br/> 외부 이벤트는 발행하지 않고 내부 hook은 실행한다.
        /// <br/> 새 Modifier 복제가 실패하면 이 MValue를 바꾸지 않고 예외를 전달한다.
        /// </summary>
        // ----------------------------------------------------------------------
        protected override void CloneState(Value<T> source)
        {
            var typed = (MValue<T>)source;
            var previousModified = Modified;

            try
            {
                // 목록 복제는 XOrdered가 맡는다. 새 Modifier 준비가 끝난 뒤에 Base 복사와 구독 해제가 일어난다.
                modifiers.CloneFrom(typed.modifiers, new CloneHook(this, typed));
            }
            finally
            {
                // 복사 중 Modifier가 예외를 던져도 구독과 Modified 캐시를 현재 상태에 맞춘다.
                ResubscribeModifiers();
                FinishClone(previousModified);
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 복제 뒤 Modified를 다시 계산하거나 묶음 범위 종료로 미룬다.
        /// </summary>
        // ------------------------------------------------------------
        private void FinishClone(T previousModified)
        {
            // 복제는 외부 이벤트를 요청하지 않는다.
            if (batchDepth > 0)
            {
                MarkBatchChanged(raiseBase: false, raiseModified: false);
                return;
            }

            UpdateModified(previousModified);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Base를 복사하고 Modifier 구독을 끊는다.
        /// </summary>
        // ------------------------------------------------------------
        private void CopyBaseAndDetach(MValue<T> typed)
        {
            base.CloneState(typed);

            // Modifier의 CloneFrom 알림으로 재계산되지 않도록 구독을 끊는다. 재구독은 CloneState가 맡는다.
            UnsubscribeModifiers();
        }

        // ============================================================
        /// <summary>
        /// 목록 준비가 끝난 시점에 Base 복사와 구독 해제를 실행하는 훅.
        /// </summary>
        // ============================================================
        private readonly struct CloneHook : ICloneReadyHook
        {
            private readonly MValue<T> owner;
            private readonly MValue<T> source;

            // ------------------------------------------------------------
            /// <summary>
            /// 복제 대상과 source를 보관한다.
            /// </summary>
            // ------------------------------------------------------------
            public CloneHook(MValue<T> owner, MValue<T> source)
            {
                this.owner = owner;
                this.source = source;
            }

            // ------------------------------------------------------------
            /// <summary>
            /// 대상의 Base를 복사하고 Modifier 구독을 끊는다.
            /// </summary>
            // ------------------------------------------------------------
            public void OnCloneReady() => owner.CopyBaseAndDetach(source);
        }

    #endregion

    #region 값 설정

        // ------------------------------------------------------------
        /// <summary>
        /// Base 값을 설정한 뒤 Modified 캐시를 갱신한다.
        /// </summary>
        // ------------------------------------------------------------
        public override void Set(T value, bool invokeEvent = true)
        {
            var previous = BeginChange();

            base.Set(value, invokeEvent: false);

            EndChange(previous, invokeEvent);
        }

    #endregion

    #region 수정자 관리

        // ------------------------------------------------------------
        /// <summary>
        /// 키를 명시하여 수정자를 추가한다.
        /// </summary>
        // ------------------------------------------------------------
        public void AddModifier(string key, IModifier<T> modifier, int order = 0, bool invokeEvent = true)
        {
            if (modifier == null)
            {
                throw new ArgumentNullException(nameof(modifier), "추가하려는 수정자가 null입니다.");
            }

            var previous = BeginChange();

            modifiers.Add(order, key, modifier);
            SubscribeModifier(modifier);

            EndChange(previous, invokeEvent);
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <c>IKeyable&lt;string&gt;</c> 을 구현한 수정자를 자기 키로 추가한다.
        /// </summary>
        // ----------------------------------------------------------------------
        public void AddModifier<TModifier>(TModifier modifier, int order = 0, bool invokeEvent = true)
        where TModifier : IModifier<T>, IKeyable<string>
        {
            if (modifier == null)
            {
                throw new ArgumentNullException(nameof(modifier), "추가하려는 수정자가 null입니다.");
            }

            AddModifier(modifier.Key, modifier, order, invokeEvent);
        }

        // --------------------------------------------------------------------------------
        /// <summary>
        /// 수정자가 <c>IKeyable&lt;string&gt;</c> 을 구현하면 자기 키로 추가한다. 아니면 예외.
        /// </summary>
        // --------------------------------------------------------------------------------
        public void AddModifier(IModifier<T> modifier, int order = 0, bool invokeEvent = true)
        {
            if (modifier == null)
            {
                throw new ArgumentNullException(nameof(modifier), "추가하려는 수정자가 null입니다.");
            }

            if (modifier is IKeyable<string> keyable)
            {
                AddModifier(keyable.Key, modifier, order, invokeEvent);
            }
            else
            {
                throw new ArgumentException
                (
                    $"수정자({modifier.GetType().Name})가 IKeyable<string>을 구현하지 않아 키를 추출할 수 없습니다."
                );
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 키로 수정자를 제거한다.
        /// </summary>
        // ------------------------------------------------------------
        public bool RemoveModifier(string key, bool invokeEvent = true)
        {
            var previous = BeginChange();
            var removed = modifiers.Remove(key);

            if (removed)
            {
                ResubscribeModifiers();
            }

            EndChange(previous, invokeEvent);

            return removed;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 모든 수정자를 제거한다.
        /// </summary>
        // ------------------------------------------------------------
        public void ClearModifiers(bool invokeEvent = true)
        {
            if (modifiers.Count == 0) return;

            var previous = BeginChange();

            UnsubscribeModifiers();
            modifiers.Clear();

            EndChange(previous, invokeEvent);
        }

    #endregion

    #region 묶음 갱신

        // 묶음 범위 안에서 재계산이 미뤄진 변경이 있는지 표시한다.
        private DirtyFlag BatchDirty
        {
            get
            {
                batchDirty ??= new();
                return batchDirty;
            }
        }
        [NonSerialized]
        private DirtyFlag batchDirty;

        [NonSerialized]
        private int batchDepth;

        // 깊이별로 열려 있는 범위의 발급 번호. 같은 깊이에 다시 연 범위와 닫힌 범위를 구별한다.
        private List<Revision> OpenBatchGenerations
        {
            get
            {
                openBatchGenerations ??= new();
                return openBatchGenerations;
            }
        }
        [NonSerialized]
        private List<Revision> openBatchGenerations;

        [NonSerialized]
        private Revision nextBatchGeneration = Revision.None;

        [NonSerialized]
        private bool batchRaiseBase;

        [NonSerialized]
        private bool batchRaiseModified;

        [NonSerialized]
        private T batchBase;

        [NonSerialized]
        private T batchModified;

        // ============================================================
        /// <summary>
        /// BeginBatch 가 연 묶음 범위. using 으로 닫는다.
        /// </summary>
        // ============================================================
        public ref struct BatchScope
        {
            private readonly MValue<T> owner;
            private readonly int depth;
            private readonly Revision generation;

            internal BatchScope(MValue<T> owner, int depth, Revision generation)
            {
                this.owner = owner;
                this.depth = depth;
                this.generation = generation;
            }

            // ------------------------------------------------------------
            /// <summary>
            /// 범위를 닫는다. 가장 바깥 범위면 한 번 재계산한다.
            /// </summary>
            // ------------------------------------------------------------
            public void Dispose()
            {
                owner?.EndBatch(depth, generation);
            }
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 여러 변경의 재계산과 외부 이벤트를 하나로 합치는 범위를 연다.
        /// <br/> 범위는 중첩할 수 있고 가장 바깥 범위가 닫힐 때 한 번만 계산한다.
        /// </summary>
        // ----------------------------------------------------------------------
        public BatchScope BeginBatch()
        {
            if (batchDepth == 0)
            {
                batchBase = Base;
                batchModified = Modified;
                batchRaiseBase = false;
                batchRaiseModified = false;
                BatchDirty.Clear();
            }

            nextBatchGeneration = nextBatchGeneration.Inc();
            var generation = nextBatchGeneration;
            var generations = OpenBatchGenerations;

            batchDepth++;

            // 범위가 닫힐 때 줄어든 자리를 재사용해 깊이 최댓값 이상으로 할당하지 않는다.
            if (generations.Count < batchDepth)
            {
                generations.Add(generation);
            }
            else
            {
                generations[batchDepth - 1] = generation;
            }

            return new BatchScope(this, batchDepth, generation);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 범위를 닫고, 가장 바깥 범위면 미룬 재계산과 이벤트를 처리한다.
        /// <br/> 이미 닫힌 범위면 상태를 바꾸지 않고, 순서를 어기면
        /// <br/> 안쪽 범위까지 닫은 뒤 예외를 던진다.
        /// </summary>
        // ------------------------------------------------------------
        private void EndBatch(int depth, Revision generation)
        {
            // 이미 닫힌 범위를 다시 닫으면 상태를 바꾸지 않고 오용을 알린다. 같은 깊이에 새 범위가 열려 있어도 번호로 구별한다.
            if (depth > batchDepth || OpenBatchGenerations[depth - 1] != generation)
            {
                throw new InvalidOperationException("이미 닫힌 묶음 범위를 다시 닫으려고 했습니다.");
            }

            // 바깥 범위를 먼저 닫으면 안쪽 범위까지 함께 닫아 묶음 상태에 갇히지 않게 한다.
            var outOfOrder = depth < batchDepth;

            batchDepth = depth - 1;

            if (batchDepth == 0)
            {
                FlushBatch();
            }

            if (outOfOrder)
            {
                throw new InvalidOperationException("묶음 범위는 연 순서의 역순으로 닫아야 합니다. 안쪽 범위까지 함께 닫았습니다.");
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 미룬 재계산을 한 번 하고, 요청된 종류의 변경만
        /// <br/> 범위 시작 값과 비교해 통지한다.
        /// <br/> 이벤트 핸들러가 새 범위를 열어도 이번 범위 값이 덮이지 않는다.
        /// </summary>
        // ------------------------------------------------------------
        private void FlushBatch()
        {
            if (!BatchDirty.IsDirty)
            {
                return;
            }

            // 이벤트 핸들러가 BeginBatch로 필드를 초기화해도 영향이 없도록 지역 값으로 옮긴다.
            var previousBase = batchBase;
            var previousModified = batchModified;
            var raiseBase = batchRaiseBase;
            var raiseModified = batchRaiseModified;

            BatchDirty.Clear();
            UpdateModified(previousModified);

            var currentBase = Base;
            var currentModified = Modified;

            // 범위 밖과 같게, 이벤트를 요청한 변경 종류만 통지한다.
            if (raiseBase && !comparer.Equals(previousBase, currentBase))
            {
                RaiseBaseChange(previousBase, currentBase);
            }

            if (raiseModified && !comparer.Equals(previousModified, currentModified))
            {
                RaiseModifiedChange(previousModified, currentModified);
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 범위 안의 변경과 요청된 이벤트 종류를 기록한다.
        /// </summary>
        // ------------------------------------------------------------
        private void MarkBatchChanged(bool raiseBase, bool raiseModified)
        {
            BatchDirty.SetDirty();
            batchRaiseBase |= raiseBase;
            batchRaiseModified |= raiseModified;
        }

    #endregion

    #region 갱신과 통지

        // ------------------------------------------------------------
        /// <summary>
        /// 변경 전 Base와 Modified를 기록한다.
        /// </summary>
        // ------------------------------------------------------------
        private (T Base, T Modified) BeginChange() => (Base, Modified);

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 변경 뒤 Modified를 갱신하고 변경 전 값과 비교해 외부 이벤트를 전달한다.
        /// <br/> 묶음 범위 안이면 재계산과 이벤트를 범위 종료로 미룬다.
        /// </summary>
        // ----------------------------------------------------------------------
        private void EndChange((T Base, T Modified) previous, bool invokeEvent)
        {
            if (batchDepth > 0)
            {
                MarkBatchChanged(raiseBase: invokeEvent, raiseModified: invokeEvent);
                return;
            }

            UpdateModified(previous.Modified);

            if (invokeEvent)
            {
                RaiseChanges(previous.Base, previous.Modified);
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 이전 Modified와 재계산 결과를 비교해 캐시와 내부 hook을 갱신한다.
        /// </summary>
        // ------------------------------------------------------------
        private void UpdateModified(T previous, bool invokeHook = true)
        {
            var current = Modify(Base);
            cached = current;

            if (comparer.Equals(previous, current))
            {
                return;
            }

            OnModifiedApplied?.Invoke(previous, current);

            if (invokeHook)
            {
                OnModifiedUpdated(in previous, in current);
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// modifiers 를 Order 순서대로 순차 적용한 값을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        private T Modify(T value)
        {
            foreach (var entry in modifiers)
            {
                if (entry.Value == null)
                {
                    continue;
                }

                value = entry.Value.Modify(value);
            }

            return value;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 확정된 Base와 Modified를 이전 값과 비교해 통지한다.
        /// </summary>
        // ------------------------------------------------------------
        private void RaiseChanges(T previousBase, T previousModified)
        {
            var currentBase = Base;
            var currentModified = Modified;

            if (!comparer.Equals(previousBase, currentBase))
            {
                RaiseBaseChange(previousBase, currentBase);
            }

            if (!comparer.Equals(previousModified, currentModified))
            {
                RaiseModifiedChange(previousModified, currentModified);
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 확정된 Modified 변경을 외부에 전달한다.
        /// </summary>
        // ------------------------------------------------------------
        private void RaiseModifiedChange(T previous, T current)
        {
            OnModifiedChange?.Invoke(this, new(previous, current));
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Modified가 갱신된 뒤 외부 이벤트와 무관하게 호출되는 내부 hook.
        /// </summary>
        // ------------------------------------------------------------
        protected virtual void OnModifiedUpdated(in T prev, in T next)
        {
            // NONE
        }

    #endregion

    #region Modifier 구독

        private HashSet<IModifier<T>> SubscribedModifiers
        {
            get
            {
                subscribedModifiers ??= new
                (
                    ReferenceEqualityComparer<IModifier<T>>.Instance
                );

                return subscribedModifiers;
            }
        }
        [NonSerialized]
        private HashSet<IModifier<T>> subscribedModifiers;

        // 구독·해제마다 메서드 그룹 delegate를 새로 만들지 않도록 한 번만 만든다.
        private Action ModifierChangeHandler
        {
            get
            {
                modifierChangeHandler ??= HandleModifierChange;
                return modifierChangeHandler;
            }
        }
        [NonSerialized]
        private Action modifierChangeHandler;

        // ----------------------------------------------------------------------
        /// <summary>
        /// 이 MValue의 변경 handler가 Modifier에 한 번만 등록되도록 구독한다.
        /// </summary>
        // ----------------------------------------------------------------------
        private void SubscribeModifier(IModifier<T> modifier)
        {
            if (modifier == null || !SubscribedModifiers.Add(modifier))
            {
                return;
            }

            modifier.OnChange -= ModifierChangeHandler;
            modifier.OnChange += ModifierChangeHandler;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 등록된 Modifier에서 이 MValue의 변경 구독을 모두 해제한다.
        /// </summary>
        // ------------------------------------------------------------
        private void UnsubscribeModifiers()
        {
            foreach (var modifier in SubscribedModifiers)
            {
                modifier.OnChange -= ModifierChangeHandler;
            }

            SubscribedModifiers.Clear();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 Modifier 목록을 기준으로 변경 이벤트를 다시 구독한다.
        /// </summary>
        // ------------------------------------------------------------
        private void ResubscribeModifiers()
        {
            UnsubscribeModifiers();

            foreach (var entry in modifiers)
            {
                SubscribeModifier(entry.Value);
            }
        }

    #endregion

    #region 직렬화 변경 확정

        // ------------------------------------------------------------
        /// <summary>
        /// 직렬화로 적용된 Base·Modified 변경과 Modifier 구독을 확정한다.
        /// </summary>
        // ------------------------------------------------------------
        internal void FinalizeSerializedChange(T previousBase, T previousModified)
        {
            ResubscribeModifiers();

            // 현재 캐시를 되돌리지 않고 직렬화 변경 전 값으로 차이를 판단한다.
            UpdateModified(previousModified);

            RaiseChanges(previousBase, previousModified);
        }

    #endregion

    #region 이벤트 핸들러

        // ------------------------------------------------------------
        /// <summary>
        /// Modifier 상태 변경 시 Modified 캐시를 갱신한다.
        /// </summary>
        // ------------------------------------------------------------
        private void HandleModifierChange()
        {
            // 묶음 범위 안이면 재계산을 범위 종료로 미룬다. Modifier 변경은 Modified 이벤트만 요청한다.
            if (batchDepth > 0)
            {
                MarkBatchChanged(raiseBase: false, raiseModified: true);
                return;
            }

            var previousModified = Modified;
            UpdateModified(previousModified);

            if (!comparer.Equals(previousModified, Modified))
            {
                RaiseModifiedChange(previousModified, Modified);
            }
        }

    #endregion

    #region ISerializationCallbackReceiver

        // ------------------------------------------------------------
        /// <summary>
        /// 직렬화 전에 추가 동기화는 수행하지 않는다.
        /// </summary>
        // ------------------------------------------------------------
        public void OnBeforeSerialize()
        {
            // NONE
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 역직렬화된 authoritative state에서 runtime state를 다시 구성한다.
        /// </summary>
        // ----------------------------------------------------------------------
        public void OnAfterDeserialize()
        {
            ResubscribeModifiers();
            cached = Modify(Base);
        }

    #endregion

    #region 암시적 변환

        // ------------------------------------------------------------
        /// <summary>
        /// MValue&lt;T&gt;에서 T로의 암시적 변환(Modified 값).
        /// </summary>
        // ------------------------------------------------------------
        public static implicit operator T(MValue<T> wrapper)
        {
            return wrapper != null ? wrapper.Modified : default;
        }

    #endregion

    #region Object 오버라이드

        // ------------------------------------------------------------
        /// <summary>
        /// 같은 구체 타입의 Modified 또는 T 값과 비교한다.
        /// </summary>
        // ------------------------------------------------------------
        public override bool Equals(object obj)
        {
            if (obj is MValue<T> other && other.GetType() == GetType())
                return comparer.Equals(Modified, other.Modified);
            if (obj is T directValue)
                return comparer.Equals(Modified, directValue);
            return false;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Modified의 해시 코드를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public override int GetHashCode() => comparer.GetHashCode(Modified);

        // ------------------------------------------------------------
        /// <summary>
        /// Modified와 Base를 문자열로 표현한다.
        /// </summary>
        // ------------------------------------------------------------
        public override string ToString() => $"{Modified}({Base})";

    #endregion

    }
}
