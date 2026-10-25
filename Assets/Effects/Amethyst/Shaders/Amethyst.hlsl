#ifndef AMETHYST_INCLUDED
#define AMETHYST_INCLUDED

// 노멀 방향으로 화면 UV를 밀어 뒤 배경을 일그러뜨린다 (Scene Color 노드에 연결)
void AmethystRefractUV_float(float4 ScreenPos, float3 NormalWS, float Strength, out float2 UV)
{
    float3 normalVS = mul((float3x3)UNITY_MATRIX_V, NormalWS);
    UV = ScreenPos.xy + normalVS.xy * Strength;
}

// MaskTex: R = 결정면 밝기, G = 균열/결, B = 큰 색 얼룩
// RampTex: 어두운 남보라 -> 흰보라 그라디언트 (U 방향)
void Amethyst_float(
    float2 UV, float3 NormalWS, float3 ViewDirWS, float3 SceneColor,
    UnityTexture2D MaskTex, UnityTexture2D RampTex,
    float4 Tint, float4 RimColor, float4 CrackColor,
    float FresnelPower, float Parallax, float Refraction, float MinAlpha,
    out float3 Color, out float Alpha)
{
    float3 n = normalize(NormalWS);
    float3 v = normalize(ViewDirWS);
    // 양면 렌더링이라 뒷면도 같은 프레넬이 나오도록 abs
    float fresnel = pow(1.0 - saturate(abs(dot(n, v))), FresnelPower);

    float4 surface = SAMPLE_TEXTURE2D(MaskTex.tex, MaskTex.samplerstate, UV);
    // 시선 방향으로 밀린 두 번째 샘플 = 안쪽에 비치는 결
    float2 innerUV = UV * 0.8 + (v.xy - n.xy) * Parallax;
    float4 inner = SAMPLE_TEXTURE2D(MaskTex.tex, MaskTex.samplerstate, innerUV);

    float facet = surface.r;
    float crack = max(surface.g, inner.g * 0.6);
    float rampU = saturate(facet * 0.55 + surface.b * 0.25 + fresnel * 0.4);
    float3 body = SAMPLE_TEXTURE2D(RampTex.tex, RampTex.samplerstate, float2(rampU, 0.5)).rgb * Tint.rgb;

    // 굴절된 배경을 보라로 물들여 몸체와 섞는다 (Refraction이 클수록 더 투명해 보임)
    float3 refracted = SceneColor * Tint.rgb;
    Color = lerp(body, refracted, Refraction * (1.0 - fresnel))
          + crack * CrackColor.rgb
          + fresnel * RimColor.rgb;

    Alpha = saturate(lerp(MinAlpha, 1.0, max(fresnel, facet * 0.5)) * Tint.a);
}

#endif
