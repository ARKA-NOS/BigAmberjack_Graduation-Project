Shader "CTJ/Enemies/TemporaryOutline"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        [PerRendererData] _OutlineWidth ("Outline Width (Screen Pixels)", Range(0, 6)) = 2
        [PerRendererData] _SpriteUVRect ("Sprite UV Bounds", Vector) = (0, 0, 1, 1)
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
            #pragma vertex OutlineVertex
            #pragma fragment OutlineFragment
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
                float _OutlineWidth;
                float4 _SpriteUVRect;
            CBUFFER_END
            float4 _MainTex_TexelSize;

            Varyings OutlineVertex(Attributes input)
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SKINNED_VERTEX_COMPUTE(input);
                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);
                Varyings output = CommonUnlitVertex(input);
                output.color = input.color * unity_SpriteColor;
                return output;
            }

            half AlphaAt(float2 uv)
            {
                // 시트/아틀라스의 이웃 프레임이 테두리 검사에 섞이지 않게 현재 Sprite 밖은 투명 취급.
                if (any(uv < _SpriteUVRect.xy) || any(uv > _SpriteUVRect.zw)) return 0;
                float2 halfTexel = _MainTex_TexelSize.xy * 0.5;
                return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex,
                    clamp(uv, _SpriteUVRect.xy + halfTexel, _SpriteUVRect.zw - halfTexel)).a;
            }

            half4 OutlineFragment(Varyings input) : SV_Target
            {
                // 화면 픽셀 단위이므로 단색 사각형 터렛과 픽셀 아트 모두 같은 두께로 표시합니다.
                float2 dx = ddx(input.uv) * _OutlineWidth;
                float2 dy = ddy(input.uv) * _OutlineWidth;
                half center = AlphaAt(input.uv);
                half neighbor = min(AlphaAt(input.uv + dx), AlphaAt(input.uv - dx));
                neighbor = min(neighbor, min(AlphaAt(input.uv + dy), AlphaAt(input.uv - dy)));
                neighbor = min(neighbor, min(AlphaAt(input.uv + dx + dy), AlphaAt(input.uv - dx - dy)));
                neighbor = min(neighbor, min(AlphaAt(input.uv + dx - dy), AlphaAt(input.uv - dx + dy)));
                // 실루엣 안쪽 가장자리만 덮으므로 메쉬 확장이나 원본 머티리얼 교체가 필요 없습니다.
                half edge = _OutlineWidth > 0 ? center * (1 - neighbor) : 0;
                return half4(input.color.rgb, edge * input.color.a);
            }
            ENDHLSL
        }
    }
}
