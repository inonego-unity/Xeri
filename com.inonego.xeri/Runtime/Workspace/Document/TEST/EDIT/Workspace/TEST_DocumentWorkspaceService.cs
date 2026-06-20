/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_DocumentWorkspaceService.cs
수정일 : 2026-06-23

# 설명
DocumentWorkspaceService의 create/open/save/close 실행 계약을 검증한다.
Unity Test Runner (Edit Mode) 에서 실행한다.

# 테스트 구성
 F: Flow - create/open/save 대표 흐름
 O: Open - 이미 열린 session 재사용 흐름
 S: Save - save/saveAs/saveTo 상태 변화
 H: Handler - handler 등록과 반환값 검증
 X: Failure - 실패 응답과 실패 후 상태 보존
========================================================================= BLOCK_HEADER_END */

using System;

using NUnit.Framework;

using inonego.Xeri.Workspace.Document;

namespace inonego.Xeri.TEST.Workspace._Document
{
   // ============================================================
   /// <summary>
   /// DocumentWorkspaceService 실행 흐름 테스트.
   /// </summary>
   // ============================================================
   public class TEST_DocumentWorkspaceService : DocumentWorkspaceTestFixture
   {

   #region F-1: Create / SaveAs / Open 라운드트립

      // --------------------------------------------------------------------------------
      /// <summary>
      /// Create한 문서를 SaveAs로 저장한 뒤 닫고 같은 location을 Open하면 payload가 복원된다.
      /// </summary>
      // --------------------------------------------------------------------------------
      [Test]
      public void TEST_DocumentWorkspaceService_Create_SaveAs_Open_라운드트립()
      {
         var service = CreateService(out var workspace);
         var loc = CreateLocation("Roundtrip");

         var createResponse = service.Create(TypeID, "Roundtrip");

         Assert.IsTrue(createResponse.Success, createResponse.Error);
         Assert.AreEqual(1, workspace.Sessions.Count);
         Assert.AreSame(createResponse.Session, workspace.Sessions[0]);

         SetPayload(createResponse.Session, "hello", 42);

         var saveResponse = service.SaveAs(createResponse.Session, loc);

         Assert.IsTrue(saveResponse.Success, saveResponse.Error);
         Assert.AreSame(loc, createResponse.Session.Location);
         Assert.IsFalse(createResponse.Session.IsDirty);
         Assert.IsFalse(string.IsNullOrEmpty(GetIOLocation(loc).Value));

         Assert.IsTrue(service.Close(createResponse.Session));
         Assert.AreEqual(0, workspace.Sessions.Count);

         var openResponse = service.Open(TypeID, loc);

         Assert.IsTrue(openResponse.Success, openResponse.Error);
         Assert.AreEqual(DocumentOpenKind.NewSession, openResponse.Kind);
         Assert.AreEqual(1, workspace.Sessions.Count);
         Assert.AreNotSame(createResponse.Session, openResponse.Session);
         Assert.AreSame(loc, openResponse.Session.Location);
         Assert.IsFalse(openResponse.Session.IsDirty);
         Assert.AreEqual(TypeID, openResponse.Session.Document.TypeID);
         Assert.AreEqual(Version, openResponse.Session.Document.Version);
         Assert.AreEqual(loc.Name, openResponse.Session.Document.Name);
         Assert.AreEqual("hello", GetPayload(openResponse.Session).Text);
         Assert.AreEqual(42, GetPayload(openResponse.Session).Count);
      }

   #endregion

   #region O-1: 이미 열린 session 재사용 흐름

      // ------------------------------------------------------------------------------------------
      /// <summary>
      /// Open은 같은 typeID와 location을 가진 session이 이미 열려 있으면 기존 session을 반환한다.
      /// </summary>
      // ------------------------------------------------------------------------------------------
      [Test]
      public void TEST_DocumentWorkspaceService_Open_같은TypeID와Location_기존Session반환()
      {
         var service = CreateService(out var workspace);
         var loc = CreateLocation("AlreadyOpen");
         var session = CreateSavedSession(service, loc, "base", 1);

         SetPayload(session, "dirty", 2);

         var response = service.Open(TypeID, new MemoryDocumentLocation("Alias", loc.Key));

         Assert.IsTrue(response.Success, response.Error);
         Assert.AreEqual(DocumentOpenKind.AlreadyOpen, response.Kind);
         Assert.AreSame(session, response.Session);
         Assert.AreEqual(1, workspace.Sessions.Count);
         Assert.IsTrue(session.IsDirty);
         Assert.AreEqual("dirty", GetPayload(session).Text);
         Assert.AreEqual(2, GetPayload(session).Count);
      }

