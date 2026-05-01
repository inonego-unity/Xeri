/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : INeedToConnect.cs
수정일 : 2026-05-01

# 설명
외부 대상과 연결/해제가 필요한 객체를 위한 인터페이스.
파라미터 없는 버전과 추가 파라미터를 받는 버전 두 가지를 제공한다.
========================================================================= BLOCK_HEADER_END */

namespace inonego.Xeri
{
    // ============================================================
    /// <summary>
    /// 연결해야 하는 객체를 위한 인터페이스입니다.
    /// </summary>
    // ============================================================
    public interface INeedToConnect<T>
    {
        public void Connect(T lTarget);
        public void Disconnect();
    }

    // ============================================================
    /// <summary>
    /// 추가 파라미터와 함께 연결해야 하는 객체를 위한 인터페이스입니다.
    /// </summary>
    // ============================================================
    public interface INeedToConnect<T, TParam>
    {
        public void Connect(T lTarget, TParam param);
        public void Disconnect();
    }
}
