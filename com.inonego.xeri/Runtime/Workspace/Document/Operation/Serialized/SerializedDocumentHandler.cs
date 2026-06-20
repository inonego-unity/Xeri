/* BLOCK_HEADER_BEGIN ==========================================================================================
파일명 : SerializedDocumentHandler.cs
수정일 : 2026-06-22

# 설명
문서 location을 IO location으로 변환한 뒤 문자열 serializer로 모델을 열고 저장하는 handler 기반 클래스를 정의한다.
Document location과 IO location을 분리해 파일, address, reference 등 서로 다른 입력원에 확장할 수 있게 한다.
============================================================================================ BLOCK_HEADER_END */

using System;

using inonego.Xeri.IO;
using inonego.Xeri.Serializable;

namespace inonego.Xeri.Workspace.Document
{
   // ================================================================================================
   /// <summary>
   /// <br/> 문자열 serializer와 reader/writer를 조합해 문서 모델을 열고 저장하는 handler 기반 클래스.
   /// <br/> TModel 자체를 직렬화하므로 concrete model은 주입된 serializer가 처리할 수 있는 구조여야 한다.
   /// </summary>
   /// <typeparam name="TModel">handler가 생성하고 저장하는 문서 모델 타입.</typeparam>
   /// <typeparam name="TLocation">reader/writer가 실제로 사용하는 IO location 타입.</typeparam>
   // ================================================================================================
   [Serializable]
   public abstract class SerializedDocumentHandler<TModel, TLocation> : IDocumentHandler
   where TModel : IDocumentModel
   {

   #region 필드

      // ------------------------------------------------------------
      /// <summary>
      /// Handler가 담당하는 문서 종류 식별자.
      /// </summary>
      // ------------------------------------------------------------
      public string TypeID => _TypeID;

      private readonly string _TypeID = string.Empty;

      // ------------------------------------------------------------
      /// <summary>
      /// Handler가 생성하는 문서 버전.
      /// </summary>
      // ------------------------------------------------------------
      public string Version => version;

      private readonly string version = string.Empty;

      // ------------------------------------------------------------
      /// <summary>
      /// 모델과 문자열 사이를 변환하는 serializer.
      /// </summary>
      // ------------------------------------------------------------
      protected ISerializer Serializer => serializer;

      private readonly ISerializer serializer = null;

      // ------------------------------------------------------------
      /// <summary>
      /// IO location에서 serialized text를 읽는 reader.
      /// </summary>
      // ------------------------------------------------------------
      protected IDataReader<TLocation, string> Reader => reader;

      private readonly IDataReader<TLocation, string> reader = null;

      // ------------------------------------------------------------
      /// <summary>
      /// IO location에 serialized text를 쓰는 writer.
      /// </summary>
      // ------------------------------------------------------------
      protected IDataWriter<TLocation, string> Writer => writer;

      private readonly IDataWriter<TLocation, string> writer = null;

   #endregion

   #region 생성자

      // --------------------------------------------------------------------------------
      /// <summary>
      /// <br/> SerializedDocumentHandler를 생성한다.
      /// <br/> serializer는 TModel 인스턴스 자체를 serialize/deserialize 할 수 있어야 한다.
      /// </summary>
      // --------------------------------------------------------------------------------
      protected SerializedDocumentHandler
      (
         string _TypeID,
         string version,
         ISerializer serializer,
         IDataReader<TLocation, string> reader,
         IDataWriter<TLocation, string> writer
      ) : base()
      {
         if (string.IsNullOrEmpty(_TypeID))
         {
            throw new ArgumentException("문서 종류 식별자가 비어 있습니다.", nameof(_TypeID));
         }

         if (string.IsNullOrEmpty(version))
         {
            throw new ArgumentException("문서 버전이 비어 있습니다.", nameof(version));
         }

         this._TypeID = _TypeID;
         this.version = version;
         this.serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
         this.reader = reader ?? throw new ArgumentNullException(nameof(reader));
         this.writer = writer ?? throw new ArgumentNullException(nameof(writer));
      }

   #endregion

   #region 생성

      // ------------------------------------------------------------
      /// <summary>
      /// 지정한 이름으로 새 문서 세션을 생성한다.
      /// </summary>
      // ------------------------------------------------------------
      public DocumentCreateResponse Create(string name)
      {
         if (string.IsNullOrEmpty(name))
         {
            return DocumentCreateResponse.Fail("문서 이름이 비어 있습니다.");
         }

         try
         {
            var model = CreateModel(name);
            if (model == null)
            {
               return DocumentCreateResponse.Fail("생성된 문서 모델이 없습니다.");
            }

            var document = CreateDocument(name);
            var session = new DocumentSession(document, model, null);

            return DocumentCreateResponse.Succeed(session);
         }
         catch (Exception exception)
         {
            return DocumentCreateResponse.Fail(exception.Message);
         }
      }

      // ------------------------------------------------------------
      /// <summary>
      /// 새 문서 생성 시 사용할 기본 모델을 생성한다.
      /// </summary>
      // ------------------------------------------------------------
      protected abstract TModel CreateModel(string name);

   #endregion

   #region 열기

      // ------------------------------------------------------------
      /// <summary>
      /// 지정한 location을 열 수 있는지 확인한다.
      /// </summary>
      // ------------------------------------------------------------
      public bool CanOpen(IDocumentLocation loc)
      {
         if (loc == null)
         {
            return false;
         }

         return TryMapIOLocation(loc, out _);
      }

      // ------------------------------------------------------------
      /// <summary>
      /// 지정한 location을 문서 세션으로 연다.
      /// </summary>
      // ------------------------------------------------------------
      public DocumentOpenResponse Open(IDocumentLocation loc)
      {
         if (loc == null)
         {
            return DocumentOpenResponse.Fail("열 location이 없습니다.");
         }

         if (!TryMapIOLocation(loc, out var ioLoc))
         {
            return DocumentOpenResponse.Fail("location을 IO location으로 변환할 수 없습니다.");
         }

         IReleaseHandle releaseHandle = null;

         try
         {
            var readResponse = reader.Read(ioLoc);
            releaseHandle = readResponse.ReleaseHandle;

            if (!readResponse.Success)
            {
               return DocumentOpenResponse.Fail(readResponse.Error);
            }

            var model = serializer.Deserialize<TModel>(readResponse.Value);
            if (model == null)
            {
               return DocumentOpenResponse.Fail("역직렬화된 문서 모델이 없습니다.");
            }

            var document = CreateDocument(model, loc);
            var session = new DocumentSession(document, model, loc);

            return DocumentOpenResponse.Succeed(session);
         }
         catch (Exception exception)
         {
            return DocumentOpenResponse.Fail(exception.Message);
         }
         finally
         {
            // Serialized handler는 문자열을 모델로 변환한 뒤 원본 IO 수명을 보관하지 않는다.
            releaseHandle?.Release();
         }
      }

   #endregion

   #region 저장

      // ------------------------------------------------------------
      /// <summary>
      /// 지정한 문서 세션을 location에 저장할 수 있는지 확인한다.
      /// </summary>
      // ------------------------------------------------------------
      public bool CanSave(IDocumentSession session, IDocumentLocation loc)
      {
         if (session == null || loc == null)
         {
            return false;
         }

         if (session.Document == null || session.Document.TypeID != TypeID)
         {
            return false;
         }

         if (session.Model is not TModel)
         {
            return false;
         }

         return TryMapIOLocation(loc, out _);
      }

      // ------------------------------------------------------------
      /// <summary>
      /// 지정한 문서 세션을 location에 저장한다.
      /// </summary>
      // ------------------------------------------------------------
      public DocumentSaveResponse Save(IDocumentSession session, IDocumentLocation loc)
      {
         if (!CanSave(session, loc))
         {
            return DocumentSaveResponse.Fail("session을 지정 location에 저장할 수 없습니다.");
         }

         if (!TryMapIOLocation(loc, out var ioLoc))
         {
            return DocumentSaveResponse.Fail("location을 IO location으로 변환할 수 없습니다.");
         }

         try
         {
            var model = (TModel)session.Model;
            var text = serializer.Serialize(model);
            var writeResponse = writer.Write(ioLoc, text);

            if (!writeResponse.Success)
            {
               return DocumentSaveResponse.Fail(writeResponse.Error);
            }

            return DocumentSaveResponse.Succeed();
         }
         catch (Exception exception)
         {
            return DocumentSaveResponse.Fail(exception.Message);
         }
      }

   #endregion

   #region Location 변환

      // ------------------------------------------------------------
      /// <summary>
      /// Document location을 reader/writer가 사용하는 IO location으로 변환한다.
      /// </summary>
      // ------------------------------------------------------------
      protected abstract bool TryMapIOLocation(IDocumentLocation loc, out TLocation ioLoc);

   #endregion

   #region Document 생성

      // ------------------------------------------------------------
      /// <summary>
      /// 새 문서 생성 시 사용할 document 설명 정보를 생성한다.
      /// </summary>
      // ------------------------------------------------------------
      protected virtual IDocument CreateDocument(string name)
      {
         return new Document(TypeID, Version, name);
      }

      // ------------------------------------------------------------
      /// <summary>
      /// 열린 모델과 location을 기준으로 document 설명 정보를 생성한다.
      /// </summary>
      // ------------------------------------------------------------
      protected virtual IDocument CreateDocument(TModel model, IDocumentLocation loc)
      {
         var name = string.IsNullOrEmpty(loc?.Name) ? TypeID : loc.Name;

         return new Document(TypeID, Version, name);
      }

   #endregion

   }
}
