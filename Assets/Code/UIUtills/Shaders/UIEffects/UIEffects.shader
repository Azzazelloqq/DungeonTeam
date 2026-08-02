Shader "DungeonTeam/UI/Effects"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)

        [Toggle(UI_EFFECT_ROUNDED)] _Rounded ("Rounded Corners", Float) = 0
        _TopLeftRadius ("Top Left Radius (screen px)", Range(0, 512)) = 0
        _TopRightRadius ("Top Right Radius (screen px)", Range(0, 512)) = 0
        _BottomRightRadius ("Bottom Right Radius (screen px)", Range(0, 512)) = 0
        _BottomLeftRadius ("Bottom Left Radius (screen px)", Range(0, 512)) = 0

        [Toggle(UI_EFFECT_GRADIENT)] _Gradient ("Gradient", Float) = 0
        _GradientColorA ("Gradient Start Color", Color) = (1, 1, 1, 1)
        _GradientColorB ("Gradient End Color", Color) = (1, 1, 1, 1)
        _GradientDirection ("Gradient Direction", Vector) = (0, 1, 0, 0)
        _GradientStart ("Gradient Start", Range(0, 1)) = 0
        _GradientEnd ("Gradient End", Range(0, 1)) = 1

        [Toggle(UI_EFFECT_MASK)] _Mask ("Alpha Mask", Float) = 0
        [NoScaleOffset] _MaskTex ("Mask Texture", 2D) = "white" {}
        _MaskScaleOffset ("Mask Tiling / Offset", Vector) = (1, 1, 0, 0)
        [Toggle(UI_MASK_RED_CHANNEL)] _MaskRedChannel ("Use Red Channel", Float) = 0
        [Toggle] _MaskInvert ("Invert Mask", Float) = 0
        _MaskStrength ("Mask Strength", Range(0, 1)) = 1
        _MaskCutoff ("Mask Cutoff", Range(0, 1)) = 0
        _MaskSoftness ("Mask Softness", Range(0, 1)) = 1

        [HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil ("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector] _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend One OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex Vert
            #pragma fragment Frag

            #pragma shader_feature_local_fragment _ UI_EFFECT_ROUNDED
            #pragma shader_feature_local _ UI_EFFECT_GRADIENT
            #pragma shader_feature_local_fragment _ UI_EFFECT_MASK
            #pragma shader_feature_local_fragment _ UI_MASK_RED_CHANNEL
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color : COLOR;
                float4 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                fixed4 color : COLOR;
                float2 textureUv : TEXCOORD0;
                float2 effectUv : TEXCOORD1;
                float4 positionOS : TEXCOORD2;
#if UI_EFFECT_GRADIENT
                half gradientPosition : TEXCOORD3;
#endif
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            sampler2D _MaskTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_ST;

            half _TopLeftRadius;
            half _TopRightRadius;
            half _BottomRightRadius;
            half _BottomLeftRadius;

            fixed4 _GradientColorA;
            fixed4 _GradientColorB;
            float4 _GradientDirection;
            half _GradientStart;
            half _GradientEnd;

            float4 _MaskScaleOffset;
            half _MaskInvert;
            half _MaskStrength;
            half _MaskCutoff;
            half _MaskSoftness;

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.positionCS = UnityObjectToClipPos(input.positionOS);
                output.positionOS = input.positionOS;
                output.textureUv = TRANSFORM_TEX(input.uv.xy, _MainTex);
                output.effectUv = input.uv.xy;
                output.color = input.color * _Color;

#if UI_EFFECT_GRADIENT
                float2 direction = _GradientDirection.xy;
                direction *= rsqrt(max(dot(direction, direction), 0.0001));
                float extent = max(0.5 * (abs(direction.x) + abs(direction.y)), 0.0001);
                output.gradientPosition = (dot(output.effectUv - 0.5, direction) + extent) / (2.0 * extent);
#endif

                return output;
            }

            half RoundedRectangleCoverage(float2 uv)
            {
                float2 uvDx = ddx(uv);
                float2 uvDy = ddy(uv);
                float2 uvPerPixel = float2(
                    length(float2(uvDx.x, uvDy.x)),
                    length(float2(uvDx.y, uvDy.y)));
                float2 sizePixels = rcp(max(uvPerPixel, 0.00001));
                float2 halfSize = sizePixels * 0.5;
                float2 position = (uv - 0.5) * sizePixels;

                half leftRadius = lerp(_BottomRightRadius, _BottomLeftRadius, step(uv.x, 0.5));
                half rightRadius = lerp(_TopRightRadius, _TopLeftRadius, step(uv.x, 0.5));
                half radius = lerp(leftRadius, rightRadius, step(0.5, uv.y));
                radius = min(radius, min(halfSize.x, halfSize.y));

                float2 distanceToEdge = abs(position) - (halfSize - radius);
                float signedDistance = length(max(distanceToEdge, 0.0))
                    + min(max(distanceToEdge.x, distanceToEdge.y), 0.0)
                    - radius;
                float antialiasing = max(fwidth(signedDistance), 0.75);

                return 1.0 - smoothstep(-antialiasing, antialiasing, signedDistance);
            }

            half SampleMask(float2 uv)
            {
                float2 maskUv = uv * _MaskScaleOffset.xy + _MaskScaleOffset.zw;
                fixed4 sampleValue = tex2D(_MaskTex, maskUv);

#if UI_MASK_RED_CHANNEL
                half maskValue = sampleValue.r;
#else
                half maskValue = sampleValue.a;
#endif

                maskValue = lerp(maskValue, 1.0 - maskValue, step(0.5, _MaskInvert));
                half transition = max(_MaskSoftness, fwidth(maskValue));
                half coverage = saturate((maskValue - _MaskCutoff) / max(transition, 0.0001));

                return lerp(1.0, coverage, _MaskStrength);
            }

            fixed4 Frag(Varyings input) : SV_Target
            {
                fixed4 color = (tex2D(_MainTex, input.textureUv) + _TextureSampleAdd) * input.color;

#if UI_EFFECT_GRADIENT
                half gradientRange = max(_GradientEnd - _GradientStart, 0.0001);
                half gradient = saturate((input.gradientPosition - _GradientStart) / gradientRange);
                color *= lerp(_GradientColorA, _GradientColorB, gradient);
#endif

#if UI_EFFECT_ROUNDED
                color.a *= RoundedRectangleCoverage(input.effectUv);
#endif

#if UI_EFFECT_MASK
                color.a *= SampleMask(input.effectUv);
#endif

#ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(input.positionOS.xy, _ClipRect);
#endif

#ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
#endif

                color.rgb *= color.a;
                return color;
            }
            ENDHLSL
        }
    }

    CustomEditor "Code.UI.Editor.UIEffectsShaderGUI"
}
