/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : XeriXmlSerializer.cs
수정일 : 2026-06-28

# 설명
System.Xml.Serialization.XmlSerializer 기반 ISerializer 구현.
타입별 XmlSerializer 생성 비용을 줄이기 위해 제네릭 static 캐시를 사용한다.
출력 formatting은 serializer 인스턴스의 prettyPrint 설정으로 결정한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace inonego.Xeri.Serializable
{
   // ============================================================
   /// <summary>
   /// System XmlSerializer 기반 XML serializer.
   /// </summary>
   // ============================================================
   [Serializable]
   public sealed class XeriXmlSerializer : ISerializer
   {

   #region 내부 데이터

      // ============================================================
      /// <summary>
      /// T별 XmlSerializer 캐시.
      /// </summary>
      // ============================================================
      private static class XmlSerializerCache<T>
      {
         // ------------------------------------------------------------
         /// <summary>
         /// T 타입용 기본 XmlSerializer 인스턴스.
         /// </summary>
         // ------------------------------------------------------------
         public static readonly XmlSerializer Default = new(typeof(T));
      }

   #endregion

   #region 필드

      // ------------------------------------------------------------
      /// <summary>
      /// 기본 XML serializer 인스턴스.
      /// </summary>
      // ------------------------------------------------------------
      public static XeriXmlSerializer Default { get; } = new();

      // ------------------------------------------------------------
      /// <summary>
      /// 들여쓰기 XML serializer 인스턴스.
      /// </summary>
      // ------------------------------------------------------------
      public static XeriXmlSerializer Pretty { get; } = new(true);

      // ------------------------------------------------------------
      /// <summary>
      /// XML 출력에 들여쓰기를 적용할지 여부.
      /// </summary>
      // ------------------------------------------------------------
      public bool PrettyPrint => prettyPrint;

      private readonly bool prettyPrint = false;

   #endregion

   #region 생성자

      // ------------------------------------------------------------
      /// <summary>
      /// XML serializer를 생성한다.
      /// </summary>
      // ------------------------------------------------------------
      public XeriXmlSerializer(bool prettyPrint = false) : base()
      {
         this.prettyPrint = prettyPrint;
      }

   #endregion

   #region 메서드

      // ------------------------------------------------------------
      /// <summary>
      /// 객체를 XML 문자열로 직렬화한다.
      /// </summary>
      // ------------------------------------------------------------
      public string Serialize<T>(T value)
      {
         using var writer = new StringWriter();

         var settings = new XmlWriterSettings
         {
            Indent = prettyPrint,
            IndentChars = "   ",
            NewLineChars = "\n",
            NewLineHandling = NewLineHandling.Replace,
            OmitXmlDeclaration = true,
         };

         using var xmlWriter = XmlWriter.Create(writer, settings);

         XmlSerializerCache<T>.Default.Serialize(xmlWriter, value);

         return writer.ToString();
      }

      // ------------------------------------------------------------
      /// <summary>
      /// XML 문자열에서 객체를 역직렬화한다.
      /// </summary>
      // ------------------------------------------------------------
      public T Deserialize<T>(string text)
      {
         using var reader = new StringReader(text);

         return (T)XmlSerializerCache<T>.Default.Deserialize(reader);
      }

   #endregion

   }
}
