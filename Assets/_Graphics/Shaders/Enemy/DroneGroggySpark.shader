Shader "Enemy/DroneGroggySpark"
{
    Properties
    {
        _GlowStrength ("Soft Glow", Range(0, 3)) = 0.7
        _CoreStrength ("Bright Core", Range(0, 4)) = 1.8
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha One
        ZWrite Off
        Cull Off
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half _GlowStrength;
                half _CoreStrength;
            CBUFFER_END

            struct Attributes { float3 positionOS : POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 centeredUV = input.uv * 2 - 1;
                half radial = saturate(1 - dot(centeredUV, centeredUV));
                half halo = radial * radial;
                half core = pow(radial, 12);
                half brightness = _GlowStrength * halo + _CoreStrength * core;
                return half4(input.color.rgb * brightness, input.color.a * halo);
            }
            ENDHLSL
        }
    }
}
