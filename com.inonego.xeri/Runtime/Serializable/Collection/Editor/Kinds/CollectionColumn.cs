/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : CollectionColumn.cs
수정일 : 2026-11-23

# 설명
컬렉션 표의 열 하나를 정의한다.
셀 경로, 값 형식, 편집 방식, 행 추가 전 값 생성 여부와 열 지정 drawer를 함께 가진다.

# 특이사항, 제약사항
열 정의는 SerializedObject와 무관한 불변 데이터이며 같은 컬렉션의 다른 SerializedObject에서도 재사용한다.
========================================================================= BLOCK_HEADER_END */

using System;

using inonego;
using inonego.Xeri;
using inonego.Xeri.UI;
using inonego.Xeri.UI.TableGrid;

namespace inonego.Xeri.Serializable.Editor
{
    // ============================================================
    /// <summary>
    /// 셀 값을 고치는 방식.
    /// </summary>
    // ============================================================
    internal enum CollectionCellEdit
    {
        Inline,
        Delayed,
        Draft,
        ReadOnly,
    }

    // ============================================================
    /// <summary>
    /// 컬렉션 표의 열 정의.
    /// </summary>
    // ============================================================
    internal sealed class CollectionColumn
    {

    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// 열 식별자를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public string ID { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 열 헤더에 표시할 이름을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public string Header { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 열의 폭 정책을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public TableGridColumnLayout Layout { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 행 기준 셀 경로를 반환하며 null이면 행 자체가 셀이다.
        /// <br/> 한 종류 안에서 null 열과 경로가 있는 열을 섞지 않는다(null 열이 하위 경로를 모두 가져가
        /// <br/> CollectionSource.TryLocate가 뒤의 열을 찾지 못한다).
        /// </summary>
        // ------------------------------------------------------------
        public string RelativePath { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 셀의 선언 형식을 반환하며 알 수 없으면 null이다.
        /// </summary>
        // ------------------------------------------------------------
        public Type ValueType { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 셀이 SerializeReference인지 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public bool IsManagedReference { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 셀 값을 고치는 방식을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public CollectionCellEdit Edit { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 행을 추가하기 전에 값을 만들어야 하는지 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public bool CreatesValueOnAdd { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 열에 지정한 drawer를 반환하며 없으면 null이다.
        /// </summary>
        // ------------------------------------------------------------
        public ICollectionCellDrawer Drawer { get; }

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// 열 정의를 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public CollectionColumn
        (
            string id,
            string header,
            TableGridColumnLayout layout,
            string relativePath,
            Type valueType,
            bool isManagedReference,
            CollectionCellEdit edit = CollectionCellEdit.Inline,
            bool createsValueOnAdd = false,
            ICollectionCellDrawer drawer = null
        )
        {
            ID = id ?? throw new ArgumentNullException(nameof(id));
            Header = header ?? string.Empty;
            Layout = layout ?? throw new ArgumentNullException(nameof(layout));
            RelativePath = string.IsNullOrEmpty(relativePath) ? null : relativePath;
            ValueType = valueType;
            IsManagedReference = isManagedReference;
            Edit = edit;
            CreatesValueOnAdd = createsValueOnAdd;
            Drawer = drawer;
        }

    #endregion

    }
}
