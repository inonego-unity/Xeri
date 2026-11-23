/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TableGridCellRegistry.cs
수정일 : 2026-11-24

# 설명
값 형식별 값 규칙·셀 뷰·Inline 셀 뷰·편집기 등록소와 Editor 자동 등록에 쓰는 특성을 정의한다.
열이 지정하지 않은 값 규칙·셀 뷰·편집기는 여기서 값 형식으로 찾는다. Inline 편집 열은 셀 뷰 대신 Inline 셀 뷰를 찾는다.
찾는 순서: 정확한 형식 → 기반 형식(가까운 순) → 인터페이스 → 없음(열이 기본값을 쓴다).
기반 형식·인터페이스 등록은 UseForChildren으로 등록한 것만 하위 형식에 쓴다.

# 특이사항, 제약사항
Runtime은 Register*를 직접 부른다. Editor는 특성을 붙인 형식을 TypeCache로 찾아 자동 등록한다(TableGrid/Editor).
같은 값 형식에 다시 등록하면 나중 등록이 이긴다. 열은 만들 때 값 규칙·셀 뷰를 찾으므로 등록은 열을 만들기 전에 한다.
셀 뷰(읽기용)와 Inline 셀 뷰(셀 안 입력칸)는 값 형식 하나에 따로 둔다. 읽기 열이 입력칸으로 바뀌지 않게 하기 위해서다.
Inline 셀 뷰 표는 기본 입력 셀 뷰(string·int·long·float·double·bool·enum)를 미리 담고 있다.
메인 스레드에서만 쓴다. 값 규칙·셀 뷰·Inline 셀 뷰·편집기는 같은 등록 표(RegistrationTable) 규칙을 쓰고, 공개 메서드는 종류별 진입점이다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

namespace inonego.Xeri.UI.TableGrid
{
    // ============================================================
    /// <summary>
    /// 값 형식별 값 규칙·셀 뷰·편집기 등록소.
    /// </summary>
    // ============================================================
    public static class TableGridCellRegistry
    {

    #region 내부 타입

        // ============================================================
        /// <summary>
        /// 한 종류(값 규칙·셀 뷰·편집기)의 값 형식별 등록 표.
        /// </summary>
        // ============================================================
        private sealed class RegistrationTable<T> where T : class
        {
            private readonly Dictionary<Type, Registration<T>> table = new();

            // --------------------------------------------------------------------------------
            /// <summary>
            /// <br/> item을 valueType에 등록한다. handledType은 item이 다루는 값 형식이며
            /// <br/> null이면(값 없는 셀 뷰) 어떤 값 형식에도 등록할 수 있다.
            /// </summary>
            // --------------------------------------------------------------------------------
            public void Register(Type valueType, T item, Type handledType, bool useForChildren, string itemName)
            {
                if (item == null)
                {
                    throw new ArgumentNullException(itemName);
                }

                if (valueType == null)
                {
                    throw new ArgumentNullException(nameof(valueType), "등록할 값 형식이 필요합니다.");
                }

                if (handledType != null && !handledType.IsAssignableFrom(valueType))
                {
                    throw new ArgumentException($"{handledType} 값을 다루는 항목은 {valueType}에 등록할 수 없습니다.", nameof(valueType));
                }

                table[valueType] = new Registration<T>(item, useForChildren);
            }

            // ------------------------------------------------------------
            /// <summary>
            /// valueType 등록을 지운다. 지웠으면 true다.
            /// </summary>
            // ------------------------------------------------------------
            public bool Unregister(Type valueType)
            {
                return valueType != null && table.Remove(valueType);
            }

            // ------------------------------------------------------------
            /// <summary>
            /// <br/> valueType 열에 쓸 항목을 찾는다. 없으면 null이다.
            /// <br/> 정확한 형식 → 기반 형식(가까운 순) → 인터페이스 순이며,
            /// <br/> 뒤의 둘은 UseForChildren 등록만 본다.
            /// </summary>
            // ------------------------------------------------------------
            public T Find(Type valueType)
            {
                if (valueType == null)
                {
                    return null;
                }

                if (table.TryGetValue(valueType, out var exact))
                {
                    return exact.Item;
                }

                for (var type = valueType.BaseType; type != null; type = type.BaseType)
                {
                    if (table.TryGetValue(type, out var inherited) && inherited.UseForChildren)
                    {
                        return inherited.Item;
                    }
                }

                foreach (var face in valueType.GetInterfaces())
                {
                    if (table.TryGetValue(face, out var implemented) && implemented.UseForChildren)
                    {
                        return implemented.Item;
                    }
                }

                return null;
            }
        }

