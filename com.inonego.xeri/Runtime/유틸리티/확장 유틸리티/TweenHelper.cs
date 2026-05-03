/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TweenHelper.cs
수정일 : 2026-05-03

# 설명
트윈 애니메이션 커브 데이터를 담는 직렬화 가능 구조체 모음.
DOTween 미사용 환경에서는 Ease.None 폴백 열거형을 함께 제공한다.
========================================================================= BLOCK_HEADER_END */

using System;

#if DOTWEEN
using DG.Tweening;
#endif

namespace inonego.Xeri
{

#if !DOTWEEN
    // DOTween 미사용 환경에서의 폴백 열거형
    public enum Ease { None }
#endif

    // ============================================================
    /// <summary>
    /// 단방향 트윈 커브 데이터 (지속 시간·지연·이징).
    /// </summary>
    // ============================================================
    [Serializable]
    public struct TweenCurve
    {
        public float Duration;
        public float Delay;
        public Ease  Ease;

        // ------------------------------------------------------------
        /// <summary>
        /// Duration·Delay·Ease를 지정해 초기화한다.
        /// </summary>
        // ------------------------------------------------------------
        public TweenCurve(float duration, float delay, Ease ease)
        {
            (Duration, Delay, Ease) = (duration, delay, ease);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Duration·Delay·Ease를 분해 대입(Deconstruct)으로 추출한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Deconstruct(out float duration, out float delay, out Ease ease)
        {
            (duration, delay, ease) = (Duration, Delay, Ease);
        }
    }

    // ============================================================
    /// <summary>
    /// 진입(In)·퇴장(Out) 트윈 커브 쌍 데이터.
    /// </summary>
    // ============================================================
    [Serializable]
    public struct TweenInOutCurve
    {
        public TweenCurve In;
        public TweenCurve Out;

        // ------------------------------------------------------------
        /// <summary>
        /// In·Out 커브를 지정해 초기화한다.
        /// </summary>
        // ------------------------------------------------------------
        public TweenInOutCurve(TweenCurve inCurve, TweenCurve outCurve)
        {
            (In, Out) = (inCurve, outCurve);
        }
    }
}
