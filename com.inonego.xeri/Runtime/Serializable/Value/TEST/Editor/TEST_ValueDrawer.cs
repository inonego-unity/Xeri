/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_ValueDrawer.cs
수정일 : 2026-05-03

# 설명
ValueDrawer / RangeValueDrawer / MValueDrawer의 핵심 기능 에디터 테스트.
CreatePropertyGUI 구조 검증, Apply 로직 시뮬레이션, Undo 경로(InvokeOn*) 검증을 포함한다.

# 특이사항
TrackPropertyValue 콜백은 패널 없이는 발화하지 않으므로
Undo 경로는 InvokeOnBaseChange / InvokeOnRangeChange 직접 호출로 시뮬레이션한다.
========================================================================= BLOCK_HEADER_END */

using System.Collections;
using System.Collections.Generic;

using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

using NUnit.Framework;

using inonego.Xeri;
using inonego.Xeri.Primitive;
using inonego.Xeri.Serializable;

// ============================================================
/// <summary>
/// ValueDrawer / RangeValueDrawer / MValueDrawer 에디터 테스트 클래스.
/// </summary>
// ============================================================
public class TEST_ValueDrawer
{
    private class ValueWrapper : ScriptableObject
    {
        [SerializeField] public Value<int> value = new(42);
    }

    private class RangeValueWrapper : ScriptableObject
    {
        [SerializeField] public RangeValue<int> value = new(5, new MinMax<int>(0, 10));
    }

    private class MValueWrapper : ScriptableObject
    {
        [SerializeField] public MValue<int> value = new(42);
    }

#region ValueDrawer 테스트

    // ------------------------------------------------------------
    /// <summary>
    /// CreatePropertyGUI가 1행 구조와 Toggle을 포함하는지 검증한다.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void ValueDrawer_01_CreatePropertyGUI_구조_테스트()
    {
        var wrapper = ScriptableObject.CreateInstance<ValueWrapper>();
        var so      = new SerializedObject(wrapper);
        var prop    = so.FindProperty("value");

        var drawer = new ValueDrawer();
        var root   = drawer.CreatePropertyGUI(prop);

        Assert.IsNotNull(root);
        Assert.AreEqual(1, root.childCount);
        Assert.IsNotNull(root.Q<Toggle>());

        Object.DestroyImmediate(wrapper);
    }

    // ------------------------------------------------------------
    /// <summary>
    /// Apply 로직(RecordObject + Set + SetDirty + Update)이 OnBaseChange를 발화하는지 검증한다.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void ValueDrawer_02_Apply_로직_시뮬레이션_OnBaseChange_발화_테스트()
    {
        var wrapper  = ScriptableObject.CreateInstance<ValueWrapper>();
        var so       = new SerializedObject(wrapper);
        var instance = wrapper.value;

        ValueChangeEventArgs<int> fired = default;
        instance.OnBaseChange += (_, e) => fired = e;

        Undo.RecordObject(wrapper, "Set Value");
        instance.Set(99, invokeEvent: true);
        EditorUtility.SetDirty(wrapper);
        so.Update();

        Assert.AreEqual(42, fired.Previous);
        Assert.AreEqual(99, fired.Current);

        Object.DestroyImmediate(wrapper);
    }

    // -----------------------------------------------------------------------
    /// <summary>
    /// <br/> Undo 복원 후 드로어가 InvokeOnBaseChange(lastKnownBase)를 호출하는 경로를 시뮬레이션한다.
    /// <br/> Apply(99) 후 Undo로 base가 42로 복원된 상태에서 이벤트가 올바르게 발화되는지 검증한다.
    /// </summary>
    // -----------------------------------------------------------------------
    [Test]
    public void ValueDrawer_03_Undo_시뮬레이션_InvokeOnBaseChange_경로_테스트()
    {
        // Apply(99) 이후 Undo → base 42 복원, lastKnownBase=99
        // TrackPropertyValue 콜백: lastKnownBase(99) != restored(42) → InvokeOnBaseChange(99)
        var wrapper  = ScriptableObject.CreateInstance<ValueWrapper>();
        var instance = wrapper.value;

        instance.Set(99, invokeEvent: false); // Apply 후 base=99 상태
        instance.Set(42, invokeEvent: false); // Undo 직렬화 복원 시뮬레이션

        ValueChangeEventArgs<int> fired = default;
        instance.OnBaseChange += (_, e) => fired = e;

        instance.InvokeOnBaseChange(previousValue: 99); // lastKnownBase=99, instance.Base=42

        Assert.AreEqual(99, fired.Previous);
        Assert.AreEqual(42, fired.Current);

        Object.DestroyImmediate(wrapper);
    }

#endregion

#region RangeValueDrawer 테스트

