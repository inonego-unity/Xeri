/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : PickerPreviewModel.cs
수정일 : 2026-06-06

# 설명
Picker preview 영역이 공통으로 표시할 이미지, 이름, 보조 라벨, 설명, 태그 모델.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace inonego.Xeri.UI.Picker
{
   // ============================================================
   /// <summary>
   /// Picker preview 영역이 공통으로 표시할 데이터 모델.
   /// </summary>
   // ============================================================
   [Serializable]
   public sealed class PickerPreviewModel
   {

   #region 필드

      // ------------------------------------------------------------
      /// <summary>
      /// preview 이미지.
      /// </summary>
      // ------------------------------------------------------------
      public readonly Texture2D Image;

      // ------------------------------------------------------------
      /// <summary>
      /// preview 대표 이름.
      /// </summary>
      // ------------------------------------------------------------
      public readonly string Name;

      // ------------------------------------------------------------
      /// <summary>
      /// 이름 아래에 표시할 보조 라벨.
      /// </summary>
      // ------------------------------------------------------------
      public readonly string SubLabel;

      // ------------------------------------------------------------
      /// <summary>
      /// preview 설명.
      /// </summary>
      // ------------------------------------------------------------
      public readonly string Desc;

      // ------------------------------------------------------------
      /// <summary>
      /// preview 하단 태그 목록.
      /// </summary>
      // ------------------------------------------------------------
      public readonly IReadOnlyList<PickerTag> Tags;

   #endregion

   #region 생성자

      // ------------------------------------------------------------
      /// <summary>
      /// preview 표시 모델을 생성한다.
      /// </summary>
      // ------------------------------------------------------------
      public PickerPreviewModel
      (
         Texture2D image,
         string name,
         string subLabel,
         string desc,
         IReadOnlyList<PickerTag> tags
      ) : base()
      {
         Image    = image;
         Name     = name ?? string.Empty;
         SubLabel = subLabel ?? string.Empty;
         Desc     = desc ?? string.Empty;
         Tags     = tags ?? Array.Empty<PickerTag>();
      }

   #endregion

   }
}
