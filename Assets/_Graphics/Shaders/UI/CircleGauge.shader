// 원형 HP/MP 게이지 셰이더 (UI Image용, Unlit).
// 도넛 모양 트랙 위에 왼쪽 반원은 HP, 오른쪽 반원은 MP가 아래쪽 끝에서 위쪽 끝으로 차오른다.
// _Hide를 올리면 원의 중심에서 바깥쪽으로 링이 안쪽 가장자리부터 사라진다.
//
// 사용법: Image의 Sprite는 비워 두고(전체 UV 0~1을 쓰는 사각형이어야 한다) Material에 이 셰이더를 지정한다.
// Image는 가로·세로가 같은 정사각형이어야 원이 찌그러지지 않는다.
Shader "Graphics/UI/CircleGauge"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}

        [Header(Gauge)]
        _HpFill ("HP Fill (Left)", Range(0, 1)) = 1
        _MpFill ("MP Fill (Right)", Range(0, 1)) = 1
        _HpColor ("HP Color", Color) = (0.941, 0.114, 0.165, 1)
        _MpColor ("MP Color", Color) = (0, 0.706, 0.906, 1)
        _TrackColor ("Track Color", Color) = (0.157, 0.157, 0.196, 1)

        [Header(Hide)]
        // 0이면 전부 보이고 1이면 중심에서 바깥까지 전부 사라진다.
        _Hide ("Hidden Amount (Center to Outside)", Range(0, 1)) = 0

        [Header(Pixel Art)]
        // Image 가로 한 변을 몇 칸으로 쪼갤지. 1 이상이면 픽셀 도트 모양(계단 + 안티앨리어싱 없음)으로 그린다.
        // 0이면 부드러운 링. 중심이 칸 경계에 오도록 짝수를 쓴다. ui-HUD(32x32)와 같은 크기로 겹치면 32.
        _PixelCount ("Pixel Count (0 = Smooth)", Range(0, 256)) = 0

        [Header(Ring Size)]
        // 반지름은 Image 한 변을 1로 본 값이다. 기본값은 HTML(200 기준)의 트랙 76~98, 게이지 78.5~95.5.
        _TrackInner ("Track Inner Radius", Range(0, 0.5)) = 0.38
        _TrackOuter ("Track Outer Radius", Range(0, 0.5)) = 0.49
        _FillInner ("Fill Inner Radius", Range(0, 0.5)) = 0.3925
        _FillOuter ("Fill Outer Radius", Range(0, 0.5)) = 0.4775
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
            #pragma vertex GaugeVertex
            #pragma fragment GaugeFragment
            #pragma multi_compile_instancing
            #pragma multi_compile _ SKINNED_SPRITE

            struct Attributes
            {
                COMMON_2D_INPUTS
                half4 color : COLOR;
                UNITY_SKINNED_VERTEX_INPUTS
            };
            struct Varyings
            {
                COMMON_2D_OUTPUTS
                half4 color : COLOR;
            };

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/2DCommon.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half _HpFill;
                half _MpFill;
                half4 _HpColor;
                half4 _MpColor;
                half4 _TrackColor;
                half _Hide;
                float _PixelCount;
                half _TrackInner;
                half _TrackOuter;
                half _FillInner;
                half _FillOuter;
            CBUFFER_END

            Varyings GaugeVertex(Attributes input)
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SKINNED_VERTEX_COMPUTE(input);
                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);
                Varyings output = CommonUnlitVertex(input);
                output.color = input.color * unity_SpriteColor;
                return output;
            }

            // 반지름 r이 [inner, outer] 안이면 1, 가장자리는 w(한 픽셀 폭)만큼 부드럽게 처리한다.
            float RingMask(float r, float inner, float outer, float w)
            {
                return saturate((r - inner) / w + 0.5) * saturate((outer - r) / w + 0.5);
            }

            // 진행도 t가 fill보다 작으면 1. fill이 0이면 아무것도 칠하지 않는다.
            float FillMask(float t, float fill, float w)
            {
                return saturate((fill - t) / w + 0.5) * step(0.0001, fill);
            }

            half4 GaugeFragment(Varyings input) : SV_Target
            {
                // 픽셀 모드: UV를 칸 중심으로 맞춰서 한 칸 안은 모두 같은 값이 되게 한다.
                float pixelMode = step(1.0, _PixelCount);
                float2 snapped = (floor(input.uv * _PixelCount) + 0.5) / max(_PixelCount, 1.0);
                float2 p = lerp(input.uv, snapped, pixelMode) - 0.5;
                float r = length(p);

                // 아래쪽 끝 0 → 옆 끝 0.5 → 위쪽 끝 1. 좌우 반원에 같은 값을 쓴다.
                float t = atan2(abs(p.x), -p.y) / PI;

                // 가장자리 폭: 부드러운 모드는 한 픽셀, 픽셀 모드는 딱 잘리게 거의 0.
                float rw = lerp(max(fwidth(r), 1e-5), 1e-5, pixelMode);
                float tw = lerp(max(fwidth(t), 1e-5), 1e-5, pixelMode);

                float track = RingMask(r, _TrackInner, _TrackOuter, rw);
                float fillRing = RingMask(r, _FillInner, _FillOuter, rw);

                float isLeft = step(p.x, 0.0);
                float hp = fillRing * isLeft * FillMask(t, _HpFill, tw);
                float mp = fillRing * (1.0 - isLeft) * FillMask(t, _MpFill, tw);

                half3 rgb = _TrackColor.rgb;
                rgb = lerp(rgb, _HpColor.rgb, hp);
                rgb = lerp(rgb, _MpColor.rgb, mp);
                float alpha = track * lerp(_TrackColor.a, 1.0, max(hp, mp));

                // 중심에서 바깥으로 숨김: 숨김 반지름 안쪽은 지운다.
                // 0일 때 안쪽 가장자리가 깎이지 않도록 양끝에 여유(2px)를 둔다.
                float hideRadius = lerp(_TrackInner - 2.0 * rw, _TrackOuter + 2.0 * rw, _Hide);
                float visible = saturate((r - hideRadius) / rw + 0.5);

                half4 color = half4(rgb, alpha * visible);
                return color * input.color;
            }
            ENDHLSL
        }
    }
}
