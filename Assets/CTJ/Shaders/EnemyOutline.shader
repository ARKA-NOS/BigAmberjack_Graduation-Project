Shader "CTJ/Enemies/TemporaryOutline"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        [PerRendererData] _OutlineWidth ("Outline Width (Screen Pixels)", Range(0, 6)) = 2
        [PerRendererData] _SpriteUVRect ("Sprite UV Bounds", Vector) = (0, 0, 1, 1)
        [PerRendererData] _SpriteLocalRect ("Sprite Local Center / Size", Vector) = (0, 0, 1, 1)
        [PerRendererData] _OuterOutline ("Draw Outside Behind Parts", Float) = 0
    }
    SubShader
    {
        // 꼭짓점 확장은 Sprite의 로컬 좌표를 사용하므로 월드 좌표로 합치는 동적 배칭을 막습니다.
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "DisableBatching"="True" }
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
                float4 _SpriteLocalRect;
                float _OuterOutline;
            CBUFFER_END
            float4 _MainTex_TexelSize;

            Varyings OutlineVertex(Attributes input)
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SKINNED_VERTEX_COMPUTE(input);
                SetUpSpriteInstanceProperties();
                float2 corner = sign(input.positionOS.xy - _SpriteLocalRect.xy);
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);
                if (_OuterOutline > 0.5)
                {
                    // Full Rect의 네 꼭짓점을 화면 픽셀 두께만큼 확장하고 UV도 같이 늘립니다.
                    // 원본 SpriteRenderer/스프라이트 에셋의 크기와 콜라이더는 건드리지 않습니다.
                    float4 clip = TransformObjectToHClip(input.positionOS);
                    float4 clipX = TransformObjectToHClip(input.positionOS + float3(1, 0, 0));
                    float4 clipY = TransformObjectToHClip(input.positionOS + float3(0, 1, 0));
                    float2 screen = GetScaledScreenParams().xy * 0.5;
                    float2 pixelsPerUnit = float2(length((clipX.xy / clipX.w - clip.xy / clip.w) * screen),
                        length((clipY.xy / clipY.w - clip.xy / clip.w) * screen));
                    // 대각선 샘플의 화면 거리까지 포함해 회전한 파츠의 선도 잘리지 않게 합니다.
                    float2 expand = (_OutlineWidth * 1.414214 + 1) / max(pixelsPerUnit, 0.001);
                    input.positionOS.xy += corner * unity_SpriteProps.xy * expand;
                    input.uv += corner * expand / max(_SpriteLocalRect.zw, 0.0001) * (_SpriteUVRect.zw - _SpriteUVRect.xy);
                }
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
                if (_OuterOutline > 0.5)
                {
                    half outer = 0;
                    // 최대 거리만 검사하면 얇은 받침/포신을 건너뛰어 선에 틈이 생깁니다.
                    // 안쪽 1픽셀부터 지정 두께까지 검사해 경계 바로 옆도 채웁니다.
                    int steps = (int)ceil(_OutlineWidth);
                    [loop] for (int step = 1; step <= steps; step++)
                    {
                        float fraction = min(step / max(_OutlineWidth, 0.001), 1);
                        float2 sx = dx * fraction, sy = dy * fraction;
                        outer = max(outer, max(AlphaAt(input.uv + sx), AlphaAt(input.uv - sx)));
                        outer = max(outer, max(AlphaAt(input.uv + sy), AlphaAt(input.uv - sy)));
                        outer = max(outer, max(AlphaAt(input.uv + sx + sy), AlphaAt(input.uv - sx - sy)));
                        outer = max(outer, max(AlphaAt(input.uv + sx - sy), AlphaAt(input.uv - sx + sy)));
                    }
                    return half4(input.color.rgb, (_OutlineWidth > 0 ? outer * (1 - center) : 0) * input.color.a);
                }
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
