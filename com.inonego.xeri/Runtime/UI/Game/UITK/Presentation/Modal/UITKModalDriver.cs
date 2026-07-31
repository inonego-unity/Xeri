/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : UITKModalDriver.cs
수정일 : 2026-07-31

# 설명
Modal Stack 상단 여부를 UI Toolkit Root 상호작용과 선택적 Dim Element에 적용한다.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEngine.UIElements;

namespace inonego.Xeri.UI.Game
{
    // ============================================================
    /// <summary>
    /// UI Toolkit Modal 상단 상태 backend.
    /// </summary>
    // ============================================================
    public sealed class UITKModalDriver : IModalDriver
    {
    #region 필드

        private readonly VisualElement root = null;
        private readonly VisualElement dim = null;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// Modal Root와 선택적 Dim Element를 연결한다.
        /// </summary>
        // ------------------------------------------------------------
        public UITKModalDriver
        (
            VisualElement root,
            VisualElement dim = null
        ) : base()
        {
            this.root = root ?? throw new ArgumentNullException(nameof(root));
            this.dim = dim;
        }

    #endregion

    #region IModalDriver

        // ------------------------------------------------------------
        /// <summary>
        /// Stack 상단 Modal만 enabled와 picking을 받도록 적용한다.
        /// </summary>
        // ------------------------------------------------------------
        public void SetTop(bool isTop)
        {
            root.SetEnabled(isTop);
            root.pickingMode = isTop ? PickingMode.Position : PickingMode.Ignore;

            if (dim != null)
            {
                dim.style.display = isTop ? DisplayStyle.Flex : DisplayStyle.None;
                dim.pickingMode = isTop ? PickingMode.Position : PickingMode.Ignore;
            }
        }

    #endregion

    }
}
