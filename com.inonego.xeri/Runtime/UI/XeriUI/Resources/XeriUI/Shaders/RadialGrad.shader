/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : RadialGrad.shader
수정일 : 2026-08-02

# 설명
UI Toolkit Element의 Layout UV를 기준으로 다중 Stop 방사형 Gradient를 렌더링한다.

# 특이사항, 제약사항
UnityUIE 공통 계약을 사용하며 특정 Scriptable Render Pipeline에 의존하지 않는다.
========================================================================= BLOCK_HEADER_END */

Shader "XeriUI/RadialGrad"
{
    Properties
    {
        _Color0     ("Color 0", Color) = (1, 1, 1, 1)
        _Color1     ("Color 1", Color) = (0, 0, 0, 1)
        _Color2     ("Color 2", Color) = (0, 0, 0, 1)
        _Color3     ("Color 3", Color) = (0, 0, 0, 1)
        _Color4     ("Color 4", Color) = (0, 0, 0, 1)
        _Color5     ("Color 5", Color) = (0, 0, 0, 1)
        _Color6     ("Color 6", Color) = (0, 0, 0, 1)
        _Color7     ("Color 7", Color) = (0, 0, 0, 1)
        _ColorCount ("Color Count", Float) = 2
        _Stop0      ("Stop 0 (start end)", Vector) = (0, 0, 0, 0)
        _Stop1      ("Stop 1 (start end)", Vector) = (1, 1, 0, 0)
        _Stop2      ("Stop 2 (start end)", Vector) = (0, 0, 0, 0)
        _Stop3      ("Stop 3 (start end)", Vector) = (0, 0, 0, 0)
        _Stop4      ("Stop 4 (start end)", Vector) = (0, 0, 0, 0)
        _Stop5      ("Stop 5 (start end)", Vector) = (0, 0, 0, 0)
        _Stop6      ("Stop 6 (start end)", Vector) = (0, 0, 0, 0)
        _Stop7      ("Stop 7 (start end)", Vector) = (0, 0, 0, 0)
        _Center     ("Center", Vector) = (0.5, 0.5, 0, 0)
        _Radius     ("Radius", Vector) = (1, 1, 0, 0)
        _Tiling     ("Tiling", Float) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderType"         = "Transparent"
            "isCustomUITKShader" = "true"
            "Queue"              = "Transparent"
            "IgnoreProjector"    = "True"
        }

        Cull Off
        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        ZWrite Off

        Pass
        {
            Name "Default"

            HLSLPROGRAM

            #pragma target 3.5
            #pragma vertex uie_custom_vert
            #pragma fragment uie_custom_frag

            #pragma multi_compile_local _ _UIE_FORCE_GAMMA

            #pragma multi_compile_local _ _UIE_TEXTURE_SLOT_COUNT_4 _UIE_TEXTURE_SLOT_COUNT_2 _UIE_TEXTURE_SLOT_COUNT_1
            #include "Internal/UnityUIE.cginc"

            CBUFFER_START(UnityPerMaterial)
                float4 _Color0;
                float4 _Color1;
                float4 _Color2;
                float4 _Color3;
                float4 _Color4;
                float4 _Color5;
                float4 _Color6;
                float4 _Color7;
                float  _ColorCount;
                float4 _Stop0;
                float4 _Stop1;
                float4 _Stop2;
                float4 _Stop3;
                float4 _Stop4;
                float4 _Stop5;
                float4 _Stop6;
                float4 _Stop7;
                float4 _Center;
                float4 _Radius;
                float  _Tiling;
            CBUFFER_END

            struct Attributes
            {
                float3 positionOS : POSITION;
                float4 color      : COLOR;
                float4 uv0        : TEXCOORD0;
                uint4  uv4        : TEXCOORD4;
                float4 uv5        : TEXCOORD5;
                #if UNITY_ANY_INSTANCING_ENABLED
                uint instanceID   : INSTANCEID_SEMANTIC;
                #endif
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 texCoord0;
                float4 texCoord1;
                float4 texCoord2;
                float4 texCoord3;
                float4 texCoord4;
                float4 color;
                #if UNITY_ANY_INSTANCING_ENABLED
                uint instanceID   : CUSTOM_INSTANCE_ID;
                #endif
            };

            struct PackedVaryings
            {
                float4 positionCS : SV_POSITION;
                float4 texCoord0  : INTERP0;
                float4 texCoord1  : INTERP1;
                float4 texCoord2  : INTERP2;
                float4 texCoord3  : INTERP3;
                float4 texCoord4  : INTERP4;
                float4 color      : INTERP5;
                #if UNITY_ANY_INSTANCING_ENABLED
                uint instanceID   : CUSTOM_INSTANCE_ID;
                #endif
            };

            PackedVaryings PackVaryings(Varyings input)
            {
                PackedVaryings output = (PackedVaryings)0;
                output.positionCS     = input.positionCS;
                output.texCoord0.xyzw = input.texCoord0;
                output.texCoord1.xyzw = input.texCoord1;
                output.texCoord2.xyzw = input.texCoord2;
                output.texCoord3.xyzw = input.texCoord3;
                output.texCoord4.xyzw = input.texCoord4;
                output.color.xyzw     = input.color;
                return output;
            }

            Varyings UnpackVaryings(PackedVaryings input)
            {
                Varyings output;
                output.positionCS = input.positionCS;
                output.texCoord0  = input.texCoord0.xyzw;
                output.texCoord1  = input.texCoord1.xyzw;
                output.texCoord2  = input.texCoord2.xyzw;
                output.texCoord3  = input.texCoord3.xyzw;
                output.texCoord4  = input.texCoord4.xyzw;
                output.color      = input.color.xyzw;
                return output;
            }

            struct SurfaceDescriptionInputs
            {
                float4 color;
                float4 typeTexSettings;
                float2 textCoreLoc;
                float4 circle;
                float4 uvClip;
                float2 layoutUV;
            };

            struct SurfaceDescription
            {
                float3 BaseColor;
                float  Alpha;
            };

            float4 ToGradientSpace(float4 color)
            {
                #if UIE_COLORSPACE_GAMMA && !defined(UNITY_COLORSPACE_GAMMA)
                return float4(uie_linear_to_gamma(color.rgb), color.a);
                #else
                return color;
                #endif
            }

            float4 MultiGrad(float t)
            {
                float tc = saturate(t);

                float4 colors[8];
                colors[0] = _Color0; colors[1] = _Color1;
                colors[2] = _Color2; colors[3] = _Color3;
                colors[4] = _Color4; colors[5] = _Color5;
                colors[6] = _Color6; colors[7] = _Color7;

                int count = (int)_ColorCount;

                for (int j = 0; j < count; j++)
                {
                    colors[j] = ToGradientSpace(colors[j]);
                    colors[j].rgb *= colors[j].a;
                }

                float stops[8]    = { _Stop0.x, _Stop1.x, _Stop2.x, _Stop3.x, _Stop4.x, _Stop5.x, _Stop6.x, _Stop7.x };
                float stopsEnd[8] = { _Stop0.y, _Stop1.y, _Stop2.y, _Stop3.y, _Stop4.y, _Stop5.y, _Stop6.y, _Stop7.y };

                float4 c = colors[0];
                for (int i = 0; i < count - 1; i++)
                {
                    float tStart = stopsEnd[i];
                    float tEnd   = stops[i + 1];
                    float tSeg   = saturate((tc - tStart) / max(tEnd - tStart, 1e-4));
                    c = lerp(c, colors[i + 1], tSeg * step(tStart, tc));
                }

                c.rgb /= max(c.a, 1e-4);
                return c;
            }

            SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
            {
                SurfaceDescription surface = (SurfaceDescription)0;

                float2 center = float2(_Center.x, 1.0 - _Center.y);

                // CSS circle은 정규화 UV가 아니라 요소의 실제 화면 비율에서 거리를 계산한다.
                float width = rcp(max(length(float2(ddx(IN.layoutUV.x), ddy(IN.layoutUV.x))), 1e-5));
                float height = rcp(max(length(float2(ddx(IN.layoutUV.y), ddy(IN.layoutUV.y))), 1e-5));
                float2 size = float2(width, height);
                float2 position = (IN.layoutUV - center) * size;

                float d0 = length(center * size);
                float d1 = length((float2(1.0, 0.0) - center) * size);
                float d2 = length((float2(0.0, 1.0) - center) * size);
                float d3 = length((float2(1.0, 1.0) - center) * size);
                float fc = max(max(d0, d1), max(d2, d3));

                float dist = length(position / (_Radius.xy * fc));

                float t = _Tiling > 1.001 ? frac(dist * _Tiling) : saturate(dist);

                float4 grad       = MultiGrad(t);
                float4 bg         = IN.color;
                surface.BaseColor = grad.rgb * bg.rgb;
                surface.Alpha     = grad.a * bg.a;
                return surface;
            }

            SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
            {
                SurfaceDescriptionInputs output = (SurfaceDescriptionInputs)0;
                output.uvClip          = input.texCoord0;
                output.typeTexSettings = input.texCoord1;
                output.circle          = input.texCoord4;
                output.layoutUV        = input.texCoord3.zw;
                output.color           = input.color;
                return output;
            }

            #include "XeriUIGradientPass.hlsl"

            ENDHLSL
        }
    }
    FallBack off
}