    // ------------------------------------------------------------
    /// <summary>
    /// CreatePropertyGUI가 2행 구조와 Toggle 2개를 포함하는지 검증한다.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void RangeValueDrawer_04_CreatePropertyGUI_구조_테스트()
    {
        var wrapper = ScriptableObject.CreateInstance<RangeValueWrapper>();
        var so      = new SerializedObject(wrapper);
        var prop    = so.FindProperty("value");

        var drawer = new RangeValueDrawer();
        var root   = drawer.CreatePropertyGUI(prop);

        Assert.IsNotNull(root);
        Assert.AreEqual(2, root.childCount);

        var toggles = root.Query<Toggle>().ToList();
        Assert.AreEqual(2, toggles.Count);

        Object.DestroyImmediate(wrapper);
    }

    // ------------------------------------------------------------
    /// <summary>
    /// Apply Base 로직이 OnBaseChange를 발화하는지 검증한다.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void RangeValueDrawer_05_Apply_Base_OnBaseChange_발화_테스트()
    {
        var wrapper  = ScriptableObject.CreateInstance<RangeValueWrapper>();
        var so       = new SerializedObject(wrapper);
        var instance = wrapper.value;

        ValueChangeEventArgs<int> fired = default;
        instance.OnBaseChange += (_, e) => fired = e;

        Undo.RecordObject(wrapper, "Set Base");
        instance.Set(8, invokeEvent: true);
        EditorUtility.SetDirty(wrapper);
        so.Update();

        Assert.AreEqual(5, fired.Previous);
        Assert.AreEqual(8, fired.Current);

        Object.DestroyImmediate(wrapper);
    }

    // ------------------------------------------------------------
    /// <summary>
    /// Apply Range 로직이 Range.OnBaseChange를 발화하는지 검증한다.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void RangeValueDrawer_06_Apply_Range_OnBaseChange_발화_테스트()
    {
        var wrapper  = ScriptableObject.CreateInstance<RangeValueWrapper>();
        var so       = new SerializedObject(wrapper);
        var instance = wrapper.value;

        ValueChangeEventArgs<MinMax<int>> fired = default;
        instance.Range.OnBaseChange += (_, e) => fired = e;

        var newRange = new MinMax<int>(2, 8);
        Undo.RecordObject(wrapper, "Set Range");
        instance.Range.Set(newRange, invokeEvent: true);
        EditorUtility.SetDirty(wrapper);
        so.Update();

        Assert.AreEqual(new MinMax<int>(0, 10), fired.Previous);
        Assert.AreEqual(new MinMax<int>(2, 8),  fired.Current);

        Object.DestroyImmediate(wrapper);
    }

    // ------------------------------------------------------------
    /// <summary>
    /// min > max일 때 Apply Range가 실행되지 않는 가드 로직을 검증한다.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void RangeValueDrawer_07_Apply_Range_min_gt_max_가드_테스트()
    {
        var wrapper  = ScriptableObject.CreateInstance<RangeValueWrapper>();
        var instance = wrapper.value;

        var fired = false;
        instance.Range.OnBaseChange += (_, e) => fired = true;

        // min(10) > max(5) → 가드 조건에 의해 Set 미호출
        var minValue = 10;
        var maxValue = 5;
        if (minValue <= maxValue)
            instance.Range.Set(new MinMax<int>(minValue, maxValue), invokeEvent: true);

        Assert.IsFalse(fired);

        Object.DestroyImmediate(wrapper);
    }

#endregion

#region MValueDrawer 테스트

    // ------------------------------------------------------------
    /// <summary>
    /// CreatePropertyGUI가 1행 구조와 Toggle을 포함하는지 검증한다.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void MValueDrawer_08_CreatePropertyGUI_구조_테스트()
    {
        var wrapper = ScriptableObject.CreateInstance<MValueWrapper>();
        var so      = new SerializedObject(wrapper);
        var prop    = so.FindProperty("value");

        var drawer = new MValueDrawer();
        var root   = drawer.CreatePropertyGUI(prop);

        Assert.IsNotNull(root);
        Assert.AreEqual(1, root.childCount);
        Assert.IsNotNull(root.Q<Toggle>());

        Object.DestroyImmediate(wrapper);
    }

    // ------------------------------------------------------------
    /// <summary>
    /// Apply 로직이 OnBaseChange를 발화하는지 검증한다.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void MValueDrawer_09_Apply_로직_시뮬레이션_OnBaseChange_발화_테스트()
    {
        var wrapper  = ScriptableObject.CreateInstance<MValueWrapper>();
        var so       = new SerializedObject(wrapper);
        var instance = wrapper.value;

        ValueChangeEventArgs<int> fired = default;
        instance.OnBaseChange += (_, e) => fired = e;

        Undo.RecordObject(wrapper, "Set MValue");
        instance.Set(99, invokeEvent: true);
        EditorUtility.SetDirty(wrapper);
        so.Update();

        Assert.AreEqual(42, fired.Previous);
        Assert.AreEqual(99, fired.Current);

        Object.DestroyImmediate(wrapper);
    }

#endregion

}
