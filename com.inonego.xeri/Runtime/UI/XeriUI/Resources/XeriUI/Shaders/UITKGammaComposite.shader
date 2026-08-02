/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : UITKGammaComposite.shader
수정일 : 2026-08-01

# 설명
UI Toolkit Background Image로 전달된 Gamma Panel RenderTexture를 Linear 화면에 합성한다.

# 특이사항, 제약사항
Premultiplied RenderTexture를 Straight Color로 변환한 뒤 gamma→linear 변환하고 다시 Premultiply한다.
========================================================================= BLOCK_HEADER_END */

Shader "Hidden/XeriUI/UITKGammaComposite"
{
    SubShader
    {
        Tags
        {
            "RenderType"         = "Transparent"
            "isCustomUITKShader" = "true"
            "Queue"              = "Overlay"
        }

        Cull Off
        Blend One OneMinusSrcAlpha
        ZWrite Off

        Pass
        {
            Name "GammaToLinearBlend"

            HLSLPROGRAM

            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag

            #pragma multi_compile_local _ _UIE_TEXTURE_SLOT_COUNT_4 _UIE_TEXTURE_SLOT_COUNT_2 _UIE_TEXTURE_SLOT_COUNT_1
            #pragma multi_compile_local _ _UIE_RENDER_TYPE_TEXTURE

            #include "Internal/UnityUIE.cginc"

            v2f Vert(appdata_t input)
            {
                return uie_std_vert(input);
            }

            UIE_FRAG_T Frag(v2f input) : SV_Target
            {
                half textureSlot = input.typeTexSettings.y;
                float4 color = SampleTextureSlot(textureSlot, input.uvClip.xy);

                // Offscreen UI는 Premultiplied Gamma이므로 색 공간 변환 전 Straight Color를 복원한다.
                if (color.a > 0.001f)
                {
                    color.rgb /= color.a;
                }

                #ifndef UNITY_COLORSPACE_GAMMA
                color.r = GammaToLinearSpaceExact(color.r);
                color.g = GammaToLinearSpaceExact(color.g);
                color.b = GammaToLinearSpaceExact(color.b);
                #endif

                color *= input.color;
                color.rgb *= color.a;

                float coverage = 1.0f;

                if (TestIsArc(input.typeTexSettings.w))
                {
                    coverage = ComputeCoverage(input.circle.xy, input.circle.zw);
                }

                coverage *= uie_fragment_clip(input.uvClip.zw);
                clip(coverage - 0.003f);

                return color * coverage;
            }

            ENDHLSL
        }
    }

    FallBack Off
}
