/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : ScreenSession.cs
수정일 : 2026-07-30

# 설명
한 Screen의 상태, Source, Transition, Layer, 입력과 하위 표시 Handle 수명을 묶는다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections.Generic;

namespace inonego.Xeri.UI.Game
{
    // ============================================================
    /// <summary>
    /// 한 Screen의 실행 수명과 상태 조회를 제공한다.
    /// </summary>
    // ============================================================
    public sealed class ScreenSession
    {
    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// Screen stable string ID.
        /// </summary>
        // ------------------------------------------------------------
        public string ID => Options.ID;

        // ------------------------------------------------------------
        /// <summary>
        /// Screen 등록 정책.
        /// </summary>
        // ------------------------------------------------------------
        public ScreenOptions Options { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// Screen Open 호출 인자.
        /// </summary>
        // ------------------------------------------------------------
        public ScreenOpenParams OpenParams { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 Screen 상태.
        /// </summary>
        // ------------------------------------------------------------
        public ScreenState State { get; internal set; }

        // ------------------------------------------------------------
        /// <summary>
        /// Source 획득 뒤 Stack 실행 수명으로 수락됐는지 여부.
        /// </summary>
        // ------------------------------------------------------------
        internal bool IsAccepted { get; set; }

        // ------------------------------------------------------------
        /// <summary>
        /// Screen View와 Presenter를 공급한 Source.
        /// </summary>
        // ------------------------------------------------------------
        internal IScreenSource Source { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// Source가 조립한 Screen backend 묶음.
        /// </summary>
        // ------------------------------------------------------------
        internal ScreenInstance Instance { get; set; }

        // ------------------------------------------------------------
        /// <summary>
        /// Screen이 점유한 Presentation Layer 사용 수명.
        /// </summary>
        // ------------------------------------------------------------
        internal IDisposable LayerUsage { get; set; }

        // ------------------------------------------------------------
        /// <summary>
        /// Screen의 입력 정책 점유 수명.
        /// </summary>
        // ------------------------------------------------------------
        internal ScreenInputSession InputSession { get; set; }

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 진행 중인 Presentation Transition.
        /// </summary>
        // ------------------------------------------------------------
        internal PresentationTransitionHandle Transition { get; set; }

        // ------------------------------------------------------------
        /// <summary>
        /// Replace 요청에서 새 Screen이 대체하는 이전 Session.
        /// </summary>
        // ------------------------------------------------------------
        internal ScreenSession ReplacedSession { get; set; }

        // ------------------------------------------------------------
        /// <summary>
        /// 닫기 완료 뒤 이전 Stack top을 복원할지 여부.
        /// </summary>
        // ------------------------------------------------------------
        internal bool RestorePreviousOnClose { get; set; }

        // ------------------------------------------------------------
        /// <summary>
        /// 닫기 입력 해제 대기 중 현재 Screen의 Cursor 정책을 유지할지 여부.
        /// </summary>
        // ------------------------------------------------------------
        internal bool RetainCursorOnClose { get; set; }

        // ------------------------------------------------------------
        /// <summary>
        /// OnClosed 훅이 이미 호출됐는지 여부.
        /// </summary>
        // ------------------------------------------------------------
        internal bool ClosedHookCalled { get; set; }

        // ------------------------------------------------------------
        /// <summary>
        /// Source View와 Presenter가 반환됐는지 여부.
        /// </summary>
        // ------------------------------------------------------------
        internal bool SourceReleased { get; set; }

        // ------------------------------------------------------------
        /// <summary>
        /// Presentation Layer 사용 수명이 반환됐는지 여부.
        /// </summary>
        // ------------------------------------------------------------
        internal bool LayerReleased { get; set; }

        // ------------------------------------------------------------
        /// <summary>
        /// 입력 정책 Session 반환이 요청됐는지 여부.
        /// </summary>
        // ------------------------------------------------------------
        internal bool InputReleased { get; set; }

        // ------------------------------------------------------------
        /// <summary>
        /// 늦은 Transition callback을 거부할 현재 세대값.
        /// </summary>
        // ------------------------------------------------------------
        internal int TransitionGeneration { get; set; }

        private readonly ScreenController controller = null;
        private readonly List<IDisposable> childHandles = new List<IDisposable>();

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// Controller가 소유하는 준비 상태 Screen Session을 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        internal ScreenSession
        (
            ScreenController controller,
            ScreenOptions options,
            ScreenOpenParams openParams,
            IScreenSource source
        ) : base()
        {
            this.controller = controller ?? throw new ArgumentNullException(nameof(controller));
            Options = options ?? throw new ArgumentNullException(nameof(options));
            OpenParams = openParams;
            Source = source ?? throw new ArgumentNullException(nameof(source));
            State = ScreenState.Opening;
        }

    #endregion

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 Session이 Stack top일 때 같은 Controller 닫기 경로를 요청한다.
        /// </summary>
        // ------------------------------------------------------------
        public bool Close()
        {
            return controller.Close(this);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 부모 Screen 종료 시 역순으로 해제할 하위 표시 Handle을 등록한다.
        /// </summary>
        // ------------------------------------------------------------
        public THandle RegisterChild<THandle>(THandle handle)
        where THandle : class, IDisposable
        {
            if (handle == null)
            {
                throw new ArgumentNullException(nameof(handle));
            }

            if (State == ScreenState.Closing || State == ScreenState.Closed)
            {
                throw new InvalidOperationException("종료 중인 Screen에는 하위 Handle을 등록할 수 없습니다.");
            }

            childHandles.Add(handle);
            return handle;
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 하위 표시 Handle을 생성 역순으로 소유 목록에서 먼저 제거한 뒤 한 번 해제한다.
        /// <br/> 실패한 Handle은 다시 보관하지 않고 나머지 독립 정리를 계속한다.
        /// </summary>
        // ----------------------------------------------------------------------
        internal List<Exception> ReleaseChildren()
        {
            var errors = new List<Exception>();

            for (var i = childHandles.Count - 1; i >= 0; i--)
            {
                var handle = childHandles[i];
                childHandles.RemoveAt(i);

                try
                {
                    handle.Dispose();
                }
                catch (Exception exception)
                {
                    errors.Add(exception);
                }
            }

            return errors;
        }

    #endregion

    }
}
