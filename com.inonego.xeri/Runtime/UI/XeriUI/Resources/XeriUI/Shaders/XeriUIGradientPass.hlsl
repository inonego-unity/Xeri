/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : XeriUIGradientPass.hlsl
수정일 : 2026-08-01

# 설명
Unity UI Toolkit의 내장 Vertex, Clip, Arc 처리를 Xeri Gradient Surface 함수에 연결한다.

# 특이사항, 제약사항
특정 Scriptable Render Pipeline의 ShaderLibrary를 참조하지 않는다.
========================================================================= BLOCK_HEADER_END */

#ifndef XERI_UI_GRADIENT_PASS_INCLUDED
#define XERI_UI_GRADIENT_PASS_INCLUDED

PackedVaryings uie_custom_vert(Attributes input)
{
    appdata_t uieInput = (appdata_t)0;
    uieInput.vertex = float4(input.positionOS, 1.0f);
    uieInput.color = input.color;
    uieInput.uv = input.uv0;
    uieInput.packedIds = input.uv4;
    uieInput.circle = input.uv5;

    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, uieInput);

    v2f uieOutput = uie_std_vert(uieInput);
    Varyings output = (Varyings)0;
    output.positionCS = uieOutput.pos;
    output.texCoord0 = uieOutput.uvClip;
    output.texCoord1 = uieOutput.typeTexSettings;
    output.texCoord2 = float4(uieOutput.textCoreLoc, (float)(input.uv4.z & 0x7u), 0.0f);
    output.texCoord3 = float4(0.0f, 0.0f, input.uv0.z, input.uv0.w);
    output.texCoord4 = uieOutput.circle;
    output.color = uieOutput.color;

    return PackVaryings(output);
}

UIE_FRAG_T uie_custom_frag(PackedVaryings packedInput) : SV_Target
{
    Varyings input = UnpackVaryings(packedInput);
    SurfaceDescriptionInputs surfaceInput = BuildSurfaceDescriptionInputs(input);
    uint vertexType = (uint)round(input.texCoord2.z);

    // Custom Material은 하위 Draw에도 유지될 수 있으므로 원본 Solid만 Xeri Surface로 처리한다.
    if (vertexType != (uint)k_VertTypeSolid)
    {
        v2f standardInput = (v2f)0;
        standardInput.pos = input.positionCS;
        standardInput.color = input.color;
        standardInput.uvClip = input.texCoord0;
        standardInput.typeTexSettings = input.texCoord1;
        #ifdef UNITY_PLATFORM_WEBGL
        standardInput.textCoreLoc = input.texCoord2.xy;
        #else
        standardInput.textCoreLoc = (uint2)round(input.texCoord2.xy);
        #endif
        standardInput.circle = input.texCoord4;
        return uie_std_frag(standardInput);
    }

    SurfaceDescription surface = SurfaceDescriptionFunction(surfaceInput);
    half renderType = round(surfaceInput.typeTexSettings.x);

    // UI Toolkit이 제공하는 Clip Rect와 둥근 모서리 Coverage를 Custom Surface에도 적용한다.
    float coverage = uie_sg_compute_aa_coverage
    (
        renderType,
        surfaceInput.typeTexSettings.w,
        surfaceInput.circle.xy,
        surfaceInput.circle.zw
    );
    coverage *= uie_fragment_clip(surfaceInput.uvClip.zw);
    clip(coverage - 0.003f);

    surface.Alpha *= coverage;
    return UIE_FRAG_T(surface.BaseColor, surface.Alpha);
}

#endif