      // ------------------------------------------------------------------------------------------
      /// <summary>
      /// Open은 같은 location이라도 typeID가 다르면 기존 session으로 판단하지 않는다.
      /// </summary>
      // ------------------------------------------------------------------------------------------
      [Test]
      public void TEST_DocumentWorkspaceService_Open_같은Location_다른TypeID_기존Session아님()
      {
         var workspace = new DocumentWorkspace();
         var loc = CreateLocation("SameLocation");
         var seedService = CreateService(out var seedWorkspace);
         var seedSession = CreateSavedSession(seedService, loc, "seed", 1);
         var foreignSession = new DocumentSession
         (
            new Document(ForeignTypeID, Version, "Foreign"),
            new DocumentModel(new Payload("foreign", 1)),
            loc
         );
         var service = new DocumentWorkspaceService
         (
            workspace,
            new IDocumentHandler[]
            {
               new DocumentHandler(),
               new TEST_Handler(ForeignTypeID, openSession: foreignSession),
            }
         );

         Assert.IsTrue(seedService.Close(seedSession));
         Assert.AreEqual(0, seedWorkspace.Sessions.Count);
         Assert.IsTrue(workspace.AddSession(foreignSession));

         var response = service.Open(TypeID, loc);

         Assert.IsTrue(response.Success, response.Error);
         Assert.AreEqual(DocumentOpenKind.NewSession, response.Kind);
         Assert.AreEqual(2, workspace.Sessions.Count);
         Assert.AreNotSame(foreignSession, response.Session);
         Assert.AreEqual(TypeID, response.Session.Document.TypeID);
      }

   #endregion

   #region S-1: Save / SaveTo location과 dirty 상태

      // ------------------------------------------------------------
      /// <summary>
      /// Save는 기준 location에 저장하고 성공하면 dirty를 해제한다.
      /// </summary>
      // ------------------------------------------------------------
      [Test]
      public void TEST_DocumentWorkspaceService_Save_기준Location에_저장_Dirty해제()
      {
         var service = CreateService(out _);
         var loc = CreateLocation("Save");
         var session = CreateSavedSession(service, loc, "before", 1);
         var ioLoc = GetIOLocation(loc);
         var beforeText = ioLoc.Value;

         SetPayload(session, "after", 2);

         var response = service.Save(session);

         Assert.IsTrue(response.Success, response.Error);
         Assert.AreSame(loc, session.Location);
         Assert.IsFalse(session.IsDirty);
         Assert.AreNotEqual(beforeText, ioLoc.Value);
      }

      // ----------------------------------------------------------------------
      /// <summary>
      /// SaveTo는 다른 location에 저장하지만 기준 location과 dirty 상태는 유지한다.
      /// </summary>
      // ----------------------------------------------------------------------
      [Test]
      public void TEST_DocumentWorkspaceService_SaveTo_다른Location저장_기준Location과Dirty유지()
      {
         var service = CreateService(out _);
         var baseLoc = CreateLocation("Base");
         var copyLoc = CreateLocation("Copy");
         var session = CreateSavedSession(service, baseLoc, "base", 1);
         var baseText = GetIOLocation(baseLoc).Value;

         SetPayload(session, "copy", 2);

         var response = service.SaveTo(session, copyLoc);

         Assert.IsTrue(response.Success, response.Error);
         Assert.AreSame(baseLoc, session.Location);
         Assert.IsTrue(session.IsDirty);
         Assert.AreEqual(baseText, GetIOLocation(baseLoc).Value);
         Assert.IsFalse(string.IsNullOrEmpty(GetIOLocation(copyLoc).Value));
         Assert.AreNotEqual(baseText, GetIOLocation(copyLoc).Value);
      }

