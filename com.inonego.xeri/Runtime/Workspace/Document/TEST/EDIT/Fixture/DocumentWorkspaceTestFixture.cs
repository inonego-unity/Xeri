/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : DocumentWorkspaceTestFixture.cs
수정일 : 2026-06-23

# 설명
Workspace Document 테스트에서 공유하는 handler, model, payload, location helper를 정의한다.
Unity Test Runner (Edit Mode) 에서 실행되는 테스트 fixture 전용 코드다.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEngine;

using NUnit.Framework;

using inonego.Xeri.IO;
using inonego.Xeri.Serializable;
using inonego.Xeri.Workspace.Document;

namespace inonego.Xeri.TEST.Workspace._Document
{
   // ============================================================
   /// <summary>
   /// Workspace Document 테스트 공통 fixture.
   /// </summary>
   // ============================================================
   public abstract class DocumentWorkspaceTestFixture
   {

   #region 내부 데이터

      protected const string TypeID = "test.document";
      protected const string ForeignTypeID = "test.foreign";
      protected const string Version = "1.0.0";

      // ============================================================
      /// <summary>
      /// 테스트용 document handler.
      /// </summary>
      // ============================================================
      [Serializable]
      protected sealed class DocumentHandler : SerializedDocumentHandler<DocumentModel, MemoryLocation<string>>
      {

      #region 생성자

         // ------------------------------------------------------------
         /// <summary>
         /// 테스트용 document handler를 생성한다.
         /// </summary>
         // ------------------------------------------------------------
         public DocumentHandler() : base
         (
            DocumentWorkspaceTestFixture.TypeID,
            DocumentWorkspaceTestFixture.Version,
            UnityJsonSerializer.Default,
            MemoryIO<string>.Default,
            MemoryIO<string>.Default
         ) {}

      #endregion

      #region 생성

         // ------------------------------------------------------------
         /// <summary>
         /// 새 문서 생성 시 사용할 기본 모델을 생성한다.
         /// </summary>
         // ------------------------------------------------------------
         protected override DocumentModel CreateModel(string name)
         {
            return new DocumentModel(new Payload(name, 0));
         }

      #endregion

      #region Location 변환

         // ----------------------------------------------------------------------
         /// <summary>
         /// <br/> Workspace 문서 location에서 실제 문자열 저장소로 사용할 memory IO location을 꺼낸다.
         /// <br/> 테스트는 파일 시스템이 아니라 workspace document 흐름을 검증한다.
         /// </summary>
         // ----------------------------------------------------------------------
         protected override bool TryMapIOLocation(IDocumentLocation loc, out MemoryLocation<string> ioLoc)
         {
            if (loc is MemoryDocumentLocation memoryLoc && memoryLoc.Value is MemoryLocation<string> memoryIO)
            {
               ioLoc = memoryIO;
               return true;
            }

            ioLoc = null;
            return false;
         }

      #endregion

      }

      // ============================================================
      /// <summary>
      /// 테스트용 document model.
      /// </summary>
      // ============================================================
      [Serializable]
      protected sealed class DocumentModel : IDocumentModel<Payload>
      {

      #region 필드

         // ------------------------------------------------------------
         /// <summary>
         /// 테스트용 payload.
         /// </summary>
         // ------------------------------------------------------------
         public Payload Value => value;

         object IDocumentModel.Value => Value;

         [SerializeField]
         private Payload value = new Payload();

      #endregion

      #region 생성자

         // ------------------------------------------------------------
         /// <summary>
         /// 기본 document model을 생성한다.
         /// </summary>
         // ------------------------------------------------------------
         public DocumentModel() : base() {}

         // ------------------------------------------------------------
         /// <summary>
         /// 지정한 payload를 가진 document model을 생성한다.
         /// </summary>
         // ------------------------------------------------------------
         public DocumentModel(Payload value) : this()
         {
            this.value = value ?? throw new ArgumentNullException(nameof(value));
         }

      #endregion

      #region 메서드

