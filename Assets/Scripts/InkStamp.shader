Shader "Hidden/InkStamp"
{
    Properties
    {
        _BrushTex ("Brush", 2D) = "white" {}
        _Rotation ("Rotation", Float) = 0
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
            float _Rotation;        // 회전 각도 (도 단위)

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

            // UV 회전 함수
            float2 RotateUV(float2 uv, float2 center, float angleDegrees)
            {
                // 도를 라디안으로 변환
                float angleRad = angleDegrees * 0.01745329251; // PI / 180
                
                float cosAngle = cos(angleRad);
                float sinAngle = sin(angleRad);
                
                // 중심 기준으로 이동
                float2 delta = uv - center;
                
                // 2D 회전 행렬 적용
                float2 rotated;
                rotated.x = delta.x * cosAngle - delta.y * sinAngle;
                rotated.y = delta.x * sinAngle + delta.y * cosAngle;
                
                // 중심으로 복원
                return rotated + center;
            }

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

                // UV 회전 적용 (중심은 0.5, 0.5)
                buv = RotateUV(buv, float2(0.5, 0.5), _Rotation);

                if (buv.x < 0 || buv.x > 1 || buv.y < 0 || buv.y > 1)
                    return old;

                // 브러시 마스크 (너 텍스처가 알파 없으면 .r 로 바꿔)
                fixed m = tex2D(_BrushTex, buv).a;

                // (선택) 스탬프 강도 조절하고 싶으면 _StampColor.a 를 "강도"로 사용
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
