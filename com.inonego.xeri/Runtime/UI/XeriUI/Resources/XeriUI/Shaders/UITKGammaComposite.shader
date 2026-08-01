Shader "Hidden/XeriUI/UITKGammaComposite"
{
    HLSLINCLUDE
    #include "UnityCG.cginc"

    sampler2D _BlitTexture;
    float4 _BlitScaleBias;

    struct Attributes
    {
        uint vertexID : SV_VertexID;
    };

    struct Varyings
    {
        float4 positionCS : SV_POSITION;
        float2 texcoord : TEXCOORD0;
    };

    Varyings Vert(Attributes input)
    {
        Varyings output;
        float2 vertex = float2((input.vertexID << 1) & 2, input.vertexID & 2);
        output.positionCS = float4(vertex * 2.0 - 1.0, 0.0, 1.0);
        output.texcoord = vertex * _BlitScaleBias.xy + _BlitScaleBias.zw;
        return output;
    }

    float4 SampleBlitTexture(Varyings input)
    {
        return tex2D(_BlitTexture, input.texcoord);
    }

    float4 SampleGammaToLinear(Varyings input)
    {
        float4 color = SampleBlitTexture(input);

        #ifndef UNITY_COLORSPACE_GAMMA
        color.r = GammaToLinearSpaceExact(color.r);
        color.g = GammaToLinearSpaceExact(color.g);
        color.b = GammaToLinearSpaceExact(color.b);
        #endif

        return color;
    }
    ENDHLSL

    SubShader
    {
        Tags
        {
            "Queue" = "Overlay"
            "RenderType" = "Transparent"
        }

        ZTest Always
        ZWrite Off
        Cull Off

        Pass
        {
            Name "GammaToLinearBlend"
            Blend One OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag

            float4 Frag(Varyings input) : SV_Target
            {
                return SampleGammaToLinear(input);
            }
            ENDHLSL
        }

    }
}
