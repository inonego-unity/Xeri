/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : ICollectionCellDrawer.cs
수정일 : 2026-11-19

# 설명
셀 하나를 그리는 drawer 계약, 셀 값을 만드는 생성 계약,
drawer가 표에 요청을 보내는 셀 문맥(CollectionCell)과 drawer 레지스트리를 둔다.

# 특이사항, 제약사항
drawer는 CollectionSession을 직접 부르지 않는다. 값 변경·확정·상세 편집은 셀 문맥을 거쳐 표(호스트)에 요청한다.
drawer 선택은 열마다 한 번: 열 지정 drawer → 등록 drawer(FullName 순, 먼저 맞는 것) → 기본 drawer.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

using inonego;
using inonego.Xeri;

namespace inonego.Xeri.Serializable.Editor
{
    // ============================================================
    /// <summary>
    /// 셀 drawer의 요청을 처리하는 표.
    /// </summary>
    // ============================================================
    internal interface ICollectionCellHost
    {
        // ------------------------------------------------------------
        /// <summary>
        /// 셀이 아직 같은 직렬화 위치를 가리키면 현재 속성을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        bool TryResolve(int sourceIndex, CollectionColumn column, string propertyPath, out SerializedProperty property);

        // ------------------------------------------------------------
        /// <summary>
        /// 열의 편집 방식에 맞게 셀 값을 새 값으로 바꾼다.
        /// </summary>
        // ------------------------------------------------------------
        void Replace(int sourceIndex, CollectionColumn column, object value);

        // ------------------------------------------------------------
        /// <summary>
        /// 셀 안에서 이미 쓴 값을 확정한다.
        /// </summary>
        // ------------------------------------------------------------
        void Commit(string propertyPath);

        // ------------------------------------------------------------
        /// <summary>
        /// 셀의 상세 편집 또는 초안 편집을 연다.
        /// </summary>
        // ------------------------------------------------------------
        void OpenDetail(int sourceIndex, CollectionColumn column);
    }

