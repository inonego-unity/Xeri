/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TableGridCellRegistryLoader.cs
수정일 : 2026-11-24

# 설명
[TableGridValueFormat]·[TableGridCellView]·[TableGridInlineCellView]·[TableGridCellEditor] 특성을 붙인 형식을 TypeCache로 찾아
TableGridCellRegistry에 자동 등록한다.

# 특이사항, 제약사항
Editor 어셈블리(inonego.Xeri.Editor)에서만 컴파일된다. 도메인 재로드마다 다시 등록한다.
특성을 붙인 형식은 추상이 아니고 매개변수 없는 생성자가 있어야 한다. 등록할 수 없는 형식은 경고만 남기고 건너뛴다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEditor;
using UnityEngine;

using inonego;
using inonego.Xeri;
using inonego.Xeri.UI;
using inonego.Xeri.UI.TableGrid;

namespace inonego.Xeri.UI.TableGrid.Editor
{
    // ============================================================
    /// <summary>
    /// 셀 구성 특성을 붙인 형식을 등록소에 자동 등록한다.
    /// </summary>
    // ============================================================
    internal static class TableGridCellRegistryLoader
    {

    #region 자동 등록

        [InitializeOnLoadMethod]
        private static void Load()
        {
            Register<TableGridValueFormatAttribute, TableGridValueFormat>(TableGridCellRegistry.RegisterFormat);
            Register<TableGridCellViewAttribute, TableGridCellView>(TableGridCellRegistry.RegisterView);
            Register<TableGridInlineCellViewAttribute, TableGridCellView>(TableGridCellRegistry.RegisterInlineView);
            Register<TableGridCellEditorAttribute, TableGridCellEditor>(TableGridCellRegistry.RegisterEditor);
        }

        private static void Register<TAttribute, TItem>(Action<Type, TItem, bool> register)
        where TAttribute : TableGridCellRegistrationAttribute
        where TItem : class
        {
            foreach (var type in TypeCache.GetTypesWithAttribute<TAttribute>())
            {
                if (type.IsAbstract || type.ContainsGenericParameters || !typeof(TItem).IsAssignableFrom(type))
                {
                    Debug.LogWarning($"[TableGrid] {type}은 {typeof(TItem).Name}의 구체 형식이 아니라 {typeof(TAttribute).Name}로 등록하지 않습니다.");
                    continue;
                }

                if (!TryCreate(type, out TItem instance))
                {
                    continue;
                }

                // 특성 하나가 값 형식 하나다. 같은 인스턴스를 여러 값 형식에 등록할 수 있다.
                foreach (TAttribute attribute in type.GetCustomAttributes(typeof(TAttribute), false))
                {
                    try
                    {
                        register(attribute.ValueType, instance, attribute.UseForChildren);
                    }
                    catch (ArgumentException exception)
                    {
                        Debug.LogWarning($"[TableGrid] {type}을 {attribute.ValueType}에 등록하지 못했습니다: {exception.Message}");
                    }
                }
            }
        }

        private static bool TryCreate<TItem>(Type type, out TItem instance) where TItem : class
        {
            try
            {
                instance = (TItem)Activator.CreateInstance(type, true);
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[TableGrid] {type}을 만들지 못했습니다(매개변수 없는 생성자 필요): {exception.Message}");
                instance = null;
                return false;
            }
        }

    #endregion

    }
}