         // ------------------------------------------------------------
         /// <summary>
         /// 테스트용 payload를 교체한다.
         /// </summary>
         // ------------------------------------------------------------
         public void SetValue(Payload value)
         {
            this.value = value ?? throw new ArgumentNullException(nameof(value));
         }

      #endregion

      }

      // ============================================================
      /// <summary>
      /// 테스트용 payload.
      /// </summary>
      // ============================================================
      [Serializable]
      protected sealed class Payload
      {

      #region 필드

         // ------------------------------------------------------------
         /// <summary>
         /// 저장 검증용 문자열 값.
         /// </summary>
         // ------------------------------------------------------------
         public string Text => text;

         [SerializeField]
         private string text = string.Empty;

         // ------------------------------------------------------------
         /// <summary>
         /// 저장 검증용 숫자 값.
         /// </summary>
         // ------------------------------------------------------------
         public int Count => count;

         [SerializeField]
         private int count = 0;

      #endregion

      #region 생성자

         // ------------------------------------------------------------
         /// <summary>
         /// 기본 payload를 생성한다.
         /// </summary>
         // ------------------------------------------------------------
         public Payload() : base() {}

         // ------------------------------------------------------------
         /// <summary>
         /// 지정한 값으로 payload를 생성한다.
         /// </summary>
         // ------------------------------------------------------------
         public Payload(string text, int count) : this()
         {
            this.text  = text ?? string.Empty;
            this.count = count;
         }

      #endregion

      }

      // ============================================================
      /// <summary>
      /// 테스트용 document handler.
      /// </summary>
      // ============================================================
      [Serializable]
      protected sealed class TEST_Handler : IDocumentHandler
      {

      #region 필드

         // ------------------------------------------------------------
         /// <summary>
         /// Handler가 담당하는 문서 종류 식별자.
         /// </summary>
         // ------------------------------------------------------------
         public string TypeID => typeID;

         private readonly string typeID = string.Empty;
         private readonly IDocumentSession createSession = null;
         private readonly IDocumentSession openSession = null;

      #endregion

      #region 생성자

         // ------------------------------------------------------------
         /// <summary>
         /// 테스트용 document handler를 생성한다.
         /// </summary>
         // ------------------------------------------------------------
         public TEST_Handler
         (
            string typeID,
            IDocumentSession createSession = null,
            IDocumentSession openSession = null
         ) : base()
         {
            this.typeID        = typeID ?? string.Empty;
            this.createSession = createSession;
            this.openSession   = openSession;
         }

      #endregion

      #region 생성

         // ------------------------------------------------------------
         /// <summary>
         /// 설정된 생성 session을 반환한다.
         /// </summary>
         // ------------------------------------------------------------
         public DocumentCreateResponse Create(string name)
         {
            return createSession == null
               ? DocumentCreateResponse.Fail("생성 session이 없습니다.")
               : DocumentCreateResponse.Succeed(createSession);
         }

      #endregion

      #region 열기

         // ------------------------------------------------------------
         /// <summary>
         /// 설정된 열기 session이 있으면 열 수 있다고 판단한다.
         /// </summary>
         // ------------------------------------------------------------
         public bool CanOpen(IDocumentLocation location)
         {
            return openSession != null;
         }

         // ------------------------------------------------------------
         /// <summary>
         /// 설정된 열기 session을 반환한다.
         /// </summary>
         // ------------------------------------------------------------
         public DocumentOpenResponse Open(IDocumentLocation location)
         {
            return openSession == null
               ? DocumentOpenResponse.Fail("열기 session이 없습니다.")
               : DocumentOpenResponse.Succeed(openSession);
         }

      #endregion

      #region 저장

         // ------------------------------------------------------------
         /// <summary>
         /// 테스트용 handler는 저장을 지원하지 않는다.
         /// </summary>
         // ------------------------------------------------------------
         public bool CanSave(IDocumentSession session, IDocumentLocation location)
         {
            return false;
         }

         // ------------------------------------------------------------
         /// <summary>
         /// 테스트용 handler는 저장을 실패로 반환한다.
         /// </summary>
         // ------------------------------------------------------------
         public DocumentSaveResponse Save(IDocumentSession session, IDocumentLocation location)
         {
            return DocumentSaveResponse.Fail("저장을 지원하지 않습니다.");
         }

