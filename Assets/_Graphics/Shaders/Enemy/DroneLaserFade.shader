Shader "Enemy/DroneLaserFade"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _BeamOrigin ("Beam Origin", Vector) = (0,0,0,0)
        _BeamDirection ("Beam Direction", Vector) = (0,-1,0,0)
        _BeamLength ("Beam Length", Float) = 7.5
        _EndFadeDistance ("End Fade Distance", Float) = 1
        _FloorY ("Floor Height", Float) = -1000
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Cull Off
        ZWrite Off
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float4 _BeamOrigin, _BeamDirection;
                float _BeamLength, _EndFadeDistance, _FloorY;
            CBUFFER_END
            struct Attributes { float3 positionOS : POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float3 positionWS : TEXCOORD1; half4 color : COLOR; };
            Varyings Vert(Attributes input)
            {
                UNITY_SETUP_INSTANCE_ID(input);
                SetUpSpriteInstanceProperties();
                Varyings output;
                output.positionWS = TransformObjectToWorld(UnityFlipSprite(input.positionOS, unity_SpriteProps.xy));
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.uv = input.uv;
                output.color = input.color * _Color * unity_SpriteColor;
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * input.color;
                float remaining = _BeamLength - dot(input.positionWS.xy - _BeamOrigin.xy, _BeamDirection.xy);
                float fadeDistance = max(0.01, _EndFadeDistance);
                color.a *= smoothstep(0, fadeDistance, remaining) * smoothstep(_FloorY, _FloorY + fadeDistance, input.positionWS.y);
                return color;
            }
            ENDHLSL
        }
    }
}