      // ------------------------------------------------------------------------------------------
      /// <summary>
      /// SaveAs 성공 후에는 새 location이 open session lookup 기준이 된다.
      /// </summary>
      // ------------------------------------------------------------------------------------------
      [Test]
      public void TEST_DocumentWorkspaceService_SaveAs_성공_새Location으로OpenSession재사용()
      {
         var service = CreateService(out var workspace);
         var baseLoc = CreateLocation("Base");
         var nextLoc = CreateLocation("Next");
         var session = CreateSavedSession(service, baseLoc, "base", 1);

         SetPayload(session, "next", 2);

         var saveAsResponse = service.SaveAs(session, nextLoc);

         Assert.IsTrue(saveAsResponse.Success, saveAsResponse.Error);
         Assert.AreSame(nextLoc, session.Location);

         var nextOpenResponse = service.Open(TypeID, nextLoc);

         Assert.IsTrue(nextOpenResponse.Success, nextOpenResponse.Error);
         Assert.AreEqual(DocumentOpenKind.AlreadyOpen, nextOpenResponse.Kind);
         Assert.AreSame(session, nextOpenResponse.Session);
         Assert.AreEqual(1, workspace.Sessions.Count);
      }

      // ------------------------------------------------------------------------------------------
      /// <summary>
      /// SaveTo 성공 후에는 기준 location이 유지되므로 복사 location open은 새 session을 만든다.
      /// </summary>
      // ------------------------------------------------------------------------------------------
      [Test]
      public void TEST_DocumentWorkspaceService_SaveTo_성공_복사Location은새Session생성()
      {
         var service = CreateService(out var workspace);
         var baseLoc = CreateLocation("Base");
         var copyLoc = CreateLocation("Copy");
         var session = CreateSavedSession(service, baseLoc, "base", 1);

         SetPayload(session, "copy", 2);

         var saveToResponse = service.SaveTo(session, copyLoc);

         Assert.IsTrue(saveToResponse.Success, saveToResponse.Error);
         Assert.AreSame(baseLoc, session.Location);

         var baseOpenResponse = service.Open(TypeID, baseLoc);

         Assert.IsTrue(baseOpenResponse.Success, baseOpenResponse.Error);
         Assert.AreEqual(DocumentOpenKind.AlreadyOpen, baseOpenResponse.Kind);
         Assert.AreSame(session, baseOpenResponse.Session);
         Assert.AreEqual(1, workspace.Sessions.Count);

         var copyOpenResponse = service.Open(TypeID, copyLoc);

         Assert.IsTrue(copyOpenResponse.Success, copyOpenResponse.Error);
         Assert.AreEqual(DocumentOpenKind.NewSession, copyOpenResponse.Kind);
         Assert.AreNotSame(session, copyOpenResponse.Session);
         Assert.AreEqual(2, workspace.Sessions.Count);
         Assert.AreSame(copyLoc, copyOpenResponse.Session.Location);
      }

   #endregion

   #region H-1: Handler 등록 검증

      // ------------------------------------------------------------
      /// <summary>
      /// 중복된 handler TypeID는 service 생성 단계에서 실패한다.
      /// </summary>
      // ------------------------------------------------------------
      [Test]
      public void TEST_DocumentWorkspaceService_Handler_중복TypeID_등록실패()
      {
         var workspace = new DocumentWorkspace();
         var handlers = new IDocumentHandler[]
         {
            new DocumentHandler(),
            new DocumentHandler(),
         };

         Assert.Throws<ArgumentException>
         (
            () => new DocumentWorkspaceService(workspace, handlers)
         );
         Assert.AreEqual(0, workspace.Sessions.Count);
      }

      // ------------------------------------------------------------
      /// <summary>
      /// null handler는 service 생성 단계에서 실패한다.
      /// </summary>
      // ------------------------------------------------------------
      [Test]
      public void TEST_DocumentWorkspaceService_Handler_Null_등록실패()
      {
         var workspace = new DocumentWorkspace();
         var handlers = new IDocumentHandler[] { null };

         Assert.Throws<ArgumentException>
         (
            () => new DocumentWorkspaceService(workspace, handlers)
         );
         Assert.AreEqual(0, workspace.Sessions.Count);
      }

      // ------------------------------------------------------------
      /// <summary>
      /// 비어 있는 handler TypeID는 service 생성 단계에서 실패한다.
      /// </summary>
      // ------------------------------------------------------------
      [Test]
      public void TEST_DocumentWorkspaceService_Handler_빈TypeID_등록실패()
      {
         var workspace = new DocumentWorkspace();
         var handlers = new IDocumentHandler[] { new TEST_Handler("") };

         Assert.Throws<ArgumentException>
         (
            () => new DocumentWorkspaceService(workspace, handlers)
         );
         Assert.AreEqual(0, workspace.Sessions.Count);
      }

   #endregion

   #region H-2: Handler 반환값 검증

