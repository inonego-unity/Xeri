/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : DocumentWorkspaceService.cs
수정일 : 2026-06-20

# 설명
DocumentWorkspace와 IDocumentHandler를 조율하여 문서 create/open/save/close 흐름을 수행한다.

# 특이사항
DocumentWorkspace는 session container로 유지하고, 사용자 작업의 의미는 이 service에서 부여한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

namespace inonego.Xeri.Workspace.Document
{
   // ============================================================
   /// <summary>
   /// Document workspace의 create, open, save, close 흐름을 조율하는 서비스.
   /// </summary>
   // ============================================================
   public sealed class DocumentWorkspaceService
   {
      
   #region 필드

      // ------------------------------------------------------------
      /// <summary>
      /// 서비스가 조율하는 document workspace.
      /// </summary>
      // ------------------------------------------------------------
      public DocumentWorkspace Workspace { get; }

      // ------------------------------------------------------------
      /// <summary>
      /// 문서 종류별 handler 매핑.
      /// </summary>
      // ------------------------------------------------------------
      public IReadOnlyDictionary<string, IDocumentHandler> Handlers => handlers;

      private readonly Dictionary<string, IDocumentHandler> handlers = new Dictionary<string, IDocumentHandler>();

   #endregion

   #region 생성자

      // ------------------------------------------------------------
      /// <summary>
      /// Document workspace service를 생성한다.
      /// </summary>
      // ------------------------------------------------------------
      public DocumentWorkspaceService(DocumentWorkspace workspace) : this(workspace, null)
      {

      }

      // ------------------------------------------------------------
      /// <summary>
      /// Document workspace service를 생성한다.
      /// </summary>
      // ------------------------------------------------------------
      public DocumentWorkspaceService
      (
         DocumentWorkspace workspace,
         IEnumerable<IDocumentHandler> handlers
      ) : base()
      {
         Workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));

