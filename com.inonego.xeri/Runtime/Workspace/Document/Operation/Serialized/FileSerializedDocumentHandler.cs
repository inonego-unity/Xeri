/* BLOCK_HEADER_BEGIN =============================================================================================================
파일명 : FileSerializedDocumentHandler.cs
수정일 : 2026-06-22

# 설명
FileDocumentLocation을 문자열 파일 경로로 변환해 TextFileIO 기반 serialized document open/save를 수행하는 handler 기반 클래스를 정의한다.
구체 문서 타입은 CreateModel을 구현해 새 문서 생성 시 사용할 기본 모델을 제공한다.
=============================================================================================================== BLOCK_HEADER_END */

using System;

using inonego.Xeri.IO;
using inonego.Xeri.Serializable;

namespace inonego.Xeri.Workspace.Document
{
   // =======================================================================
   /// <summary>
   /// 파일 경로 기반 serialized document handler 기반 클래스.
   /// </summary>
   /// <typeparam name="TModel">handler가 생성하고 저장하는 문서 모델 타입.</typeparam>
   // =======================================================================
   [Serializable]
   public abstract class FileSerializedDocumentHandler<TModel> : SerializedDocumentHandler<TModel, string>
   where TModel : IDocumentModel
   {

   #region 생성자

      // ------------------------------------------------------------
      /// <summary>
      /// 기본 TextFileIO를 사용하는 FileSerializedDocumentHandler를 생성한다.
      /// </summary>
      // ------------------------------------------------------------
      protected FileSerializedDocumentHandler
      (
         string _TypeID,
         string version,
         ISerializer serializer
      ) : this(_TypeID, version, serializer, TextFileIO.Default, TextFileIO.Default)
      {

      }

      // ------------------------------------------------------------
      /// <summary>
      /// 지정한 reader와 writer를 사용하는 FileSerializedDocumentHandler를 생성한다.
      /// </summary>
      // ------------------------------------------------------------
      protected FileSerializedDocumentHandler
      (
         string _TypeID,
         string version,
         ISerializer serializer,
         IDataReader<string, string> reader,
         IDataWriter<string, string> writer
      ) : base(_TypeID, version, serializer, reader, writer)
      {

      }

   #endregion

   #region Location 변환

      // ------------------------------------------------------------
      /// <summary>
      /// FileDocumentLocation을 파일 경로 문자열로 변환한다.
      /// </summary>
      // ------------------------------------------------------------
      protected override bool TryMapIOLocation(IDocumentLocation loc, out string ioLoc)
      {
         if (loc is FileDocumentLocation _loc)
         {
            ioLoc = _loc.Path;
            return true;
         }

         ioLoc = null;
         return false;
      }

   #endregion

   }
}
