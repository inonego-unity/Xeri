Shader "XeriUI/_GammaToLinearBlit"
{
    HLSLINCLUDE
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
    ENDHLSL

    SubShader
    {
        ZTest Always
        ZWrite Off
        Cull Off

        // Pass 0: 불투명 blit (sRGB → linear)
        Pass
        {
            Name "GammaToLinear"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            float4 Frag(Varyings input) : SV_Target
            {
                float4 c = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, input.texcoord);
                #ifndef UNITY_COLORSPACE_GAMMA
                c.rgb = SRGBToLinear(c.rgb);
                #endif
                return c;
            }
            ENDHLSL
        }

        // Pass 1: 알파 블렌딩 blit (premultiplied alpha, sRGB → linear)
        Pass
        {
            Name "GammaToLinearBlend"
            Blend One OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            float4 Frag(Varyings input) : SV_Target
            {
                float4 c = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, input.texcoord);
                #ifndef UNITY_COLORSPACE_GAMMA
                c.rgb = SRGBToLinear(c.rgb);
                #endif
                return c;
            }
            ENDHLSL
        }

        // Pass 2: 알파 블렌딩 blit (premultiplied alpha, 색공간 변환 없음 — CRT가 이미 변환함)
        Pass
        {
            Name "AlphaBlendOnly"
            Blend One OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            float4 Frag(Varyings input) : SV_Target
            {
                return SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, input.texcoord);
            }
            ENDHLSL
        }
    }
}
