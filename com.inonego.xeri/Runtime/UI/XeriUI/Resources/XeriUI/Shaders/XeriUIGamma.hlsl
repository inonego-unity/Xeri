/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : XeriUIGamma.hlsl
수정일 : 2026-09-01

# 설명
Xeri UI의 Gamma Premultiplied Color를 Linear Premultiplied Color로 변환하는 공통 함수를 정의한다.
Screen UITK 합성과 외부 Gamma Texture 합성이 동일한 색공간 계약을 공유한다.

# 특이사항, 제약사항
호출 Shader가 GammaToLinearSpaceExact를 제공하는 Unity 공통 include를 먼저 포함해야 한다.
========================================================================= BLOCK_HEADER_END */

#ifndef XERI_UI_GAMMA_INCLUDED
#define XERI_UI_GAMMA_INCLUDED

float4 XeriGammaPremultipliedToLinearPremultiplied
(
    float4 color,
    float4 tint
)
{
    // Gamma PMA를 Straight Color로 복원한 뒤 색공간 변환한다.
    if (color.a > 0.001f)
    {
        color.rgb /= color.a;
    }

    #ifndef UNITY_COLORSPACE_GAMMA
    color.r = GammaToLinearSpaceExact(color.r);
    color.g = GammaToLinearSpaceExact(color.g);
    color.b = GammaToLinearSpaceExact(color.b);
    #endif

    // Tint Alpha까지 포함한 최종 Straight Color를 다시 PMA로 만든다.
    color *= tint;
    color.rgb *= color.a;
    return color;
}

#endif