      // ------------------------------------------------------------
      /// <summary>
      /// Handler가 다른 TypeID의 session을 반환하면 workspace에 추가하지 않는다.
      /// </summary>
      // ------------------------------------------------------------
      [Test]
      public void TEST_DocumentWorkspaceService_Handler_다른TypeIDSession_추가실패()
      {
         var workspace = new DocumentWorkspace();
         var session = new DocumentSession
         (
            new Document(ForeignTypeID, Version, "Foreign"),
            new DocumentModel(new Payload("foreign", 1)),
            null
         );
         var service = new DocumentWorkspaceService
         (
            workspace,
            new IDocumentHandler[] { new TEST_Handler(TypeID, session) }
         );

         var response = service.Create(TypeID, "Invalid");

         Assert.IsFalse(response.Success);
         Assert.IsFalse(string.IsNullOrEmpty(response.Error));
         Assert.IsNull(response.Session);
         Assert.AreEqual(0, workspace.Sessions.Count);
      }

      // ------------------------------------------------------------
      /// <summary>
      /// Handler가 Open에서 다른 TypeID의 session을 반환하면 workspace에 추가하지 않는다.
      /// </summary>
      // ------------------------------------------------------------
      [Test]
      public void TEST_DocumentWorkspaceService_Handler_Open_다른TypeIDSession_추가실패()
      {
         var workspace = new DocumentWorkspace();
         var loc = CreateLocation("InvalidOpen");
         var session = new DocumentSession
         (
            new Document(ForeignTypeID, Version, "Foreign"),
            new DocumentModel(new Payload("foreign", 1)),
            loc
         );
         var service = new DocumentWorkspaceService
         (
            workspace,
            new IDocumentHandler[] { new TEST_Handler(TypeID, openSession: session) }
         );

         var response = service.Open(TypeID, loc);

         Assert.IsFalse(response.Success);
         Assert.AreEqual(DocumentOpenKind.None, response.Kind);
         Assert.IsFalse(string.IsNullOrEmpty(response.Error));
         Assert.IsNull(response.Session);
         Assert.AreEqual(0, workspace.Sessions.Count);
      }

      // ------------------------------------------------------------
      /// <summary>
      /// Handler가 Open에서 location 없는 session을 반환하면 workspace에 추가하지 않는다.
      /// </summary>
      // ------------------------------------------------------------
      [Test]
      public void TEST_DocumentWorkspaceService_Handler_Open_Location없는Session_추가실패()
      {
         var workspace = new DocumentWorkspace();
         var loc = CreateLocation("InvalidOpen");
         var session = new DocumentSession
         (
            new Document(TypeID, Version, "NoLocation"),
            new DocumentModel(new Payload("invalid", 1)),
            null
         );
         var service = new DocumentWorkspaceService
         (
            workspace,
            new IDocumentHandler[] { new TEST_Handler(TypeID, openSession: session) }
         );

         var response = service.Open(TypeID, loc);

         Assert.IsFalse(response.Success);
         Assert.AreEqual(DocumentOpenKind.None, response.Kind);
         Assert.IsFalse(string.IsNullOrEmpty(response.Error));
         Assert.IsNull(response.Session);
         Assert.AreEqual(0, workspace.Sessions.Count);
      }

      // ------------------------------------------------------------
      /// <summary>
      /// Handler가 Open에서 요청 location과 다른 session을 반환하면 workspace에 추가하지 않는다.
      /// </summary>
      // ------------------------------------------------------------
      [Test]
      public void TEST_DocumentWorkspaceService_Handler_Open_다른LocationSession_추가실패()
      {
         var workspace = new DocumentWorkspace();
         var loc = CreateLocation("Requested");
         var otherLoc = CreateLocation("Other");
         var session = new DocumentSession
         (
            new Document(TypeID, Version, "Other"),
            new DocumentModel(new Payload("invalid", 1)),
            otherLoc
         );
         var service = new DocumentWorkspaceService
         (
            workspace,
            new IDocumentHandler[] { new TEST_Handler(TypeID, openSession: session) }
         );

         var response = service.Open(TypeID, loc);

         Assert.IsFalse(response.Success);
         Assert.AreEqual(DocumentOpenKind.None, response.Kind);
         Assert.IsFalse(string.IsNullOrEmpty(response.Error));
         Assert.IsNull(response.Session);
         Assert.AreEqual(0, workspace.Sessions.Count);
      }

