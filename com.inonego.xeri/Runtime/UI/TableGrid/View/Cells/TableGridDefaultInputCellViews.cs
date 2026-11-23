/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TableGridDefaultInputCellViews.cs
수정일 : 2026-11-24

# 설명
Inline 열이 셀 뷰를 지정하지 않았을 때 쓰는 기본 입력 셀 뷰(string·int·long·float·double·bool·enum)와
이들을 등록소에 담는 진입점을 정의한다. 글자·숫자 입력칸은 Inline 열의 반영 시점(Immediate·Confirm)을 따르고,
고르는 입력칸(bool·enum)은 값이 바뀌는 즉시 확정을 요청한다.

# 특이사항, 제약사항
등록소(TableGridCellRegistry)가 처음 쓰일 때 한 번 담고, 같은 값 형식에 다시 등록하면 나중 등록이 이긴다.
enum은 System.Enum에 UseForChildren으로 담아 모든 enum 형식 열에 쓰며, 입력칸 형식은 context.ValueType으로 맞춘다.
UnityEditor 전용 입력칸은 Editor 쪽 TableGrid/Editor가 [TableGridInlineCellView]로 등록한다(오브젝트 선택, [Flags] enum을
다루는 enum 입력칸이 여기 enum 입력칸을 대체한다). 여기 enum 입력칸은 [Flags]를 구분하지 않는다(Editor가 아닌 곳용).
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UIElements;

namespace inonego.Xeri.UI.TableGrid
{
    // ============================================================
    /// <summary>
    /// 기본 입력 셀 뷰와 등록 진입점.
    /// </summary>
    // ============================================================
    internal static class TableGridDefaultInputCellViews
    {

    #region 내부 타입

        // ============================================================
        /// <summary>
        /// 글자 입력칸. 값이 null이면 빈 글자로 보인다.
        /// </summary>
        // ============================================================
        private sealed class TextView : TableGridInputCellView<TextField, string>
        {
            public TextView() : base(() => new TextField(), false)
            {
                // NONE
            }

            protected override void Show(TextField field, string value, in TableGridCellContext context)
            {
                field.SetValueWithoutNotify(value ?? string.Empty);
            }
        }

        // ============================================================
        /// <summary>
        /// enum 입력칸. 후보를 보이려면 enum 형식을 알아야 한다.
        /// </summary>
        // ============================================================
        private sealed class EnumView : TableGridInputCellView<EnumField, Enum>
        {
            public EnumView() : base(() => new EnumField(), true)
            {
                // NONE
            }

            protected override void Show(EnumField field, Enum value, in TableGridCellContext context)
            {
                // 값 형식은 문맥이 알려 주고, 모르면 값 자신의 형식을 쓴다.
                var enumType = context.ValueType;
                if (enumType == null || !enumType.IsEnum)
                {
                    enumType = value?.GetType();
                }

                if (enumType == null)
                {
                    return;
                }

                // 같은 열이면 형식이 그대로이므로 형식이 바뀔 때만 후보를 다시 만든다.
                var first = (Enum)Enum.ToObject(enumType, 0);
                if (field.value == null || field.value.GetType() != enumType)
                {
                    field.Init(first);
                }

                field.SetValueWithoutNotify(value ?? first);
            }
        }

    #endregion

    #region 등록

        // ------------------------------------------------------------
        /// <summary>
        /// 기본 입력 셀 뷰를 등록소의 Inline 셀 뷰 표에 담는다.
        /// </summary>
        // ------------------------------------------------------------
        public static void Register()
        {
            TableGridCellRegistry.RegisterInlineView(typeof(string), new TextView());
            TableGridCellRegistry.RegisterInlineView(typeof(int), new TableGridInputCellView<IntegerField, int>(() => new IntegerField(), false));
            TableGridCellRegistry.RegisterInlineView(typeof(long), new TableGridInputCellView<LongField, long>(() => new LongField(), false));
            TableGridCellRegistry.RegisterInlineView(typeof(float), new TableGridInputCellView<FloatField, float>(() => new FloatField(), false));
            TableGridCellRegistry.RegisterInlineView(typeof(double), new TableGridInputCellView<DoubleField, double>(() => new DoubleField(), false));
            TableGridCellRegistry.RegisterInlineView(typeof(bool), new TableGridInputCellView<Toggle, bool>(() => new Toggle(), true));
            TableGridCellRegistry.RegisterInlineView(typeof(Enum), new EnumView(), useForChildren: true);
        }

    #endregion

    }
}
