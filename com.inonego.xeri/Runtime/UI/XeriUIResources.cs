/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : XeriUIResources.cs
수정일 : 2026-11-23

# 설명
Xeri UI 요소가 Resources에서 꼭 있어야 하는 UXML·USS를 불러오고, UXML 안의 필수 요소를 찾는 공용 입구.
없으면 주체와 경로(또는 요소 이름)를 담아 바로 예외를 던져, 시트 없이 모양이 조용히 무너지지 않게 한다.

# 특이사항, 제약사항
불러온 자산은 (형식, 경로)별로 보관해 요소마다 다시 조회하지 않는다. 같은 경로의 UXML·USS도 섞이지 않는다.
재가져오기로 자산이 해제되면(Unity null) 그때 다시 불러온다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UIElements;

using Object = UnityEngine.Object;

namespace inonego.Xeri.UI
{
    // ============================================================
    /// <summary>
    /// 필수 UI 리소스 로드.
    /// </summary>
    // ============================================================
    internal static class XeriUIResources
    {

    #region 필드

        private static readonly Dictionary<(Type, string), Object> loaded = new();

    #endregion

    #region 로드

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> Resources 경로(path)의 자산을 불러온다. 한 번 불러온 자산은 보관해 다시 쓴다.
        /// <br/> 없으면 subject(예: "TableGrid")와 경로를 담아 예외를 던진다.
        /// </summary>
        // --------------------------------------------------------------------------------
        public static T Require<T>(string path, string subject) where T : Object
        {
            var key = (typeof(T), path);
            if (loaded.TryGetValue(key, out var cached) && cached is T cachedAsset && cachedAsset != null)
            {
                return cachedAsset;
            }

            var asset = Resources.Load<T>(path);
            if (asset == null)
            {
                throw new InvalidOperationException($"{subject} 리소스를 찾을 수 없습니다: Resources/{path}");
            }

            loaded[key] = asset;
            return asset;
        }

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> UXML로 만든 root 아래에서 이름이 elementName인 요소를 찾는다.
        /// <br/> 없으면 subject와 요소 이름을 담아 예외를 던져, UXML과 코드가 어긋난 것을 바로 알게 한다.
        /// </summary>
        // --------------------------------------------------------------------------------
        public static T Require<T>(VisualElement root, string elementName, string subject) where T : VisualElement
        {
            return root.Q<T>(elementName)
                ?? throw new InvalidOperationException($"{subject} UXML에 '{elementName}' 요소가 없습니다.");
        }

    #endregion

    }
}
