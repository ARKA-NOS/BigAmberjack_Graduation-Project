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
                    // 최종 출력 해상도 기준입니다. URP Render Scale을 낮춰도 표시 두께는 같습니다.
                    float2 screen = _ScreenParams.xy * 0.5;
                    float2 pixelsPerUnit = float2(length((clipX.xy / clipX.w - clip.xy / clip.w) * screen),
                        length((clipY.xy / clipY.w - clip.xy / clip.w) * screen));
                    // 매우 얇거나 회전한 파츠에서도 래스터화 경계가 잘리지 않게 여유를 둡니다.
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
                // ddx/ddy는 내부 렌더 타깃의 1픽셀입니다. 최종 화면의 1픽셀로 환산합니다.
                float2 pixelRatio = GetScaledScreenParams().xy / max(_ScreenParams.xy, 1);
                float2 dx = ddx(input.uv) * pixelRatio.x;
                float2 dy = ddy(input.uv) * pixelRatio.y;
                half center = AlphaAt(input.uv);
                half outer = center;
                half inner = center;
                // 16방향을 모두 같은 반지름으로 검사합니다. dx+dy의 대각선은 sqrt(2)배
                // 멀리 검사하던 문제를 없애, 경계 방향/모델 크기에 따라 두께가 변하지 않습니다.
                static const float2 directions[16] = {
                    float2(1,0), float2(0.9238795,0.3826834), float2(0.7071068,0.7071068), float2(0.3826834,0.9238795),
                    float2(0,1), float2(-0.3826834,0.9238795), float2(-0.7071068,0.7071068), float2(-0.9238795,0.3826834),
                    float2(-1,0), float2(-0.9238795,-0.3826834), float2(-0.7071068,-0.7071068), float2(-0.3826834,-0.9238795),
                    float2(0,-1), float2(0.3826834,-0.9238795), float2(0.7071068,-0.7071068), float2(0.9238795,-0.3826834)
                };
                int steps = (int)ceil(_OutlineWidth);
                [loop] for (int step = 1; step <= steps; step++)
                {
                    float radius = min((float)step, _OutlineWidth);
                    [unroll] for (int direction = 0; direction < 16; direction++)
                    {
                        float2 offset = (dx * directions[direction].x + dy * directions[direction].y) * radius;
                        half sampleAlpha = AlphaAt(input.uv + offset);
                        outer = max(outer, sampleAlpha);
                        inner = min(inner, sampleAlpha);
                    }
                }
                half edge = _OuterOutline > 0.5 ? outer * (1 - center) : center * (1 - inner);
                if (_OutlineWidth <= 0) edge = 0;
                return half4(input.color.rgb, edge * input.color.a);
            }
            ENDHLSL
        }
    }
}
