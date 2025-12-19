Shader "Unlit/InkGround"
{
    Properties
    {
        _BaseMap ("Base Map", 2D) = "white" {}
        _InkTex ("Ink Tex", 2D) = "black" {}
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _BaseMap;
            sampler2D _InkTex;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
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
                fixed4 baseCol = tex2D(_BaseMap, i.uv);
                fixed4 ink = tex2D(_InkTex, i.uv);   // ink.rgb + ink.a

                fixed3 col = lerp(baseCol.rgb, ink.rgb, ink.a);
                return fixed4(col, 1);
            }
            ENDCG
        }
    }
}
