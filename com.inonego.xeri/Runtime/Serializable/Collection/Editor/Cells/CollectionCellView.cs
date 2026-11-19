/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : CollectionCellView.cs
수정일 : 2026-11-19

# 설명
값 형식별로 셀 요약 표시를 직접 그리는 공개 계약과 그 등록·조회를 둔다.
ListView의 makeItem/bindItem처럼 셀 UI를 한 번 만들고 행이 바뀔 때마다 다시 연결한다.

# 특이사항, 제약사항
셀 뷰는 요약 문자열 자리만 대신한다. 단순 값의 인라인 입력 필드와 상세·초안 편집 버튼은 그대로 쓴다.
CreateRow·CreateText는 표 스타일의 행·텍스트 클래스를 붙여 기본 셀과 같은 높이·글자 위치를 보장한다.
등록은 [CustomCollectionCell]과 TypeCache로 한 번만 모으고, 형식별 조회 결과(없음 포함)를 캐시한다.
같은 형식에 여러 뷰가 있으면 뷰 클래스 FullName 순으로 먼저 오는 것을 쓴다.
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
    /// <br/> 값 형식 하나의 셀 요약 표시를 직접 그리는 뷰.
    /// <br/> [CustomCollectionCell]로 대상 형식을 지정한다.
    /// <br/> 모든 열이 같은 인스턴스를 공유하므로 상태를 두지 않는다.
    /// </summary>
    // ============================================================
    public abstract class CollectionCellView
    {

    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// 셀 높이에 맞춰 요소를 가로로 늘어놓는 행의 USS 클래스.
        /// </summary>
        // ------------------------------------------------------------
        public const string RowClassName = "xeri-collection__cell-row";

        // ------------------------------------------------------------
        /// <summary>
        /// 기본 요약 글자와 같은 글자 크기·세로 위치를 쓰는 텍스트의 USS 클래스.
        /// </summary>
        // ------------------------------------------------------------
        public const string TextClassName = "xeri-collection__cell-text";

    #endregion

    #region 셀 UI

        // ------------------------------------------------------------
        /// <summary>
        /// 행이 바뀌어도 재사용할 셀 UI를 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public abstract VisualElement MakeCell();

        // ------------------------------------------------------------
        /// <summary>
        /// 셀 UI에 현재 행의 값을 표시한다.
        /// </summary>
        // ------------------------------------------------------------
        public abstract void BindCell(VisualElement element, SerializedProperty property);

        // ------------------------------------------------------------
        /// <summary>
        /// 셀 UI를 다른 행에서 재사용하기 전에 연결을 끊는다.
        /// </summary>
        // ------------------------------------------------------------
        public virtual void UnbindCell(VisualElement element)
        {
            // NONE
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 셀 높이에 맞춘 가로 행을 만든다.
        /// </summary>
        // ------------------------------------------------------------
        protected static VisualElement CreateRow()
        {
            var row = new VisualElement();
            row.AddToClassList(RowClassName);
            return row;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 기본 요약 글자와 같은 위치에 놓이는 텍스트를 만든다.
        /// </summary>
        // ------------------------------------------------------------
        protected static Label CreateText(string name = null)
        {
            var label = new Label();
            label.name = name;
            label.AddToClassList(TextClassName);
            return label;
        }

    #endregion

    #region 검색

        // ------------------------------------------------------------
        /// <summary>
        /// 행 검색에 쓸 문자열을 반환하며 null이면 기본 요약을 쓴다.
        /// </summary>
        // ------------------------------------------------------------
        public virtual string GetSearchText(SerializedProperty property)
        {
            return null;
        }

    #endregion

    }

    // ============================================================
    /// <summary>
    /// 셀 뷰가 그릴 값 형식을 지정한다.
    /// </summary>
    // ============================================================
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class CustomCollectionCellAttribute : Attribute
    {

    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// 셀 뷰가 그릴 값 형식을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public Type Type { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 파생 형식과 구현 형식에도 적용할지 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public bool UseForChildren { get; set; }

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// 셀 뷰가 그릴 값 형식을 지정한다.
        /// </summary>
        // ------------------------------------------------------------
        public CustomCollectionCellAttribute(Type type)
        {
            Type = type ?? throw new ArgumentNullException(nameof(type));
        }

    #endregion

    }

    // ============================================================
    /// <summary>
    /// 값 형식에 맞는 셀 뷰를 찾는 레지스트리.
    /// </summary>
    // ============================================================
    internal static class CollectionCellViews
    {

    #region 등록

        private static readonly Lazy<IReadOnlyList<(Type Type, bool UseForChildren, CollectionCellView View)>> registrations = new(LoadRegistrations);
        private static readonly Dictionary<Type, CollectionCellView> cache = new();

        private static IReadOnlyList<(Type Type, bool UseForChildren, CollectionCellView View)> LoadRegistrations()
        {
            var result = new List<(Type Type, bool UseForChildren, CollectionCellView View)>();
            var viewTypes = TypeCache.GetTypesWithAttribute<CustomCollectionCellAttribute>()
                .Where(type => typeof(CollectionCellView).IsAssignableFrom(type) && !type.IsAbstract)
                .OrderBy(type => type.FullName, StringComparer.Ordinal);
            foreach (var viewType in viewTypes)
            {
                var view = (CollectionCellView)Activator.CreateInstance(viewType, nonPublic: true);
                foreach (var attribute in viewType.GetCustomAttributes(typeof(CustomCollectionCellAttribute), inherit: false))
                {
                    var target = (CustomCollectionCellAttribute)attribute;
                    result.Add((target.Type, target.UseForChildren, view));
                }
            }

            return result;
        }

    #endregion

    #region 조회

        // ------------------------------------------------------------
        /// <summary>
        /// 셀의 실제 값 형식에 맞는 뷰를 반환하며 없으면 null이다.
        /// </summary>
        // ------------------------------------------------------------
        public static CollectionCellView Find(SerializedProperty property, CollectionColumn column)
        {
            if (property == null)
            {
                return null;
            }

            // SerializeReference는 행마다 실제 형식이 다르므로 선언 형식 대신 현재 값 형식을 쓴다.
            var valueType = property.propertyType == SerializedPropertyType.ManagedReference
                ? property.managedReferenceValue?.GetType()
                : column.ValueType;
            return Find(valueType);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 값 형식에 맞는 뷰를 반환하며 없으면 null이다.
        /// </summary>
        // ------------------------------------------------------------
        public static CollectionCellView Find(Type valueType)
        {
            if (valueType == null || registrations.Value.Count == 0)
            {
                return null;
            }

            if (!cache.TryGetValue(valueType, out var view))
            {
                view = Match(valueType);
                cache.Add(valueType, view);
            }

            return view;
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 정확히 같은 형식을 먼저, 그다음 가까운 기반 형식, 마지막으로 인터페이스를 찾는다.
        /// </summary>
        // ----------------------------------------------------------------------
        private static CollectionCellView Match(Type valueType)
        {
            foreach (var registration in registrations.Value)
            {
                if (registration.Type == valueType)
                {
                    return registration.View;
                }
            }

            for (var baseType = valueType.BaseType; baseType != null; baseType = baseType.BaseType)
            {
                var view = FindForChildren(baseType);
                if (view != null)
                {
                    return view;
                }
            }

            foreach (var interfaceType in valueType.GetInterfaces())
            {
                var view = FindForChildren(interfaceType);
                if (view != null)
                {
                    return view;
                }
            }

            return null;
        }

        private static CollectionCellView FindForChildren(Type type)
        {
            foreach (var registration in registrations.Value)
            {
                if (registration.UseForChildren && registration.Type == type)
                {
                    return registration.View;
                }
            }

            return null;
        }

    #endregion

    }

    // ======================================================================
    /// <summary>
    /// drawer의 셀 UI 안에서 셀 뷰의 UI를 뷰마다 하나씩 만들어 두고 바꿔 보이는 자리.
    /// </summary>
    // ======================================================================
    internal sealed class CollectionCellContent : VisualElement
    {

    #region 필드

        private readonly Dictionary<CollectionCellView, VisualElement> elements = new();
        private CollectionCellView current;
        private VisualElement currentElement;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// 빈 셀 뷰 자리를 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public CollectionCellContent()
        {
            AddToClassList("xeri-collection__custom-cell");
            style.display = DisplayStyle.None;
        }

    #endregion

    #region 표시

        // ------------------------------------------------------------
        /// <summary>
        /// 셀 뷰의 UI를 보이고 현재 값을 연결한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Show(CollectionCellView view, SerializedProperty property)
        {
            Release();
            if (!elements.TryGetValue(view, out var element))
            {
                element = view.MakeCell();
                elements.Add(view, element);
                Add(element);
            }

            foreach (var child in Children())
            {
                child.style.display = child == element ? DisplayStyle.Flex : DisplayStyle.None;
            }

            style.display = DisplayStyle.Flex;
            current = view;
            currentElement = element;
            view.BindCell(element, property);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 연결된 셀 뷰를 끊고 자리를 숨긴다.
        /// </summary>
        // ------------------------------------------------------------
        public void Release()
        {
            if (current != null)
            {
                current.UnbindCell(currentElement);
                current = null;
                currentElement = null;
            }

            style.display = DisplayStyle.None;
        }

    #endregion

    }
}
