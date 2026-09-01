/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : GammaTextureComposite.shader
수정일 : 2026-09-01

# 설명
UGUI Graphic이 받은 Gamma Premultiplied Texture를 Xeri Linear PMA Overlay Surface에 직접 합성한다.
World-space UITK Camera RT 등 외부 Gamma Texture가 Screen UITK와 동일한 Xeri 색공간 계약을 사용하게 한다.

# 특이사항, 제약사항
입력 Texture는 Gamma numeric RGB와 Premultiplied Alpha를 보존해야 한다.
UGUI Stencil, RectMask와 ColorMask를 따르며 clipping coverage는 RGB/A에 함께 적용해 PMA를 유지한다.
========================================================================= BLOCK_HEADER_END */

Shader "Hidden/XeriUI/GammaTextureComposite"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend One OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "GammaTextureToLinearPMA"

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            #include "XeriUIGamma.hlsl"

            struct Attributes
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                float4 localPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;
            float4 _ClipRect;

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.positionCS = UnityObjectToClipPos(input.vertex);
                output.color = input.color * _Color;
                output.uv = TRANSFORM_TEX(input.texcoord, _MainTex);
                output.localPosition = input.vertex;
                return output;
            }

            float4 Frag(Varyings input) : SV_Target
            {
                float4 gammaPma = tex2D(_MainTex, input.uv);
                float4 color = XeriGammaPremultipliedToLinearPremultiplied
                (
                    gammaPma,
                    input.color
                );

                #ifdef UNITY_UI_CLIP_RECT
                color *= UnityGet2DClipping(input.localPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001f);
                #endif

                return color;
            }

            ENDHLSL
        }
    }

    FallBack Off
}
