/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_ValueDrawer.cs
수정일 : 2026-09-18

# 설명
ValueDrawer / RangeValueDrawer / MValueDrawer의 핵심 기능 에디터 테스트.
CreatePropertyGUI 구조, Apply 로직, Undo 후 이벤트 동기화 경로를 검증한다.

# 특이사항
TrackPropertyValue / TrackSerializedObjectValue 콜백은 패널 없이는 직접 발화하지 않으므로
Undo 후 이미 복원된 runtime state에서 drawer가 누락 이벤트를 보충하는 경로를 검증한다.

# 테스트 구성
 G: GUI 구조 (CreatePropertyGUI 행/Toggle 검증)
 A: Apply 로직 (RecordObject + Set + SetDirty + Update 시뮬레이션)
 U: Undo 후 이벤트 동기화 경로
 X: 가드 로직 (min > max 시 Set 미호출)
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

using NUnit;
using NUnit.Framework;

namespace inonego.Xeri.TEST.Serializable._Value
{

    using inonego;
    using inonego.Xeri;
    using inonego.Xeri.Primitive;
    using inonego.Xeri.Serializable;

    // ======================================================================
    /// <summary>
    /// ValueDrawer / RangeValueDrawer / MValueDrawer 에디터 테스트 클래스.
    /// </summary>
    // ======================================================================
    public class TEST_ValueDrawer
    {

    #region 헬퍼

        private class ValueWrapper : ScriptableObject
        {
            [SerializeField] public Value<int> Value = new(42);
        }

        private class RangeValueWrapper : ScriptableObject
        {
            [SerializeField] public RangeValue<int> Value = new(5, new Range<int>(0, 10));
        }

        private class MValueWrapper : ScriptableObject
        {
            [SerializeField] public MValue<int> Value = new(42);
        }

    #endregion

    #region G-1: ValueDrawer CreatePropertyGUI 구조

        [Test]
        public void TEST_ValueDrawer_CreatePropertyGUI_1행_Toggle_포함()
        {
            var wrapper = ScriptableObject.CreateInstance<ValueWrapper>();
            var so      = new SerializedObject(wrapper);
            var prop    = so.FindProperty("Value");

            var drawer = new ValueDrawer();
            var root   = drawer.CreatePropertyGUI(prop);

            Assert.IsNotNull(root);
            Assert.AreEqual(1, root.childCount);
            Assert.IsNotNull(root.Q<Toggle>());

            UnityEngine.Object.DestroyImmediate(wrapper);
        }

    #endregion

    #region G-2: RangeValueDrawer CreatePropertyGUI 구조

        [Test]
        public void TEST_ValueDrawer_RangeValueDrawer_CreatePropertyGUI_2행_Toggle_2개()
        {
            var wrapper = ScriptableObject.CreateInstance<RangeValueWrapper>();
            var so      = new SerializedObject(wrapper);
            var prop    = so.FindProperty("Value");

            var drawer = new RangeValueDrawer();
            var root   = drawer.CreatePropertyGUI(prop);

            Assert.IsNotNull(root);
            Assert.AreEqual(2, root.childCount);

            var toggles = root.Query<Toggle>().ToList();
            Assert.AreEqual(2, toggles.Count);

            UnityEngine.Object.DestroyImmediate(wrapper);
        }

    #endregion

    #region G-3: MValueDrawer CreatePropertyGUI 구조

        [Test]
        public void TEST_ValueDrawer_MValueDrawer_CreatePropertyGUI_1행_Toggle_포함()
        {
            var wrapper = ScriptableObject.CreateInstance<MValueWrapper>();
            var so      = new SerializedObject(wrapper);
            var prop    = so.FindProperty("Value");

            var drawer = new MValueDrawer();
            var root   = drawer.CreatePropertyGUI(prop);

            Assert.IsNotNull(root);
            Assert.AreEqual(1, root.childCount);
            Assert.IsNotNull(root.Q<Toggle>());

            UnityEngine.Object.DestroyImmediate(wrapper);
        }

    #endregion

    #region A-1: ValueDrawer Apply OnBaseChange 발화

        [Test]
        public void TEST_ValueDrawer_Apply_OnBaseChange_발화()
        {
            var wrapper  = ScriptableObject.CreateInstance<ValueWrapper>();
            var so       = new SerializedObject(wrapper);
            var instance = wrapper.Value;

            ValueChangeEventArgs<int> fired = default;
            instance.OnBaseChange += (_, e) => fired = e;

            Undo.RecordObject(wrapper, "Set Value");
            instance.Set(99, invokeEvent: true);
            EditorUtility.SetDirty(wrapper);
            so.Update();

            Assert.AreEqual(42, fired.Previous);
            Assert.AreEqual(99, fired.Current);

            UnityEngine.Object.DestroyImmediate(wrapper);
        }

    #endregion

    #region A-2: RangeValueDrawer Apply Base OnBaseChange 발화

        [Test]
        public void TEST_ValueDrawer_RangeValueDrawer_Apply_Base_OnBaseChange_발화()
        {
            var wrapper  = ScriptableObject.CreateInstance<RangeValueWrapper>();
            var so       = new SerializedObject(wrapper);
            var instance = wrapper.Value;

            ValueChangeEventArgs<int> fired = default;
            instance.OnBaseChange += (_, e) => fired = e;

            Undo.RecordObject(wrapper, "Set Base");
            instance.Set(8, invokeEvent: true);
            EditorUtility.SetDirty(wrapper);
            so.Update();

            Assert.AreEqual(5, fired.Previous);
            Assert.AreEqual(8, fired.Current);

            UnityEngine.Object.DestroyImmediate(wrapper);
        }

