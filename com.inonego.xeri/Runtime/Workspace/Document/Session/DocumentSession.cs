/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : DocumentSession.cs
수정일 : 2026-06-19

# 설명
Workspace에서 열린 문서의 문서 정보, 모델, location, dirty 상태를 보관하는 기본 세션 구현체.

# 특이사항
저장, 열기, 다른 이름 저장 정책은 포함하지 않고 세션 상태 전환만 담당한다.
========================================================================= BLOCK_HEADER_END */

using System;

using inonego.Xeri;

namespace inonego.Xeri.Workspace.Document
{
   // ============================================================
   /// <summary>
   /// Workspace에서 열린 문서의 기본 세션 구현체.
   /// </summary>
   // ============================================================
   [Serializable]
   public class DocumentSession : IDocumentSession
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
      public bool IsDirty => dirtyFlag.IsDirty;

      // ------------------------------------------------------------
      /// <summary>
      /// 현재 세션을 저장할 수 있는지 여부.
      /// </summary>
      // ------------------------------------------------------------
      public bool CanSave => Document != null && Model != null && Location != null;

      private readonly DirtyFlag dirtyFlag = new DirtyFlag();

   #endregion

   #region 생성자

      // ------------------------------------------------------------
      /// <summary>
      /// 문서 세션을 생성한다.
      /// </summary>
      // ------------------------------------------------------------
      public DocumentSession
      (
         IDocument document,
         IDocumentModel model,
         IDocumentLocation location
      ) : base()
      {
         Document = document ?? throw new ArgumentNullException(nameof(document));
         Model = model ?? throw new ArgumentNullException(nameof(model));
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
         Document = document ?? throw new ArgumentNullException(nameof(document));
      }

      // ------------------------------------------------------------
      /// <summary>
      /// 세션의 문서 location을 설정한다.
      /// </summary>
      // ------------------------------------------------------------
      public void SetLocation(IDocumentLocation location)
      {
         Location = location;
      }

      // ------------------------------------------------------------
      /// <summary>
      /// 세션을 변경됨 상태로 표시한다.
      /// </summary>
      // ------------------------------------------------------------
      public void SetDirty()
      {
         dirtyFlag.SetDirty();
      }

      // ------------------------------------------------------------
      /// <summary>
      /// 세션의 변경됨 상태를 해제한다.
      /// </summary>
      // ------------------------------------------------------------
      public void ClearDirty()
      {
         dirtyFlag.Clear();
      }

   #endregion

   }
}
