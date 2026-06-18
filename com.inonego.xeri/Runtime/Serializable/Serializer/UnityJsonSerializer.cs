/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : UnityJsonSerializer.cs
수정일 : 2026-06-12

# 설명
UnityEngine.JsonUtility 기반 ISerializer 구현.
Unity 직렬화 규칙을 그대로 따르는 기본 JSON serializer를 제공한다.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEngine;

namespace inonego.Xeri.Serializable
{
   // ============================================================
   /// <summary>
   /// Unity JsonUtility 기반 JSON serializer.
   /// </summary>
   // ============================================================
   [Serializable]
   public sealed class UnityJsonSerializer : ISerializer
   {

   #region 필드

      // ------------------------------------------------------------
      /// <summary>
      /// 기본 JSON serializer 인스턴스.
      /// </summary>
      // ------------------------------------------------------------
      public static UnityJsonSerializer Default { get; } = new();

   #endregion

   #region 생성자

      // ------------------------------------------------------------
      /// <summary>
      /// 기본 JSON serializer를 생성한다.
      /// </summary>
      // ------------------------------------------------------------
      private UnityJsonSerializer() : base() {}

   #endregion

   #region 메서드

      // ------------------------------------------------------------
      /// <summary>
      /// 객체를 Unity JSON 문자열로 직렬화한다.
      /// </summary>
      // ------------------------------------------------------------
      public string Serialize<T>(T value)
      {
         return JsonUtility.ToJson(value);
      }

      // ------------------------------------------------------------
      /// <summary>
      /// Unity JSON 문자열에서 객체를 역직렬화한다.
      /// </summary>
      // ------------------------------------------------------------
      public T Deserialize<T>(string text)
      {
         return JsonUtility.FromJson<T>(text);
      }

   #endregion

   }
}
