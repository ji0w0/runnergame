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

            sampler2D _MainTex;
            sampler2D _BrushTex;

            float4 _Center;
            fixed4 _StampColor;
            float _Rotation;

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

            float2 RotateUV(float2 uv, float2 center, float angleDegrees)
            {
                float angleRad = angleDegrees * 0.01745329251; // PI / 180
                
                float cosAngle = cos(angleRad);
                float sinAngle = sin(angleRad);
                
                float2 delta = uv - center;
                
                float2 rotated;
                rotated.x = delta.x * cosAngle - delta.y * sinAngle;
                rotated.y = delta.x * sinAngle + delta.y * cosAngle;
                
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

                buv = RotateUV(buv, float2(0.5, 0.5), _Rotation);

                if (buv.x < 0 || buv.x > 1 || buv.y < 0 || buv.y > 1)
                    return old;

                fixed m = tex2D(_BrushTex, buv).a;

                m *= _StampColor.a;

                fixed3 newRgb = _StampColor.rgb;
                fixed  newA   = m;

                fixed3 outRgb = lerp(old.rgb, newRgb, m);
                fixed  outA   = old.a * (1 - m) + newA;

                return fixed4(outRgb, outA);
            }
            ENDCG
        }
    }
}