      // ------------------------------------------------------------
      /// <summary>
      /// Handler가 document 없는 session을 반환하면 workspace에 추가하지 않는다.
      /// </summary>
      // ------------------------------------------------------------
      [Test]
      public void TEST_DocumentWorkspaceService_Handler_Document없는Session_추가실패()
      {
         var workspace = new DocumentWorkspace();
         var session = new BrokenSession
         (
            null,
            new DocumentModel(new Payload("broken", 1)),
            null
         );
         var service = new DocumentWorkspaceService
         (
            workspace,
            new IDocumentHandler[] { new TEST_Handler(TypeID, session) }
         );

         var response = service.Create(TypeID, "Invalid");

         Assert.IsFalse(response.Success);
         Assert.IsFalse(string.IsNullOrEmpty(response.Error));
         Assert.IsNull(response.Session);
         Assert.AreEqual(0, workspace.Sessions.Count);
      }

      // ------------------------------------------------------------
      /// <summary>
      /// Handler가 model 없는 session을 반환하면 workspace에 추가하지 않는다.
      /// </summary>
      // ------------------------------------------------------------
      [Test]
      public void TEST_DocumentWorkspaceService_Handler_Model없는Session_추가실패()
      {
         var workspace = new DocumentWorkspace();
         var session = new BrokenSession
         (
            new Document(TypeID, Version, "NoModel"),
            null,
            null
         );
         var service = new DocumentWorkspaceService
         (
            workspace,
            new IDocumentHandler[] { new TEST_Handler(TypeID, session) }
         );

         var response = service.Create(TypeID, "Invalid");

         Assert.IsFalse(response.Success);
         Assert.IsFalse(string.IsNullOrEmpty(response.Error));
         Assert.IsNull(response.Session);
         Assert.AreEqual(0, workspace.Sessions.Count);
      }

      // ------------------------------------------------------------
      /// <summary>
      /// Handler가 Open에서 model 없는 session을 반환하면 workspace에 추가하지 않는다.
      /// </summary>
      // ------------------------------------------------------------
      [Test]
      public void TEST_DocumentWorkspaceService_Handler_Open_Model없는Session_추가실패()
      {
         var workspace = new DocumentWorkspace();
         var loc = CreateLocation("NoModelOpen");
         var session = new BrokenSession
         (
            new Document(TypeID, Version, "NoModel"),
            null,
            loc
         );
         var service = new DocumentWorkspaceService
         (
            workspace,
            new IDocumentHandler[] { new TEST_Handler(TypeID, openSession: session) }
         );

         var response = service.Open(TypeID, loc);

         Assert.IsFalse(response.Success);
         Assert.AreEqual(DocumentOpenKind.None, response.Kind);
         Assert.IsFalse(string.IsNullOrEmpty(response.Error));
         Assert.IsNull(response.Session);
         Assert.AreEqual(0, workspace.Sessions.Count);
      }

   #endregion

   #region X-1: 실패 응답과 상태 보존

      // ------------------------------------------------------------
      /// <summary>
      /// 등록되지 않은 문서 종류 Create는 실패하고 session을 추가하지 않는다.
      /// </summary>
      // ------------------------------------------------------------
      [Test]
      public void TEST_DocumentWorkspaceService_Create_지원하지않는TypeID_실패()
      {
         var service = CreateService(out var workspace);

         var response = service.Create(ForeignTypeID, "Unsupported");

         Assert.IsFalse(response.Success);
         Assert.IsFalse(string.IsNullOrEmpty(response.Error));
         Assert.IsNull(response.Session);
         Assert.AreEqual(0, workspace.Sessions.Count);
      }

      // ------------------------------------------------------------
      /// <summary>
      /// Location 없는 새 문서 Save는 실패하고 session 상태를 유지한다.
      /// </summary>
      // ------------------------------------------------------------
      [Test]
      public void TEST_DocumentWorkspaceService_Save_Location없는_새문서_실패()
      {
         var service = CreateService(out _);
         var createResponse = service.Create(TypeID, "NoLocation");

         Assert.IsTrue(createResponse.Success, createResponse.Error);

         createResponse.Session.SetDirty();

         var response = service.Save(createResponse.Session);

         Assert.IsFalse(response.Success);
         Assert.IsFalse(string.IsNullOrEmpty(response.Error));
         Assert.IsNull(createResponse.Session.Location);
         Assert.IsTrue(createResponse.Session.IsDirty);
      }