    // ============================================================
    /// <summary>
    /// drawer에 전달하는 셀 문맥.
    /// </summary>
    // ============================================================
    internal readonly struct CollectionCell
    {

    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// 요청을 처리하는 표를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public ICollectionCellHost Host { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 행 인덱스를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public int SourceIndex { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 열 정의를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public CollectionColumn Column { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 바인드 시점의 셀 속성을 반환하며 없으면 null이다.
        /// </summary>
        // ------------------------------------------------------------
        public SerializedProperty Property { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 읽기 전용 표인지 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public bool IsReadOnly { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 셀 값을 바꿀 수 있는지 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public bool CanEdit => !IsReadOnly && Column.Edit != CollectionCellEdit.ReadOnly;

        private readonly string propertyPath;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// 셀 문맥을 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public CollectionCell
        (
            ICollectionCellHost host, int sourceIndex, CollectionColumn column,
            SerializedProperty property, bool isReadOnly
        )
        {
            Host = host ?? throw new ArgumentNullException(nameof(host));
            SourceIndex = sourceIndex;
            Column = column ?? throw new ArgumentNullException(nameof(column));
            Property = property;
            IsReadOnly = isReadOnly;
            propertyPath = property?.propertyPath;
        }

    #endregion

    #region 요청

        // ------------------------------------------------------------
        /// <summary>
        /// 셀이 아직 같은 위치를 가리키면 현재 속성을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public bool TryResolve(out SerializedProperty property)
        {
            property = null;
            return propertyPath != null && Host.TryResolve(SourceIndex, Column, propertyPath, out property);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 셀 값을 새 값으로 바꾸도록 요청한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Replace(object value)
        {
            if (CanEdit && TryResolve(out _))
            {
                Host.Replace(SourceIndex, Column, value);
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 셀 안에서 쓴 값을 확정하도록 요청한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Commit()
        {
            if (CanEdit && TryResolve(out var property))
            {
                Host.Commit(property.propertyPath);
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 셀의 상세 편집을 열도록 요청한다.
        /// </summary>
        // ------------------------------------------------------------
        public void OpenDetail()
        {
            if (TryResolve(out _))
            {
                Host.OpenDetail(SourceIndex, Column);
            }
        }

    #endregion

    }

    // ============================================================
    /// <summary>
    /// 셀 하나를 그리는 drawer.
    /// </summary>
    // ============================================================
    internal interface ICollectionCellDrawer
    {
        // ------------------------------------------------------------
        /// <summary>
        /// 이 drawer가 그릴 열인지 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        bool Matches(CollectionColumn column);

        // ------------------------------------------------------------
        /// <summary>
        /// 행이 바뀌어도 재사용할 셀 UI를 만든다.
        /// </summary>
        // ------------------------------------------------------------
        VisualElement CreateElement(CollectionColumn column);

        // ------------------------------------------------------------
        /// <summary>
        /// 셀 UI를 셀 문맥에 연결한다.
        /// </summary>
        // ------------------------------------------------------------
        void Bind(VisualElement element, CollectionCell cell);

        // ------------------------------------------------------------
        /// <summary>
        /// 셀 UI의 연결을 끊고 UI는 재사용할 수 있게 둔다.
        /// </summary>
        // ------------------------------------------------------------
        void Unbind(VisualElement element);

        // ------------------------------------------------------------
        /// <summary>
        /// 검색과 접힌 표시에 쓰는 요약 문자열을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        string GetSummary(SerializedProperty property, CollectionColumn column);

        // ------------------------------------------------------------
        /// <summary>
        /// 새 값을 만드는 능력을 반환하며 없으면 null이다.
        /// </summary>
        // ------------------------------------------------------------
        ICollectionCellValueCreator ValueCreator { get; }
    }

    // ============================================================
    /// <summary>
    /// 선언 형식에 맞는 새 셀 값을 만드는 능력.
    /// </summary>
    // ============================================================
    internal interface ICollectionCellValueCreator
    {
        // ------------------------------------------------------------
        /// <summary>
        /// 구현 형식을 선택기로 고르게 한다.
        /// </summary>
        // ------------------------------------------------------------
        void PickType(Type declaredType, VisualElement anchor, Action<Type> onPicked);

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 새 행에 넣을 형식을 정한다.
        /// <br/> 바로 만들 수 있으면 선택기 없이 전달하고 불가하면 onFailed로 알린다.
        /// </summary>
        // ----------------------------------------------------------------------
        void PickTypeForNew
        (
            Type declaredType, VisualElement anchor, Action<Type> onPicked, Action<string> onFailed
        );

        // ------------------------------------------------------------
        /// <summary>
        /// 고른 형식의 새 값을 만든다.
        /// </summary>
        // ------------------------------------------------------------
        bool TryCreate(Type type, out object value, out string reason);

        // ------------------------------------------------------------
        /// <summary>
        /// 셀의 현재 값을 편집용으로 복제하며 값이 없으면 null이다.
        /// </summary>
        // ------------------------------------------------------------
        object CreateEditCopy(SerializedProperty property);
    }

    // ============================================================
    /// <summary>
    /// 열에 맞는 셀 drawer를 고르는 레지스트리.
    /// </summary>
    // ============================================================
    internal static class CollectionCellDrawers
    {

    #region 등록

        private static readonly ICollectionCellDrawer fallback = new CollectionDefaultCellDrawer();
        private static readonly Lazy<IReadOnlyList<ICollectionCellDrawer>> registered = new(LoadDrawers);

        private static IReadOnlyList<ICollectionCellDrawer> LoadDrawers()
        {
            return TypeCache.GetTypesDerivedFrom<ICollectionCellDrawer>()
                .Where(type => !type.IsAbstract && !type.IsInterface && type != typeof(CollectionDefaultCellDrawer))
                .OrderBy(type => type.FullName, StringComparer.Ordinal)
                .Select(type => (ICollectionCellDrawer)Activator.CreateInstance(type, nonPublic: true))
                .ToArray();
        }

    #endregion

    #region 조회

        // ------------------------------------------------------------
        /// <summary>
        /// 열 지정, 등록, 기본 순서로 열의 drawer를 고른다.
        /// </summary>
        // ------------------------------------------------------------
        public static ICollectionCellDrawer Resolve(CollectionColumn column)
        {
            if (column.Drawer != null)
            {
                return column.Drawer;
            }

            foreach (var drawer in registered.Value)
            {
                if (drawer.Matches(column))
                {
                    return drawer;
                }
            }

            return fallback;
        }

    #endregion

    }
}
