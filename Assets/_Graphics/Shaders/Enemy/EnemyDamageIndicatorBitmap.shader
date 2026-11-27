Shader "Enemy/DamageIndicatorBitmap"
{
    Properties
    {
        _MainTex ("Font Atlas", 2D) = "white" {}
        _Color ("Text Color", Color) = (1,1,1,1)
        _StrokePixels ("Stroke Expansion (Atlas Pixels)", Range(0,1)) = 0.25
        _DiffusePower ("Diffuse Power", Range(1,4)) = 1
        _VertexOffsetX ("Vertex Offset X", Float) = 0
        _VertexOffsetY ("Vertex Offset Y", Float) = 0
        _MaskSoftnessX ("Mask Softness X", Float) = 0
        _MaskSoftnessY ("Mask Softness Y", Float) = 0
        _ClipRect ("Clip Rect", Vector) = (-32767,-32767,32767,32767)

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _CullMode ("Cull Mode", Float) = 0
        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull [_CullMode]
        ZTest [unity_GUIZTestMode]
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            fixed4 _Color;
            float _StrokePixels, _DiffusePower;
            float _VertexOffsetX, _VertexOffsetY;
            float4 _ClipRect;
            float _MaskSoftnessX, _MaskSoftnessY, _UIMaskSoftnessX, _UIMaskSoftnessY;
            int _UIVertexColorAlwaysGammaSpace;

            struct Attributes
            {
                float4 vertex : POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
                float4 mask : TEXCOORD1;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                input.vertex.xy += float2(_VertexOffsetX, _VertexOffsetY);
                output.vertex = UnityPixelSnap(UnityObjectToClipPos(input.vertex));
                output.uv = input.uv;

                if (_UIVertexColorAlwaysGammaSpace && !IsGammaSpace())
                    input.color.rgb = UIGammaToLinear(input.color.rgb);

                output.color = input.color * _Color;
                output.color.rgb *= _DiffusePower;
                float4 rect = clamp(_ClipRect, -2e10, 2e10);
                float2 softness = max(float2(_UIMaskSoftnessX, _UIMaskSoftnessY), float2(_MaskSoftnessX, _MaskSoftnessY));
                output.mask = float4(input.vertex.xy * 2 - rect.xy - rect.zw,
                    0.25 / (0.25 * softness + output.vertex.w));

                return output;
            }

            fixed4 Frag(Varyings input) : SV_Target
            {
                float alpha = tex2D(_MainTex, input.uv).a;

                if (_StrokePixels > 0)
                {
                    float2 step = _MainTex_TexelSize.xy * _StrokePixels;
                    alpha = max(alpha, tex2D(_MainTex, input.uv + float2(step.x, 0)).a);
                    alpha = max(alpha, tex2D(_MainTex, input.uv - float2(step.x, 0)).a);
                    alpha = max(alpha, tex2D(_MainTex, input.uv + float2(0, step.y)).a);
                    alpha = max(alpha, tex2D(_MainTex, input.uv - float2(0, step.y)).a);
                    alpha = max(alpha, tex2D(_MainTex, input.uv + step).a);
                    alpha = max(alpha, tex2D(_MainTex, input.uv - step).a);
                    alpha = max(alpha, tex2D(_MainTex, input.uv + float2(step.x, -step.y)).a);
                    alpha = max(alpha, tex2D(_MainTex, input.uv + float2(-step.x, step.y)).a);
                }

                fixed4 color = fixed4(input.color.rgb, input.color.a * alpha);

                #ifdef UNITY_UI_CLIP_RECT
                    float2 mask = saturate((_ClipRect.zw - _ClipRect.xy - abs(input.mask.xy)) * input.mask.zw);
                    color.a *= mask.x * mask.y;
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                    clip(color.a - 0.001);
                #endif

                return color;
            }
            ENDCG
        }
    }
}