        private readonly struct Registration<T>
        {
            // ------------------------------------------------------------
            /// <summary>
            /// 등록한 항목과 하위 형식 사용 여부를 담는다.
            /// </summary>
            // ------------------------------------------------------------
            public Registration(T item, bool useForChildren)
            {
                Item = item;
                UseForChildren = useForChildren;
            }

            // ------------------------------------------------------------
            /// <summary>
            /// 등록한 값 규칙·셀 뷰·편집기.
            /// </summary>
            // ------------------------------------------------------------
            public T Item { get; }

            // ------------------------------------------------------------
            /// <summary>
            /// 하위 형식·구현 형식 열에도 쓸지 여부.
            /// </summary>
            // ------------------------------------------------------------
            public bool UseForChildren { get; }
        }

    #endregion

    #region 필드

        private static readonly RegistrationTable<TableGridValueFormat> formats = new();
        private static readonly RegistrationTable<TableGridCellView> views = new();
        private static readonly RegistrationTable<TableGridCellView> inlineViews = new();
        private static readonly RegistrationTable<TableGridCellEditor> editors = new();

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// 기본 입력 셀 뷰를 Inline 셀 뷰 표에 미리 담는다.
        /// </summary>
        // ------------------------------------------------------------
        static TableGridCellRegistry()
        {
            TableGridDefaultInputCellViews.Register();
        }

    #endregion