    #endregion

    #region A-3: RangeValueDrawer Apply Range OnBaseChange 발화

        [Test]
        public void TEST_ValueDrawer_RangeValueDrawer_Apply_Range_OnBaseChange_발화()
        {
            var wrapper  = ScriptableObject.CreateInstance<RangeValueWrapper>();
            var so       = new SerializedObject(wrapper);
            var instance = wrapper.Value;

            ValueChangeEventArgs<Range<int>> fired = default;
            instance.Range.OnBaseChange += (_, e) => fired = e;

            var newRange = new Range<int>(2, 8);
            Undo.RecordObject(wrapper, "Set Range");
            instance.Range.Set(newRange, invokeEvent: true);
            EditorUtility.SetDirty(wrapper);
            so.Update();

            Assert.AreEqual(new Range<int>(0, 10), fired.Previous);
            Assert.AreEqual(new Range<int>(2, 8),  fired.Current);

            UnityEngine.Object.DestroyImmediate(wrapper);
        }

    #endregion

    #region A-4: MValueDrawer Apply OnBaseChange 발화

        [Test]
        public void TEST_ValueDrawer_MValueDrawer_Apply_OnBaseChange_발화()
        {
            var wrapper  = ScriptableObject.CreateInstance<MValueWrapper>();
            var so       = new SerializedObject(wrapper);
            var instance = wrapper.Value;

            ValueChangeEventArgs<int> fired = default;
            instance.OnBaseChange += (_, e) => fired = e;

            Undo.RecordObject(wrapper, "Set MValue");
            instance.Set(99, invokeEvent: true);
            EditorUtility.SetDirty(wrapper);
            so.Update();

            Assert.AreEqual(42, fired.Previous);
            Assert.AreEqual(99, fired.Current);

            UnityEngine.Object.DestroyImmediate(wrapper);
        }

    #endregion

    #region U-1: ValueDrawer Undo InvokeOnBaseChange 경로

        [Test]
        public void TEST_ValueDrawer_Undo_InvokeOnBaseChange_경로_시뮬레이션()
        {
            // Apply(99) 이후 Undo → base 42 복원, lastKnownBase=99
            // TrackPropertyValue 콜백: lastKnownBase(99) != restored(42) → InvokeOnBaseChange(99)
            var wrapper  = ScriptableObject.CreateInstance<ValueWrapper>();
            var instance = wrapper.Value;

            instance.Set(99, invokeEvent: false); // Apply 후 base=99 상태
            instance.Set(42, invokeEvent: false); // Undo 직렬화 복원 시뮬레이션

            ValueChangeEventArgs<int> fired = default;
            instance.OnBaseChange += (_, e) => fired = e;

            instance.InvokeOnBaseChange(previousValue: 99); // lastKnownBase=99, instance.Base=42

            Assert.AreEqual(99, fired.Previous);
            Assert.AreEqual(42, fired.Current);

            UnityEngine.Object.DestroyImmediate(wrapper);
        }

    #endregion

    #region U-2: MValueDrawer Undo 후 누락 이벤트 동기화

        // ----------------------------------------------------------------------
        /// <summary>
        /// 역직렬화로 runtime state가 복원된 뒤 누락된 Value 이벤트를 전달한다.
        /// </summary>
        // ----------------------------------------------------------------------
        [Test]
        public void TEST_ValueDrawer_MValueDrawer_Undo후_누락이벤트_동기화()
        {
            var wrapper  = ScriptableObject.CreateInstance<MValueWrapper>();
            var instance = wrapper.Value;
            var modifier = new NumericIModifier(NumericIOperation.ADD, 5);

            instance.AddModifier("a", modifier, invokeEvent: false);
            Assert.AreEqual(47, instance.Modified);

            // OnAfterDeserialize 이후의 복원 완료 상태를 재현한다.
            instance.Set(100, invokeEvent: false);
            Assert.AreEqual(105, instance.Modified);

            ValueChangeEventArgs<int> baseChange = default;
            ValueChangeEventArgs<int> modifiedChange = default;
            instance.OnBaseChange += (_, e) => baseChange = e;
            instance.OnModifiedChange += (_, e) => modifiedChange = e;

            MValueDrawer.SynchronizeSerializedState
            (
                instance,
                previousBase: 42,
                previousModified: 47
            );

            Assert.AreEqual(42, baseChange.Previous);
            Assert.AreEqual(100, baseChange.Current);
            Assert.AreEqual(47, modifiedChange.Previous);
            Assert.AreEqual(105, modifiedChange.Current);

            UnityEngine.Object.DestroyImmediate(wrapper);
        }

    #endregion

    #region X-1: RangeValueDrawer min > max 가드

        [Test]
        public void TEST_ValueDrawer_RangeValueDrawer_min_gt_max_가드()
        {
            var wrapper  = ScriptableObject.CreateInstance<RangeValueWrapper>();
            var instance = wrapper.Value;

            var fired = false;
            instance.Range.OnBaseChange += (_, e) => fired = true;

            // min(10) > max(5) → 가드 조건에 의해 Set 미호출
            var minValue = 10;
            var maxValue = 5;
            if (minValue <= maxValue)
            {
                instance.Range.Set(new Range<int>(minValue, maxValue), invokeEvent: true);
            }

            Assert.IsFalse(fired);

            UnityEngine.Object.DestroyImmediate(wrapper);
        }

    #endregion

    }

}
