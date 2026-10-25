/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : XeriIcon.cs
수정일 : 2026-10-26

# 설명
Xeri 패키지에서 공유하는 Lucide 아이콘을 이름으로 지연 로드하고 캐시한다.
아이콘의 호출 이름은 Lucide 원본 파일명에서 확장자를 제외한 값이다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UIElements;

namespace inonego.Xeri
{
    // ============================================================
    /// <summary>
    /// Xeri 공용 아이콘을 리소스에서 가져와 동일한 벡터 이미지를 재사용한다.
    /// </summary>
    // ============================================================
    public static class XeriIcon
    {

    #region 필드

        private const string resourcePath = "Xeri/Icons/Lucide/";
        private static readonly Dictionary<string, VectorImage> cache = new();

    #endregion

    #region 아이콘 로드

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/>Lucide 원본 이름으로 아이콘을 로드하고 캐시한다.
        /// <br/>리소스가 없으면 null을 반환하며, 이후 추가될 리소스는 다시 검색한다.
        /// </summary>
        // ----------------------------------------------------------------------
        public static VectorImage Load(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("아이콘 이름은 비어 있을 수 없습니다.", nameof(name));
            }

            if (cache.TryGetValue(name, out var icon) && icon != null)
            {
                return icon;
            }

            icon = Resources.Load<VectorImage>($"{resourcePath}{name}");
            
            if (icon != null)
            {
                cache[name] = icon;
            }

            return icon;
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/>UI Toolkit의 배경 이미지로 사용할 수 있도록 공용 아이콘을 반환한다.
        /// <br/>아이콘이 없으면 빈 배경을 반환한다.
        /// </summary>
        // ----------------------------------------------------------------------
        public static Background LoadBackground(string name)
        {
            var icon = Load(name);
            return icon == null ? default : Background.FromVectorImage(icon);
        }

    #endregion

    }
}