    #region 값 규칙

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 값 규칙을 valueType에 등록한다. 값 규칙은 valueType 값을 받아야 한다.
        /// <br/> useForChildren이면 하위 형식·구현 형식 열에도 쓴다.
        /// </summary>
        // ----------------------------------------------------------------------
        public static void RegisterFormat(Type valueType, TableGridValueFormat format, bool useForChildren = false)
        {
            formats.Register(valueType, format, format?.ValueType, useForChildren, nameof(format));
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 값 규칙을 자기 값 형식(ValueType)에 등록한다.
        /// </summary>
        // ------------------------------------------------------------
        public static void RegisterFormat(TableGridValueFormat format, bool useForChildren = false)
        {
            RegisterFormat(format?.ValueType, format, useForChildren);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// valueType의 값 규칙 등록을 지운다. 지웠으면 true다.
        /// </summary>
        // ------------------------------------------------------------
        public static bool UnregisterFormat(Type valueType)
        {
            return formats.Unregister(valueType);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// valueType 열에 쓸 값 규칙을 찾는다. 없으면 null이다.
        /// </summary>
        // ------------------------------------------------------------
        public static TableGridValueFormat FindFormat(Type valueType)
        {
            return formats.Find(valueType);
        }

    #endregion

    #region 셀 뷰

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 셀 뷰를 valueType에 등록한다.
        /// <br/> 셀 뷰는 valueType 값을 받거나 값 없는 셀 뷰여야 한다.
        /// <br/> useForChildren이면 하위 형식·구현 형식 열에도 쓴다.
        /// </summary>
        // ------------------------------------------------------------
        public static void RegisterView(Type valueType, TableGridCellView view, bool useForChildren = false)
        {
            views.Register(valueType, view, view?.ValueType, useForChildren, nameof(view));
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 셀 뷰를 자기 값 형식(ValueType)에 등록한다.
        /// </summary>
        // ------------------------------------------------------------
        public static void RegisterView(TableGridCellView view, bool useForChildren = false)
        {
            RegisterView(view?.ValueType, view, useForChildren);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// valueType의 셀 뷰 등록을 지운다. 지웠으면 true다.
        /// </summary>
        // ------------------------------------------------------------
        public static bool UnregisterView(Type valueType)
        {
            return views.Unregister(valueType);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// valueType 열에 쓸 셀 뷰를 찾는다. 없으면 null이다.
        /// </summary>
        // ------------------------------------------------------------
        public static TableGridCellView FindView(Type valueType)
        {
            return views.Find(valueType);
        }

    #endregion

    #region Inline 셀 뷰

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> Inline 편집 열의 셀 안 입력칸을 그리는 셀 뷰를 valueType에 등록한다.
        /// <br/> 셀 뷰는 valueType 값을 받아야 한다(보통 TableGridInputCellView).
        /// <br/> useForChildren이면 하위 형식·구현 형식 열에도 쓴다.
        /// </summary>
        // --------------------------------------------------------------------------------
        public static void RegisterInlineView(Type valueType, TableGridCellView view, bool useForChildren = false)
        {
            inlineViews.Register(valueType, view, view?.ValueType, useForChildren, nameof(view));
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Inline 셀 뷰를 자기 값 형식(ValueType)에 등록한다.
        /// </summary>
        // ------------------------------------------------------------
        public static void RegisterInlineView(TableGridCellView view, bool useForChildren = false)
        {
            RegisterInlineView(view?.ValueType, view, useForChildren);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// valueType의 Inline 셀 뷰 등록을 지운다. 지웠으면 true다.
        /// </summary>
        // ------------------------------------------------------------
        public static bool UnregisterInlineView(Type valueType)
        {
            return inlineViews.Unregister(valueType);
        }

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> valueType의 Inline 편집 열에 쓸 셀 뷰를 찾는다. 없으면 null이다.
        /// <br/> 기본 입력 셀 뷰(string·int·long·float·double·bool·enum)도 여기서 찾는다.
        /// </summary>
        // --------------------------------------------------------------------------------
        public static TableGridCellView FindInlineView(Type valueType)
        {
            return inlineViews.Find(valueType);
        }

    #endregion

    #region 편집기

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 편집기를 valueType에 등록한다. 편집기는 valueType 값을 받아야 한다.
        /// <br/> useForChildren이면 하위 형식·구현 형식 열에도 쓴다.
        /// </summary>
        // ----------------------------------------------------------------------
        public static void RegisterEditor(Type valueType, TableGridCellEditor editor, bool useForChildren = false)
        {
            editors.Register(valueType, editor, editor?.ValueType, useForChildren, nameof(editor));
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 편집기를 자기 값 형식(ValueType)에 등록한다.
        /// </summary>
        // ------------------------------------------------------------
        public static void RegisterEditor(TableGridCellEditor editor, bool useForChildren = false)
        {
            RegisterEditor(editor?.ValueType, editor, useForChildren);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// valueType의 편집기 등록을 지운다. 지웠으면 true다.
        /// </summary>
        // ------------------------------------------------------------
        public static bool UnregisterEditor(Type valueType)
        {
            return editors.Unregister(valueType);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// valueType 열에 쓸 편집기를 찾는다. 없으면 null이다.
        /// </summary>
        // ------------------------------------------------------------
        public static TableGridCellEditor FindEditor(Type valueType)
        {
            return editors.Find(valueType);
        }

    #endregion

    }

    // ============================================================
    /// <summary>
    /// Editor 자동 등록 특성의 공통 면.
    /// </summary>
    // ============================================================
    public abstract class TableGridCellRegistrationAttribute : Attribute
    {

    #region 등록 사양

        // ------------------------------------------------------------
        /// <summary>
        /// 등록할 값 형식으로 특성을 만든다.
        /// </summary>
        // ------------------------------------------------------------
        protected TableGridCellRegistrationAttribute(Type valueType)
        {
            ValueType = valueType ?? throw new ArgumentNullException(nameof(valueType));
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 등록할 값 형식.
        /// </summary>
        // ------------------------------------------------------------
        public Type ValueType { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 하위 형식·구현 형식 열에도 쓸지 여부.
        /// </summary>
        // ------------------------------------------------------------
        public bool UseForChildren { get; set; }

    #endregion

    }

    // ================================================================================
    /// <summary>
    /// 값 규칙 형식에 붙여 Editor에서 값 형식에 자동 등록한다(매개변수 없는 생성자 필요).
    /// </summary>
    // ================================================================================
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class TableGridValueFormatAttribute : TableGridCellRegistrationAttribute
    {
        // ------------------------------------------------------------
        /// <summary>
        /// 등록할 값 형식으로 특성을 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public TableGridValueFormatAttribute(Type valueType) : base(valueType)
        {
            // NONE
        }
    }

    // ======================================================================
    /// <summary>
    /// 셀 뷰 형식에 붙여 Editor에서 값 형식에 자동 등록한다(매개변수 없는 생성자 필요).
    /// </summary>
    // ======================================================================
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class TableGridCellViewAttribute : TableGridCellRegistrationAttribute
    {
        // ------------------------------------------------------------
        /// <summary>
        /// 등록할 값 형식으로 특성을 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public TableGridCellViewAttribute(Type valueType) : base(valueType)
        {
            // NONE
        }
    }

    // ======================================================================
    /// <summary>
    /// Inline 셀 뷰 형식에 붙여 Editor에서 값 형식에 자동 등록한다(매개변수 없는 생성자 필요).
    /// </summary>
    // ======================================================================
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class TableGridInlineCellViewAttribute : TableGridCellRegistrationAttribute
    {
        // ------------------------------------------------------------
        /// <summary>
        /// 등록할 값 형식으로 특성을 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public TableGridInlineCellViewAttribute(Type valueType) : base(valueType)
        {
            // NONE
        }
    }

    // ======================================================================
    /// <summary>
    /// 편집기 형식에 붙여 Editor에서 값 형식에 자동 등록한다(매개변수 없는 생성자 필요).
    /// </summary>
    // ======================================================================
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class TableGridCellEditorAttribute : TableGridCellRegistrationAttribute
    {
        // ------------------------------------------------------------
        /// <summary>
        /// 등록할 값 형식으로 특성을 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public TableGridCellEditorAttribute(Type valueType) : base(valueType)
        {
            // NONE
        }
    }
}
