Shader "UI/SimpleUIRoundedCorners"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Color Tint", Color) = (1,1,1,1)

        // Указываем долю от 0 до 0.5
        // 0.0 — без скруглений
        // 0.5 — максимально «круглые» углы
        _CornerRadius ("Corner Radius [0..0.5]", Range(0, 0.5)) = 0.2
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "CanvasOverlay" = "True"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;
            float _CornerRadius;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Базовый цвет из текстуры (спрайта) с учетом цвета
                fixed4 col = tex2D(_MainTex, i.uv) * i.color;

                // Корректируем UV, чтобы оставаться в [0..1]
                float2 uv = saturate(i.uv);

                // CornerRadius — доля для «радиуса» скругления
                float R = _CornerRadius;

                // Изначально пиксель полностью непрозрачен
                float alpha = 1.0;

                // -----------------------------
                // Логика скругления:
                // Мы хотим «отрезать» четверть круга в каждом углу.
                // Центры окружностей:
                //   BL: (R,      R)
                //   BR: (1 - R,  R)
                //   TL: (R,      1 - R)
                //   TR: (1 - R,  1 - R)
                //
                // Если пиксель «залез» в зону угла, проверяем dist > R:
                //   > R  => пиксель за границей круга => убираем (alpha = 0)
                //   <= R => внутри круга => оставляем
                // -----------------------------

                // --- ЛЕВЫЙ НИЖНИЙ угол ---
                if (uv.x < R && uv.y < R)
                {
                    float2 centerBL = float2(R, R);
                    float distBL = distance(uv, centerBL);
                    if (distBL > R) alpha = 0.0;
                }

                // --- ПРАВЫЙ НИЖНИЙ угол ---
                if (uv.x > 1.0 - R && uv.y < R)
                {
                    float2 centerBR = float2(1.0 - R, R);
                    float distBR = distance(uv, centerBR);
                    if (distBR > R) alpha = 0.0;
                }

                // --- ЛЕВЫЙ ВЕРХНИЙ угол ---
                if (uv.x < R && uv.y > 1.0 - R)
                {
                    float2 centerTL = float2(R, 1.0 - R);
                    float distTL = distance(uv, centerTL);
                    if (distTL > R) alpha = 0.0;
                }

                // --- ПРАВЫЙ ВЕРХНИЙ угол ---
                if (uv.x > 1.0 - R && uv.y > 1.0 - R)
                {
                    float2 centerTR = float2(1.0 - R, 1.0 - R);
                    float distTR = distance(uv, centerTR);
                    if (distTR > R) alpha = 0.0;
                }

                // «Обрезаем» углы
                col.a *= alpha;
                return col;
            }
            ENDCG
        }
    }

    FallBack "UI/Default"
}