      #endregion

      }

      // ============================================================
      /// <summary>
      /// Handler 반환값 검증용 document session.
      /// </summary>
      // ============================================================
      [Serializable]
      protected sealed class BrokenSession : IDocumentSession
      {

      #region 필드

         // ------------------------------------------------------------
         /// <summary>
         /// 세션이 다루는 문서 정보.
         /// </summary>
         // ------------------------------------------------------------
         public IDocument Document { get; private set; }

         // ------------------------------------------------------------
         /// <summary>
         /// 세션에서 편집 중인 문서 모델.
         /// </summary>
         // ------------------------------------------------------------
         public IDocumentModel Model { get; }

         // ------------------------------------------------------------
         /// <summary>
         /// 세션이 현재 연결된 문서 location.
         /// </summary>
         // ------------------------------------------------------------
         public IDocumentLocation Location { get; private set; }

         // ------------------------------------------------------------
         /// <summary>
         /// 세션의 편집 내용이 저장 대상과 달라졌는지 여부.
         /// </summary>
         // ------------------------------------------------------------
         public bool IsDirty { get; private set; }

      #endregion

      #region 이벤트

         // ------------------------------------------------------------
         /// <summary>
         /// 세션의 문서 정보가 변경될 때 발생한다.
         /// </summary>
         // ------------------------------------------------------------
         public event ValueChangeEventHandler<IDocument> OnDocumentChange = null;

         // ------------------------------------------------------------
         /// <summary>
         /// 세션의 문서 location이 변경될 때 발생한다.
         /// </summary>
         // ------------------------------------------------------------
         public event ValueChangeEventHandler<IDocumentLocation> OnLocationChange = null;

         // ------------------------------------------------------------
         /// <summary>
         /// 세션의 dirty 상태가 변경될 때 발생한다.
         /// </summary>
         // ------------------------------------------------------------
         public event ValueChangeEventHandler<bool> OnDirtyChange = null;

      #endregion

      #region 생성자

         // ------------------------------------------------------------
         /// <summary>
         /// 검증용 document session을 생성한다.
         /// </summary>
         // ------------------------------------------------------------
         public BrokenSession
         (
            IDocument document,
            IDocumentModel model,
            IDocumentLocation location
         ) : base()
         {
            Document = document;
            Model    = model;
            Location = location;
         }

      #endregion

      #region 메서드

         // ------------------------------------------------------------
         /// <summary>
         /// 세션의 문서 정보를 설정한다.
         /// </summary>
         // ------------------------------------------------------------
         public void SetDocument(IDocument document)
         {
            if (Equals(Document, document)) return;

            var previous = Document;

            Document = document;
            OnDocumentChange?.Invoke(this, new ValueChangeEventArgs<IDocument>(previous, Document));
         }

         // ------------------------------------------------------------
         /// <summary>
         /// 세션의 문서 location을 설정한다.
         /// </summary>
         // ------------------------------------------------------------
         public void SetLocation(IDocumentLocation location)
         {
            if (Equals(Location, location)) return;

            var previous = Location;

            Location = location;
            OnLocationChange?.Invoke(this, new ValueChangeEventArgs<IDocumentLocation>(previous, Location));
         }

         // ------------------------------------------------------------
         /// <summary>
         /// 세션을 변경됨 상태로 표시한다.
         /// </summary>
         // ------------------------------------------------------------
         public void SetDirty()
         {
            if (IsDirty) return;

            var previous = IsDirty;

            IsDirty = true;
            OnDirtyChange?.Invoke(this, new ValueChangeEventArgs<bool>(previous, IsDirty));
         }

         // ------------------------------------------------------------
         /// <summary>
         /// 세션의 변경됨 상태를 해제한다.
         /// </summary>
         // ------------------------------------------------------------
         public void ClearDirty()
         {
            if (!IsDirty) return;

            var previous = IsDirty;

            IsDirty = false;
            OnDirtyChange?.Invoke(this, new ValueChangeEventArgs<bool>(previous, IsDirty));
         }

      #endregion

      }

