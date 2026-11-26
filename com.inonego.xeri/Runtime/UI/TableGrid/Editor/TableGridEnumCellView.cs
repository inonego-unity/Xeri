/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TableGridEnumCellView.cs
수정일 : 2026-11-26

# 설명
enum 값 열(Inline 편집)의 셀 안 입력칸 셀 뷰다. 단일 값 enum은 EnumField로, [Flags] enum은 여러 값을 고르는
EnumFlagsField로 보인다. [TableGridInlineCellView]로 모든 enum 형식 열에 자동 등록해 Runtime 기본 enum 입력칸을 대체한다.

# 특이사항, 제약사항
Editor 어셈블리(inonego.Xeri.Editor)에서만 컴파일된다(EnumFlagsField가 UnityEditor.UIElements에 있다).
입력칸 형식은 셀을 만들 때 알 수 없으므로 두 입력칸을 한 요소(TableGridEnumInput)에 두고 연결할 때 enum 형식으로 고른다.
값 변경 알림: EnumField의 ChangeEvent<Enum>은 위로 전달되어 요소가 받는다. EnumFlagsField는 어떤 형식의 ChangeEvent를
보내는지(int 마스크·Enum)에 기대지 않도록 두 형식 모두에서 현재 값을 읽어, 달라졌을 때만 요소가 ChangeEvent<Enum>을 한 번 보낸다.
enum 형식은 context.ValueType으로 알고, 모르면 값 자신의 형식을 쓴다.
EnumFlagsField는 정의된 플래그를 모두 고른 값을 Everything(모든 비트)으로 바꾸므로, 형식에 그 값이 없으면 요소의 값과 알림은
정의된 플래그를 모두 합한 값으로 되돌린다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

using inonego;
using inonego.Xeri;
using inonego.Xeri.UI;
using inonego.Xeri.UI.TableGrid;

namespace inonego.Xeri.UI.TableGrid.Editor
{
    // ============================================================
    /// <summary>
    /// 단일 값·[Flags] enum 입력칸을 한 요소에 둔 입력 요소.
    /// </summary>
    // ============================================================
    internal sealed class TableGridEnumInput : VisualElement, INotifyValueChanged<Enum>
    {

    #region 필드

        private readonly EnumField choice = new();
        private readonly EnumFlagsField flags = new();
        private Type enumType;
        private bool isFlags;
        private Enum flagsValue;

        // [Flags] 형식에 정의된 비트를 모두 합한 값과 그 enum 값. 여러 값 입력칸의 Everything을 되돌릴 때 쓴다.
        private ulong definedFlags;
        private Enum allFlags;

        private INotifyValueChanged<Enum> Active => isFlags ? flags : choice;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// 두 입력칸을 두고 enum 형식이 정해질 때까지 단일 값 입력칸만 보인다.
        /// </summary>
        // ------------------------------------------------------------
        public TableGridEnumInput()
        {
            flags.style.display = DisplayStyle.None;
            Add(choice);
            Add(flags);

            // 여러 값 입력칸의 알림은 형식이 무엇이든 값이 달라졌을 때만 요소의 알림 하나로 바꾼다.
            flags.RegisterCallback<ChangeEvent<int>>(_ => ForwardFlagsChange());
            flags.RegisterCallback<ChangeEvent<Enum>>
            (
                evt =>
                {
                    evt.StopPropagation();
                    ForwardFlagsChange();
                }
            );
        }

    #endregion

    #region 값

        // ------------------------------------------------------------
        /// <summary>
        /// 지금 보이는 입력칸의 값.
        /// </summary>
        // ------------------------------------------------------------
        public Enum value
        {
            get => isFlags ? ReadFlags() : choice.value;
            set => Active.value = value;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 지금 보이는 입력칸에 값을 알림 없이 보인다.
        /// </summary>
        // ------------------------------------------------------------
        public void SetValueWithoutNotify(Enum newValue)
        {
            Active.SetValueWithoutNotify(newValue);
            if (isFlags)
            {
                flagsValue = ReadFlags();
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> enum 형식에 맞는 입력칸을 보인다. 같은 형식이면 다시 만들지 않는다.
        /// <br/> [Flags] enum이면 여러 값을 고르는 입력칸을 보인다.
        /// </summary>
        // ------------------------------------------------------------
        public void Use(Type type)
        {
            if (type == enumType)
            {
                return;
            }

            enumType = type;
            isFlags = type.IsDefined(typeof(FlagsAttribute), false);

            var first = (Enum)Enum.ToObject(type, 0);
            definedFlags = 0;
            allFlags = null;
            if (isFlags)
            {
                foreach (Enum defined in Enum.GetValues(type))
                {
                    definedFlags |= ToBits(defined);
                }

                allFlags = (Enum)Enum.ToObject(type, definedFlags);
                flags.Init(first);
            }
            else
            {
                choice.Init(first);
            }

            choice.style.display = isFlags ? DisplayStyle.None : DisplayStyle.Flex;
            flags.style.display = isFlags ? DisplayStyle.Flex : DisplayStyle.None;
            flagsValue = isFlags ? ReadFlags() : null;
        }

        private void ForwardFlagsChange()
        {
            var next = ReadFlags();
            if (Equals(next, flagsValue))
            {
                return;
            }

            var previous = flagsValue;
            flagsValue = next;
            using (var change = ChangeEvent<Enum>.GetPooled(previous, next))
            {
                change.target = this;
                SendEvent(change);
            }
        }

        // 여러 값 입력칸의 값을 읽는다. EnumFlagsField는 정의된 플래그를 모두 고른 값을 Everything(모든 비트)으로 바꿔 돌려준다.
        // 형식에 그 값이 정의되어 있지 않으면 정의된 플래그를 모두 합한 값으로 되돌려, 셀에 정의되지 않은 값이 들어가지 않게 한다.
        private Enum ReadFlags()
        {
            var current = flags.value;
            if (current == null)
            {
                return null;
            }

            var bits = ToBits(current);
            var everything = (bits & definedFlags) == definedFlags && (bits & ~definedFlags) != 0;
            return everything ? allFlags : current;
        }

        // enum 값을 기반 형식의 비트 그대로 읽는다. 부호 있는 형식의 음수도 비트를 유지한다.
        private static ulong ToBits(Enum value)
        {
            var signed = value.GetTypeCode() is TypeCode.SByte or TypeCode.Int16 or TypeCode.Int32 or TypeCode.Int64;
            return signed ? unchecked((ulong)Convert.ToInt64(value)) : Convert.ToUInt64(value);
        }

    #endregion

    }

    // ============================================================
    /// <summary>
    /// enum 입력칸 셀 뷰.
    /// </summary>
    // ============================================================
    [TableGridInlineCellView(typeof(Enum), UseForChildren = true)]
    internal sealed class TableGridEnumCellView : TableGridInputCellView<TableGridEnumInput, Enum>
    {

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// enum 입력칸 셀 뷰를 만든다. 값이 바뀌면 바로 확정을 요청한다.
        /// </summary>
        // ------------------------------------------------------------
        public TableGridEnumCellView() : base(() => new TableGridEnumInput(), true)
        {
            // NONE
        }

    #endregion

    #region 입력칸

        // ------------------------------------------------------------
        /// <summary>
        /// enum 형식에 맞는 입력칸을 고르고 값을 보인다.
        /// </summary>
        // ------------------------------------------------------------
        protected override void Show(TableGridEnumInput field, Enum value, in TableGridCellContext context)
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

            field.Use(enumType);
            field.SetValueWithoutNotify(value ?? (Enum)Enum.ToObject(enumType, 0));
        }

    #endregion

    }
}