         CopyHandlers(handlers);
      }

   #endregion

   #region Session 생성과 열기

      // ------------------------------------------------------------
      /// <summary>
      /// 새 문서 세션을 생성하고 성공하면 workspace에 추가한다.
      /// </summary>
      // ------------------------------------------------------------
      public DocumentCreateResponse Create(string _TypeID, string name)
      {
         if (string.IsNullOrEmpty(_TypeID))
         {
            throw new ArgumentException("문서 종류 식별자가 비어 있습니다.", nameof(_TypeID));
         }

         var handler = FindHandlerByTypeID(_TypeID);
         if (handler == null)
         {
            return DocumentCreateResponse.Fail("문서 종류를 생성할 수 있는 handler를 찾을 수 없습니다.");
         }

         var response = handler.Create(name);
         if (!response.Success)
         {
            return response;
         }

         // Handler가 만든 session만 workspace에 공개한다.
         var addError = AddSessionFromHandler(response.Session, handler);
         return string.IsNullOrEmpty(addError) ? response : DocumentCreateResponse.Fail(addError);
      }

      // ------------------------------------------------------------
      /// <summary>
      /// 지정한 문서 종류와 location을 열고 성공하면 workspace에 session을 추가한다.
      /// </summary>
      // ------------------------------------------------------------
      public DocumentOpenResponse Open(string _TypeID, IDocumentLocation location)
      {
         if (string.IsNullOrEmpty(_TypeID))
         {
            throw new ArgumentException("문서 종류 식별자가 비어 있습니다.", nameof(_TypeID));
         }

         if (location == null)
         {
            throw new ArgumentNullException(nameof(location));
         }

         var handler = FindHandlerByTypeID(_TypeID);
         if (handler == null)
         {
            return DocumentOpenResponse.Fail("문서 종류를 열 수 있는 handler를 찾을 수 없습니다.");
         }

         // CanOpen은 handler 선택이 아니라 선택된 handler의 입력 검증이다.
         if (!handler.CanOpen(location))
         {
            return DocumentOpenResponse.Fail("handler가 지정 location을 열 수 없습니다.");
         }

         var response = handler.Open(location);
         if (!response.Success)
         {
            return response;
         }

         var addError = AddSessionFromHandler(response.Session, handler);
         return string.IsNullOrEmpty(addError) ? response : DocumentOpenResponse.Fail(addError);
      }

   #endregion

   #region Session 저장과 닫기

      // ------------------------------------------------------------
      /// <summary>
      /// Session의 현재 location에 저장하고 성공하면 dirty 상태를 해제한다.
      /// </summary>
      // ------------------------------------------------------------
      public DocumentSaveResponse Save(DocumentSession session)
      {
         if (session == null)
         {
            throw new ArgumentNullException(nameof(session));
         }

         if (session.Location == null)
         {
            return DocumentSaveResponse.Fail("저장할 location이 없습니다.");
         }

         var response = SaveCore(session, session.Location);
         if (!response.Success)
         {
            return response;
         }

         session.ClearDirty();
         return response;
      }

      // ------------------------------------------------------------
      /// <summary>
      /// 지정한 location에 저장하고 성공하면 session의 기준 location을 변경한다.
      /// </summary>
      // ------------------------------------------------------------
      public DocumentSaveResponse SaveAs(DocumentSession session, IDocumentLocation location)
      {
         if (session == null)
         {
            throw new ArgumentNullException(nameof(session));
         }

         if (location == null)
         {
            throw new ArgumentNullException(nameof(location));
         }

         var response = SaveCore(session, location);
         if (!response.Success)
         {
            return response;
         }

         // 저장 성공 후에만 SaveAs의 기준 location 변경을 확정한다.
         session.SetLocation(location);
         session.ClearDirty();
         return response;
      }

      // ------------------------------------------------------------
      /// <summary>
      /// 지정한 location에 저장하되 session의 기준 location과 dirty 상태는 변경하지 않는다.
      /// </summary>
      // ------------------------------------------------------------
      public DocumentSaveResponse SaveTo(IDocumentSession session, IDocumentLocation location)
      {
         if (session == null)
         {
            throw new ArgumentNullException(nameof(session));
         }

         if (location == null)
         {
            throw new ArgumentNullException(nameof(location));
         }

         return SaveCore(session, location);
      }

      // ------------------------------------------------------------
      /// <summary>
      /// Workspace에서 지정한 session을 제거한다.
      /// </summary>
      // ------------------------------------------------------------
      public bool Close(IDocumentSession session)
      {
         if (session == null)
         {
            throw new ArgumentNullException(nameof(session));
         }

         return Workspace.RemoveSession(session);
      }

   #endregion

   #region Handler 관리

      // ------------------------------------------------------------
      /// <summary>
      /// 지정한 문서 종류를 처리하는 handler를 찾는다.
      /// </summary>
      // ------------------------------------------------------------
      private IDocumentHandler FindHandlerByTypeID(string _TypeID)
      {
         if (string.IsNullOrEmpty(_TypeID))
         {
            return null;
         }

         return handlers.TryGetValue(_TypeID, out var handler) ? handler : null;
      }

      // ------------------------------------------------------------
      /// <summary>
      /// 생성자 입력 handler 목록을 내부 목록으로 복사한다.
      /// </summary>
      // ------------------------------------------------------------
      private void CopyHandlers(IEnumerable<IDocumentHandler> source)
      {
         if (source == null)
         {
            return;
         }

         foreach (var handler in source)
         {
            if (handler == null)
            {
               throw new ArgumentException("handler 목록에 null이 포함되어 있습니다.", nameof(source));
            }

            if (string.IsNullOrEmpty(handler.TypeID))
            {
               throw new ArgumentException("handler type id가 비어 있습니다.", nameof(source));
            }

            // 하나의 document type은 하나의 handler만 책임진다.
            if (handlers.ContainsKey(handler.TypeID))
            {
               throw new ArgumentException("중복된 handler type id가 포함되어 있습니다.", nameof(source));
            }

            handlers.Add(handler.TypeID, handler);
         }
      }

   #endregion

   #region Session 검증과 반영

      // ------------------------------------------------------------
      /// <summary>
      /// Handler가 반환한 session을 검증하고 workspace에 추가한다.
      /// </summary>
      // ------------------------------------------------------------
      private string AddSessionFromHandler(IDocumentSession session, IDocumentHandler handler)
      {
         var validationError = ValidateSessionForHandler(session, handler);
         if (!string.IsNullOrEmpty(validationError))
         {
            return validationError;
         }

         if (Workspace.HasSession(session))
         {
            return "이미 workspace에 추가된 session입니다.";
         }

         Workspace.AddSession(session);
         return "";
      }

      // ------------------------------------------------------------
      /// <summary>
      /// Handler가 생성한 session의 document type이 handler와 일치하는지 확인한다.
      /// </summary>
      // ------------------------------------------------------------
      private string ValidateSessionForHandler(IDocumentSession session, IDocumentHandler handler)
      {
         if (session == null)
         {
            return "handler가 생성한 session이 없습니다.";
         }

         if (session.Document == null)
         {
            return "handler가 생성한 session에 document가 없습니다.";
         }

         if (string.IsNullOrEmpty(session.Document.TypeID))
         {
            return "handler가 생성한 session의 document type id가 비어 있습니다.";
         }

         if (session.Document.TypeID != handler.TypeID)
         {
            return "handler의 type id와 session document type id가 일치하지 않습니다.";
         }

         return "";
      }

   #endregion

   #region 저장 내부 흐름

      // ------------------------------------------------------------
      /// <summary>
      /// 저장 흐름의 공통 검증과 handler 호출을 수행한다.
      /// </summary>
      // ------------------------------------------------------------
      private DocumentSaveResponse SaveCore(IDocumentSession session, IDocumentLocation location)
      {
         if (!Workspace.HasSession(session))
         {
            return DocumentSaveResponse.Fail("workspace에 포함되지 않은 session입니다.");
         }

         if (session.Document == null)
         {
            return DocumentSaveResponse.Fail("session에 document가 없습니다.");
         }

         var handler = FindHandlerByTypeID(session.Document.TypeID);
         if (handler == null)
         {
            return DocumentSaveResponse.Fail("session document type을 처리할 수 있는 handler를 찾을 수 없습니다.");
         }

         // CanSave는 handler 선택이 아니라 선택된 handler의 저장 가능성 검증이다.
         if (!handler.CanSave(session, location))
         {
            return DocumentSaveResponse.Fail("session을 지정 location에 저장할 수 없습니다.");
         }

         return handler.Save(session, location);
      }

   #endregion

   }
}
