/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : DocumentWorkspace.cs
수정일 : 2026-06-19

# 설명
열린 document session 목록을 보관하는 Workspace container를 정의한다.
Open, Save, ActiveSession 관리는 별도 service 또는 view/controller 계층의 책임으로 둔다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

namespace inonego.Xeri.Workspace.Document
{
   // ============================================================
   /// <summary>
   /// 열린 document session 목록을 관리하는 Workspace container.
   /// </summary>
   // ============================================================
   public sealed class DocumentWorkspace
   {
      
   #region 필드

      // ------------------------------------------------------------
      /// <summary>
      /// 현재 Workspace에 열린 document session 목록.
      /// </summary>
      // ------------------------------------------------------------
      public IReadOnlyList<IDocumentSession> Sessions => sessions;

      private readonly List<IDocumentSession> sessions = new List<IDocumentSession>();

   #endregion

   #region 이벤트

      // ------------------------------------------------------------
      /// <summary>
      /// Document session이 Workspace에 추가될 때 호출된다.
      /// </summary>
      // ------------------------------------------------------------
      public event EventHandler<DocumentSessionEventArgs> OnSessionAdd = null;

      // ------------------------------------------------------------
      /// <summary>
      /// Document session이 Workspace에서 제거될 때 호출된다.
      /// </summary>
      // ------------------------------------------------------------
      public event EventHandler<DocumentSessionEventArgs> OnSessionRemove = null;

   #endregion

   #region 메서드

      // ------------------------------------------------------------
      /// <summary>
      /// Workspace에 document session이 포함되어 있는지 확인한다.
      /// </summary>
      // ------------------------------------------------------------
      public bool HasSession(IDocumentSession session)
      {
         if (session == null)
         {
            throw new ArgumentNullException(nameof(session));
         }

         return sessions.Contains(session);
      }

      // ------------------------------------------------------------
      /// <summary>
      /// Workspace에 document session을 추가한다.
      /// </summary>
      // ------------------------------------------------------------
      public bool AddSession(IDocumentSession session)
      {
         if (session == null)
         {
            throw new ArgumentNullException(nameof(session));
         }

         if (sessions.Contains(session))
         {
            return false;
         }

         sessions.Add(session);
         OnSessionAdd?.Invoke(this, new DocumentSessionEventArgs(session));

         return true;
      }

      // ------------------------------------------------------------
      /// <summary>
      /// Workspace에서 document session을 제거한다.
      /// </summary>
      // ------------------------------------------------------------
      public bool RemoveSession(IDocumentSession session)
      {
         if (session == null)
         {
            throw new ArgumentNullException(nameof(session));
         }

         if (!sessions.Remove(session))
         {
            return false;
         }

         OnSessionRemove?.Invoke(this, new DocumentSessionEventArgs(session));

         return true;
      }

   #endregion

   }
}
