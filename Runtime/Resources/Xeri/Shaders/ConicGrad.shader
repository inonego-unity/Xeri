Shader "Xeri/UIConicGrad"
{
    Properties
    {
        _Color0     ("Color 0", Color) = (1, 0, 0, 1)
        _Color1     ("Color 1", Color) = (0, 0, 1, 1)
        _Color2     ("Color 2", Color) = (0, 1, 0, 1)
        _Color3     ("Color 3", Color) = (1, 1, 0, 1)
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
        _Angle      ("From Angle (deg)", Float) = 0
        _Center     ("Center", Vector) = (0.5, 0.5, 0, 0)
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"     = "UniversalPipeline"
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
            #pragma multi_compile_local _ _UIE_RENDER_TYPE_SOLID _UIE_RENDER_TYPE_TEXTURE _UIE_RENDER_TYPE_TEXT _UIE_RENDER_TYPE_GRADIENT

            #define UITK_SHADERGRAPH
            #define _SURFACE_TYPE_TRANSPARENT 1
            #define ATTRIBUTES_NEED_TEXCOORD0
            #define ATTRIBUTES_NEED_TEXCOORD1
            #define ATTRIBUTES_NEED_TEXCOORD2
            #define ATTRIBUTES_NEED_TEXCOORD3
            #define ATTRIBUTES_NEED_COLOR
            #define VARYINGS_NEED_TEXCOORD0
            #define VARYINGS_NEED_TEXCOORD1
            #define VARYINGS_NEED_TEXCOORD3
            #define VARYINGS_NEED_COLOR
            #define FEATURES_GRAPH_VERTEX

            #define SHADERPASS SHADERPASS_CUSTOM_UI

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
            #include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/ShaderLibrary/Shim/UIShim.hlsl"

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
                float  _Angle;
                float4 _Center;
            CBUFFER_END

            struct Attributes
            {
                float3 positionOS : POSITION;
                float4 color      : COLOR;
                float4 uv0        : TEXCOORD0;
                float4 uv1        : TEXCOORD1;
                float4 uv2        : TEXCOORD2;
                float4 uv3        : TEXCOORD3;
                float4 uv4        : TEXCOORD4;
                float4 uv5        : TEXCOORD5;
                float4 uv6        : TEXCOORD6;
                float4 uv7        : TEXCOORD7;
                #if UNITY_ANY_INSTANCING_ENABLED
                uint instanceID   : INSTANCEID_SEMANTIC;
                #endif
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 texCoord0;
                float4 texCoord1;
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
                float4 texCoord3  : INTERP2;
                float4 texCoord4  : INTERP3;
                float4 color      : INTERP4;
                #if UNITY_ANY_INSTANCING_ENABLED
                uint instanceID   : CUSTOM_INSTANCE_ID;
                #endif
            };

            PackedVaryings PackVaryings(Varyings input)
            {
                PackedVaryings output;
                ZERO_INITIALIZE(PackedVaryings, output);
                output.positionCS     = input.positionCS;
                output.texCoord0.xyzw = input.texCoord0;
                output.texCoord1.xyzw = input.texCoord1;
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
                output.texCoord3  = input.texCoord3.xyzw;
                output.texCoord4  = input.texCoord4.xyzw;
                output.color      = input.color.xyzw;
                return output;
            }

            struct VertexDescriptionInputs
            {
                float4 vertexPosition;
                float4 vertexColor;
                float4 uv;
                float4 xformClipPages;
                float4 ids;
                float4 flags;
                float4 opacityColorPages;
                float4 settingIndex;
                float4 circle;
            };

            struct VertexDescription {};

            VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
            {
                return (VertexDescription)0;
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

            #ifndef UNITY_PI
            #define UNITY_PI 3.14159265358979
            #endif

            #ifndef UNITY_COLORSPACE_GAMMA
            float4 ToSRGB(float4 c) { return float4(LinearToSRGB(c.rgb), c.a); }
            #endif

            float4 MultiGrad(float t)
            {
                float tc = frac(t);

                float4 colors[8];
                #ifdef UNITY_COLORSPACE_GAMMA
                colors[0] = _Color0; colors[1] = _Color1;
                colors[2] = _Color2; colors[3] = _Color3;
                colors[4] = _Color4; colors[5] = _Color5;
                colors[6] = _Color6; colors[7] = _Color7;
                #else
                colors[0] = ToSRGB(_Color0); colors[1] = ToSRGB(_Color1);
                colors[2] = ToSRGB(_Color2); colors[3] = ToSRGB(_Color3);
                colors[4] = ToSRGB(_Color4); colors[5] = ToSRGB(_Color5);
                colors[6] = ToSRGB(_Color6); colors[7] = ToSRGB(_Color7);
                #endif

                int count = (int)_ColorCount;

                for (int j = 0; j < count; j++)
                {
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

                float wrapStart = stopsEnd[count - 1];
                float tWrap = saturate((tc - wrapStart) / max(1.0 - wrapStart, 1e-4));
                c = lerp(c, colors[0], tWrap * step(wrapStart, tc));

                c.rgb /= max(c.a, 1e-4);
                return c;
            }

            SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
            {
                SurfaceDescription surface = (SurfaceDescription)0;

                float2 uv = IN.layoutUV - float2(_Center.x, 1.0 - _Center.y);
                float offset = _Angle * (UNITY_PI / 180.0);
                float angle = atan2(uv.x, uv.y) - offset;
                float t = frac(angle / (2.0 * UNITY_PI));

                float4 grad       = MultiGrad(t);
                float4 bg         = IN.color;
                surface.BaseColor = grad.rgb * bg.rgb;
                surface.Alpha     = grad.a * bg.a;
                return surface;
            }

            VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
            {
                return (VertexDescriptionInputs)0;
            }

            SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
            {
                SurfaceDescriptionInputs output;
                ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
                output.uvClip          = input.texCoord0;
                output.typeTexSettings = input.texCoord1;
                output.circle          = input.texCoord4;
                output.layoutUV        = input.texCoord3.zw;
                output.color           = input.color;
                return output;
            }

            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/UITKPass.hlsl"

            ENDHLSL
        }
    }
    CustomEditor "UnityEditor.ShaderGraph.GenericShaderGraphMaterialGUI"
    FallBack off
}
