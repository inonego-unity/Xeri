/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : HDRPOverlayComposite.shader
수정일 : 2026-08-23

# 설명
HDRP AfterPostProcess Scene Color와 Xeri FP16 Screen Overlay Surface를 Linear Premultiplied Alpha로 합성한다.

# 특이사항, 제약사항
Overlay Surface가 없을 때는 Custom Post Process Source를 그대로 출력한다.
========================================================================= BLOCK_HEADER_END */

Shader "Hidden/XeriUI/HDRPOverlayComposite"
{
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "HDRenderPipeline"
        }

        Pass
        {
            Name "OverlayComposite"
            Cull Off
            ZTest Always
            ZWrite Off
            Blend Off

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"

            TEXTURE2D_X(_CustomPostProcessInput);
            TEXTURE2D(_XeriUITexture);

            float4 _XeriUIViewportParams;
            float _XeriUIEnabled;

            struct Attributes
            {
                uint vertexID : SV_VertexID;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
                return output;
            }

            float4 Frag(Varyings input) : SV_Target
            {
                uint2 pixel = uint2(input.positionCS.xy);
                float4 sceneColor = LOAD_TEXTURE2D_X(_CustomPostProcessInput, pixel);
                if (_XeriUIEnabled < 0.5f)
                {
                    return sceneColor;
                }

                uint2 uiPixel = uint2(input.positionCS.xy);
                float4 uiColor = LOAD_TEXTURE2D(_XeriUITexture, uiPixel);

                // Overlay Surface는 Linear Premultiplied Alpha이므로 추가 색공간 변환 없이 Source 위에 합성한다.
                return uiColor + sceneColor * (1.0f - uiColor.a);
            }

            ENDHLSL
        }
    }

    FallBack Off
}
