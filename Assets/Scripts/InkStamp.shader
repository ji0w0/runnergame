Shader "Hidden/InkStamp"
{
    Properties
    {
        _BrushTex ("Brush", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Overlay" }

        Pass
        {
            ZTest Always
            ZWrite Off
            Cull Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;     // 기존 잉크RT
            sampler2D _BrushTex;    // 브러시(스플랫) 텍스처

            float4 _Center;         // (u, v, radiusX, radiusY)
            fixed4 _StampColor;     // 이번 스탬프 색

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 old = tex2D(_MainTex, i.uv);

                float2 local = (i.uv - _Center.xy) / max(float2(_Center.z, _Center.w), 1e-6);
                float2 buv = local * 0.5 + 0.5;

                if (buv.x < 0 || buv.x > 1 || buv.y < 0 || buv.y > 1)
                    return old;

                // 브러시 마스크 (너 텍스처가 알파 없으면 .r 로 바꿔)
                fixed m = tex2D(_BrushTex, buv).a;

                // (선택) 스탬프 강도 조절하고 싶으면 _StampColor.a 를 “강도”로 사용
                m *= _StampColor.a;

                fixed3 newRgb = _StampColor.rgb;
                fixed  newA   = m;

                // ✅ 덮어쓰기 합성 (Source Over)
                fixed3 outRgb = lerp(old.rgb, newRgb, m);
                fixed  outA   = old.a * (1 - m) + newA;

                return fixed4(outRgb, outA);
            }
            ENDCG
        }
    }
}