      // ------------------------------------------------------------
      /// <summary>
      /// SaveAs 실패는 기준 location과 dirty 상태를 유지한다.
      /// </summary>
      // ------------------------------------------------------------
      [Test]
      public void TEST_DocumentWorkspaceService_SaveAs_실패_기준Location과Dirty유지()
      {
         var service = CreateService(out _);
         var baseLoc = CreateLocation("Base");
         var unsupportedLoc = new MemoryDocumentLocation("Unsupported", "Unsupported", new object());
         var session = CreateSavedSession(service, baseLoc, "base", 1);
         var baseText = GetIOLocation(baseLoc).Value;

         SetPayload(session, "changed", 2);

         var response = service.SaveAs(session, unsupportedLoc);

         Assert.IsFalse(response.Success);
         Assert.IsFalse(string.IsNullOrEmpty(response.Error));
         Assert.AreSame(baseLoc, session.Location);
         Assert.IsTrue(session.IsDirty);
         Assert.AreEqual(baseText, GetIOLocation(baseLoc).Value);
      }

      // ------------------------------------------------------------
      /// <summary>
      /// SaveTo 실패는 기준 location과 dirty 상태를 유지한다.
      /// </summary>
      // ------------------------------------------------------------
      [Test]
      public void TEST_DocumentWorkspaceService_SaveTo_실패_기준Location과Dirty유지()
      {
         var service = CreateService(out _);
         var baseLoc = CreateLocation("Base");
         var unsupportedLoc = new MemoryDocumentLocation("Unsupported", "Unsupported", new object());
         var session = CreateSavedSession(service, baseLoc, "base", 1);
         var baseText = GetIOLocation(baseLoc).Value;

         SetPayload(session, "changed", 2);

         var response = service.SaveTo(session, unsupportedLoc);

         Assert.IsFalse(response.Success);
         Assert.IsFalse(string.IsNullOrEmpty(response.Error));
         Assert.AreSame(baseLoc, session.Location);
         Assert.IsTrue(session.IsDirty);
         Assert.AreEqual(baseText, GetIOLocation(baseLoc).Value);
      }

      // ------------------------------------------------------------
      /// <summary>
      /// Handler가 변환할 수 없는 location은 Open 실패로 처리된다.
      /// </summary>
      // ------------------------------------------------------------
      [Test]
      public void TEST_DocumentWorkspaceService_Open_지원하지않는Location_실패()
      {
         var service = CreateService(out var workspace);
         var loc = new ObjectDocumentLocation("Unsupported", new object());

         var response = service.Open(TypeID, loc);

         Assert.IsFalse(response.Success);
         Assert.AreEqual(DocumentOpenKind.None, response.Kind);
         Assert.IsFalse(string.IsNullOrEmpty(response.Error));
         Assert.IsNull(response.Session);
         Assert.AreEqual(0, workspace.Sessions.Count);
      }

      // ------------------------------------------------------------
      /// <summary>
      /// Handler가 지원하지 않는 model 타입은 Save 실패로 처리된다.
      /// </summary>
      // ------------------------------------------------------------
      [Test]
      public void TEST_DocumentWorkspaceService_Save_지원하지않는Model_실패()
      {
         var service = CreateService(out var workspace);
         var loc = CreateLocation("Foreign");
         var session = new DocumentSession
         (
            new Document(TypeID, Version, "Foreign"),
            new ForeignModel(),
            loc
         );

         workspace.AddSession(session);
         session.SetDirty();

         var response = service.Save(session);

         Assert.IsFalse(response.Success);
         Assert.IsFalse(string.IsNullOrEmpty(response.Error));
         Assert.IsTrue(session.IsDirty);
         Assert.AreSame(loc, session.Location);
         Assert.IsTrue(string.IsNullOrEmpty(GetIOLocation(loc).Value));
      }

      // ------------------------------------------------------------
      /// <summary>
      /// Workspace에 없는 session Close는 실패 반환하고 workspace 상태를 변경하지 않는다.
      /// </summary>
      // ------------------------------------------------------------
      [Test]
      public void TEST_DocumentWorkspaceService_Close_Workspace밖Session_실패()
      {
         var service = CreateService(out var workspace);
         var session = new DocumentSession
         (
            new Document(TypeID, Version, "Detached"),
            new DocumentModel(new Payload("detached", 1)),
            CreateLocation("Detached")
         );

         var response = service.Close(session);

         Assert.IsFalse(response);
         Assert.AreEqual(0, workspace.Sessions.Count);
      }

   #endregion

   }
}