      // ============================================================
      /// <summary>
      /// 지원하지 않는 model 타입 검증용 model.
      /// </summary>
      // ============================================================
      [Serializable]
      protected sealed class ForeignModel : IDocumentModel<object>
      {

      #region 필드

         // ------------------------------------------------------------
         /// <summary>
         /// 테스트용 값.
         /// </summary>
         // ------------------------------------------------------------
         public object Value => value;

         [SerializeReference]
         private object value = new object();

      #endregion

      }

   #endregion

   #region 메서드

      // ------------------------------------------------------------
      /// <summary>
      /// 테스트용 workspace service를 생성한다.
      /// </summary>
      // ------------------------------------------------------------
      protected static DocumentWorkspaceService CreateService(out DocumentWorkspace workspace)
      {
         workspace = new DocumentWorkspace();

         return new DocumentWorkspaceService(workspace, new[] { new DocumentHandler() });
      }

      // ------------------------------------------------------------
      /// <summary>
      /// 테스트용 workspace controller를 생성한다.
      /// </summary>
      // ------------------------------------------------------------
      protected static DocumentWorkspaceController CreateController(out DocumentWorkspace workspace)
      {
         var service = CreateService(out workspace);

         return new DocumentWorkspaceController(service);
      }

      // ------------------------------------------------------------
      /// <summary>
      /// 테스트용 memory document location을 생성한다.
      /// </summary>
      // ------------------------------------------------------------
      protected static MemoryDocumentLocation CreateLocation(string name)
      {
         return new MemoryDocumentLocation(name, name, new MemoryLocation<string>());
      }

      // ------------------------------------------------------------
      /// <summary>
      /// Document location에 연결된 memory IO location을 꺼낸다.
      /// </summary>
      // ------------------------------------------------------------
      protected static MemoryLocation<string> GetIOLocation(MemoryDocumentLocation loc)
      {
         return (MemoryLocation<string>)loc.Value;
      }

      // ------------------------------------------------------------
      /// <summary>
      /// Session model의 payload를 교체하고 dirty 상태로 표시한다.
      /// </summary>
      // ------------------------------------------------------------
      protected static void SetPayload(IDocumentSession session, string text, int count)
      {
         var model = (DocumentModel)session.Model;

         model.SetValue(new Payload(text, count));
         session.SetDirty();
      }

      // ------------------------------------------------------------
      /// <summary>
      /// Session model에서 payload를 꺼낸다.
      /// </summary>
      // ------------------------------------------------------------
      protected static Payload GetPayload(IDocumentSession session)
      {
         var model = (DocumentModel)session.Model;

         return model.Value;
      }

      // ------------------------------------------------------------
      /// <summary>
      /// 저장된 기준 location을 가진 session을 생성한다.
      /// </summary>
      // ------------------------------------------------------------
      protected static IDocumentSession CreateSavedSession
      (
         DocumentWorkspaceService service,
         MemoryDocumentLocation loc,
         string text,
         int count
      )
      {
         var response = service.Create(TypeID, loc.Name);

         Assert.IsTrue(response.Success, response.Error);

         SetPayload(response.Session, text, count);

         var saveResponse = service.SaveAs(response.Session, loc);

         Assert.IsTrue(saveResponse.Success, saveResponse.Error);

         return response.Session;
      }

      // ------------------------------------------------------------
      /// <summary>
      /// 저장된 기준 location을 가진 session을 controller 흐름으로 생성한다.
      /// </summary>
      // ------------------------------------------------------------
      protected static IDocumentSession CreateSavedSession
      (
         DocumentWorkspaceController controller,
         MemoryDocumentLocation loc,
         string text,
         int count
      )
      {
         var createResponse = controller.Create(TypeID, loc.Name);

         Assert.IsTrue(createResponse.Success, createResponse.Error);

         var session = createResponse.Session;

         SetPayload(session, text, count);

         var saveResponse = controller.SaveAs(session, loc);

         Assert.AreEqual(DocumentSaveFlowKind.Saved, saveResponse.Kind, saveResponse.Error);

         return session;
      }

   #endregion

   }
}
