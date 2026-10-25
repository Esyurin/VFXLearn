#ifndef LIGHT_BEAM_INCLUDED
#define LIGHT_BEAM_INCLUDED

// BeamCylinder 메시 기준: UV.x = 둘레(0~1), UV.y = 길이(0 = 아래 끝/검, 1 = 위 끝)
// BeamLength에 실제 길이를 넣으면 노이즈가 월드 길이 기준으로 타일링되어 늘어나지 않는다

float2 BeamNoiseUV(float2 uv, float beamLength, float noiseScale, float speed)
{
    return float2(uv.x, uv.y * beamLength * noiseScale - _Time.y * speed);
}

// 위아래 끝 페이드 (아래는 아주 짧게, 위는 TopFade 구간만큼)
float BeamFadeY(float v, float topFade)
{
    return smoothstep(0.0, 0.03, v) * smoothstep(1.0, 1.0 - max(topFade, 1e-3), v);
}

// 버텍스 단계: 길이 방향으로 굵기를 바꾸고(테이퍼), 노멀 방향으로 밀어서 실루엣 자체를 일렁이게 한다
void LightBeamVertex_float(
    float3 PositionOS, float3 NormalOS, float2 UV, UnityTexture2D NoiseTex,
    float Wobble, float WobbleSpeed, float BeamLength, float NoiseScale,
    float BottomWidth, float TopWidth,
    out float3 OutPosition)
{
    // 메시 단면 중심이 원점이므로 xz만 스케일하면 둘레가 커진다
    float width = lerp(BottomWidth, TopWidth, UV.y);
    float3 pos = float3(PositionOS.x * width, PositionOS.y, PositionOS.z * width);

    float2 nuv = BeamNoiseUV(UV, BeamLength, NoiseScale, WobbleSpeed);
    float n = SAMPLE_TEXTURE2D_LOD(NoiseTex.tex, NoiseTex.samplerstate, nuv, 0).r;
    // 아래 끝(검 쪽)은 덜 흔들리게, 굵은 쪽은 굵기에 비례해서 더 흔들리게
    float anchor = smoothstep(0.0, 0.15, UV.y);
    OutPosition = pos + NormalOS * (n - 0.5) * Wobble * anchor * width;
}

// 프래그먼트: 프레넬로 흰 코어 + 보라 글로우
void LightBeam_float(
    float2 UV, float3 NormalWS, float3 ViewDirWS, UnityTexture2D NoiseTex,
    float4 CoreColor, float CoreIntensity, float CorePower,
    float4 GlowColor, float Intensity, float GlowPower,
    float FlowSpeed, float BeamLength, float NoiseScale, float TopFade,
    float StreakAmount, float StreakScale,
    out float3 Color, out float Alpha)
{
    float f = saturate(dot(normalize(NormalWS), normalize(ViewDirWS)));

    float2 nuv = BeamNoiseUV(UV, BeamLength, NoiseScale, FlowSpeed);
    float flow = SAMPLE_TEXTURE2D(NoiseTex.tex, NoiseTex.samplerstate, nuv).g;

    // B 채널 = 세로 빛 가닥. StreakScale이 작을수록 가닥이 길게 늘어난다
    float2 suv = BeamNoiseUV(UV, BeamLength, StreakScale, FlowSpeed);
    float streak = SAMPLE_TEXTURE2D(NoiseTex.tex, NoiseTex.samplerstate, suv).b;

    // 코어는 덜 쪼개서 중심 밝기를 유지하고, 글로우는 가닥으로 쪼갠다
    float core = pow(f, CorePower) * CoreIntensity * lerp(1.0 - StreakAmount * 0.5, 1.0, streak);
    float glow = pow(f, GlowPower) * Intensity * lerp(0.6, 1.0, flow) * lerp(1.0 - StreakAmount, 1.0, streak);
    float fade = BeamFadeY(UV.y, TopFade);

    Color = (core * CoreColor.rgb + glow * GlowColor.rgb) * fade;
    // 실루엣 쪽은 빨리 꺼지게 해서 딱딱한 가장자리를 숨긴다
    Alpha = saturate(sqrt(f) * fade);
}

// 왜곡: 실루엣 쪽일수록 화면 UV를 노멀 방향으로 밀어 배경을 일렁이게 한다 (Scene Color에 연결)
void LightBeamDistortion_float(
    float4 ScreenPos, float3 NormalWS, float3 ViewDirWS, float2 UV, UnityTexture2D NoiseTex,
    float Strength, float FlowSpeed, float BeamLength, float NoiseScale, float TopFade, float EdgePower,
    out float2 DistortedUV, out float Alpha)
{
    float3 n = normalize(NormalWS);
    float f = saturate(dot(n, normalize(ViewDirWS)));
    float edge = pow(1.0 - f, EdgePower);

    // 고주파 성분이 남아 있으면 배경 경계가 톱니처럼 끊어지므로 흐린 밉(LOD 3 = 32px)에서 샘플링한다.
    // R/G를 x/y에 따로 써서 한 방향 대각선으로만 밀리지 않게 한다
    float2 nuv = BeamNoiseUV(UV, BeamLength, NoiseScale, FlowSpeed);
    float2 noise = SAMPLE_TEXTURE2D_LOD(NoiseTex.tex, NoiseTex.samplerstate, nuv, 3).rg - 0.5;

    float2 normalVS = mul((float3x3)UNITY_MATRIX_V, n).xy;
    float fade = BeamFadeY(UV.y, TopFade);
    float amount = Strength * fade * (0.4 + edge);

    DistortedUV = ScreenPos.xy + (normalVS * 0.5 + noise) * amount;
    // 배경을 그대로 다시 그리는 것이므로 메시 경계가 보이지 않게 가장자리에서 페이드
    Alpha = saturate(f * 3.0) * fade;
}

#endif
