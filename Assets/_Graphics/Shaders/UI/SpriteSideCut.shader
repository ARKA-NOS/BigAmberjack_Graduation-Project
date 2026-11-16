// 스프라이트를 왼쪽 또는 오른쪽 한쪽에서부터 슬라이더(_Amount)만큼 잘라내 보이지 않게 하는 Unlit 2D 셰이더.
// 체력/게이지 바, 로딩 바, 좌우로 열리는 연출 등에 사용한다.
Shader "Graphics/Sprite/SideCut"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        [HDR] _Color ("Tint", Color) = (1, 1, 1, 1)

        [Header(Cut)]
        [Enum(Left, 0, Right, 1)] _Side ("Cut From", Float) = 0
        _Amount ("Hidden Amount", Range(0, 1)) = 0
        _Softness ("Edge Softness", Range(0, 0.5)) = 0
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
            #pragma vertex CutVertex
            #pragma fragment CutFragment
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
                half4 _Color;
                half _Side;
                half _Amount;
                half _Softness;
            CBUFFER_END

            Varyings CutVertex(Attributes input)
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SKINNED_VERTEX_COMPUTE(input);
                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);
                Varyings output = CommonUnlitVertex(input);
                output.color = input.color * _Color * unity_SpriteColor;
                return output;
            }

            half4 CutFragment(Varyings input) : SV_Target
            {
                half4 textureColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);

                // 가로 위치 0~1 (왼쪽 0, 오른쪽 1). 스프라이트 한 장이 텍스처 전체인 경우만 정확하다.
                float x = input.uv.x;
                // 자르기 시작하는 쪽에서의 거리: 그쪽 끝이 0, 반대쪽 끝이 1
                float fromSide = lerp(x, 1.0 - x, step(0.5, _Side));

                // Amount 0이면 전부 보이고 1이면 전부 사라지도록 경계 위치를 보정한다.
                float edge = _Amount * (1.0 + _Softness) - _Softness;
                float visible = saturate((fromSide - edge) / max(_Softness, 1e-5));

                half4 color = textureColor * input.color;
                color.a *= visible;
                return color;
            }
            ENDHLSL
        }
    }
}
