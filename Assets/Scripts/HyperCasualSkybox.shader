Shader "Skybox/HyperCasual"
{
    Properties
    {
        [Header(Sky Colors)]
        _TopColor ("Top Color", Color) = (0.4, 0.7, 1.0, 1.0)
        _HorizonColor ("Horizon Color", Color) = (0.8, 0.5, 0.9, 1.0)
        _BottomColor ("Bottom Color", Color) = (1.0, 0.8, 0.5, 1.0)
        
        [Header(Gradient Settings)]
        _HorizonOffset ("Horizon Offset", Range(-1, 1)) = 0
        _HorizonBlend ("Horizon Blend", Range(0.1, 5)) = 1.5
        
        [Header(Sun)]
        _SunColor ("Sun Color", Color) = (1.0, 0.95, 0.8, 1.0)
        _SunSize ("Sun Size", Range(0.01, 0.5)) = 0.05
        _SunIntensity ("Sun Intensity", Range(0, 10)) = 3
        _SunDirection ("Sun Direction", Vector) = (0, 0.5, 1, 0)
        
        [Header(Stars)]
        _StarsColor ("Stars Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _StarsCount ("Stars Count", Range(0, 1000)) = 500
        _StarsSize ("Stars Size", Range(0.001, 0.01)) = 0.003
        _StarsBrightness ("Stars Brightness", Range(0, 1)) = 0.8
        
        [Header(Clouds)]
        [Toggle] _EnableClouds ("Enable Clouds", Float) = 1
        _CloudColor ("Cloud Color", Color) = (1.0, 1.0, 1.0, 0.8)
        _CloudSpeed ("Cloud Speed", Range(0, 1)) = 0.1
        _CloudScale ("Cloud Scale", Range(0.1, 10)) = 2
        _CloudDensity ("Cloud Density", Range(0, 1)) = 0.5
        
        [Header(Animation)]
        _RotationSpeed ("Rotation Speed", Range(0, 1)) = 0.05
    }

    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _TopColor;
            fixed4 _HorizonColor;
            fixed4 _BottomColor;
            float _HorizonOffset;
            float _HorizonBlend;
            
            fixed4 _SunColor;
            float _SunSize;
            float _SunIntensity;
            float3 _SunDirection;
            
            fixed4 _StarsColor;
            float _StarsCount;
            float _StarsSize;
            float _StarsBrightness;
            
            float _EnableClouds;
            fixed4 _CloudColor;
            float _CloudSpeed;
            float _CloudScale;
            float _CloudDensity;
            
            float _RotationSpeed;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 viewDir : TEXCOORD1;
            };

            // 노이즈 함수 (별과 구름용)
            float hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453123);
            }

            float noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                
                f = f * f * (3.0 - 2.0 * f);
                
                float a = hash(i);
                float b = hash(i + float2(1.0, 0.0));
                float c = hash(i + float2(0.0, 1.0));
                float d = hash(i + float2(1.0, 1.0));
                
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            // FBM (Fractional Brownian Motion) - 구름용
            float fbm(float2 p)
            {
                float value = 0.0;
                float amplitude = 0.5;
                
                for (int i = 0; i < 4; i++)
                {
                    value += amplitude * noise(p);
                    p *= 2.0;
                    amplitude *= 0.5;
                }
                
                return value;
            }

            // 별 생성
            float stars(float3 dir)
            {
                float3 p = dir * _StarsCount;
                float3 i = floor(p);
                float3 f = frac(p);
                
                float starValue = 0.0;
                
                for (int x = -1; x <= 1; x++)
                {
                    for (int y = -1; y <= 1; y++)
                    {
                        for (int z = -1; z <= 1; z++)
                        {
                            float3 offset = float3(x, y, z);
                            float3 cellPos = i + offset;
                            
                            float random = hash(cellPos.xy + cellPos.z * 10.0);
                            float3 starPos = offset + random - f;
                            
                            float dist = length(starPos);
                            float star = smoothstep(_StarsSize, 0.0, dist);
                            
                            float twinkle = 0.5 + 0.5 * sin(_Time.y * 3.0 + random * 10.0);
                            star *= twinkle;
                            
                            starValue += star;
                        }
                    }
                }
                
                return saturate(starValue * _StarsBrightness);
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.viewDir = normalize(v.texcoord);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 viewDir = normalize(i.viewDir);
                
                // 회전 애니메이션
                float rotation = _Time.y * _RotationSpeed;
                float cosRot = cos(rotation);
                float sinRot = sin(rotation);
                float3 rotatedDir = float3(
                    viewDir.x * cosRot - viewDir.z * sinRot,
                    viewDir.y,
                    viewDir.x * sinRot + viewDir.z * cosRot
                );
                
                float skyGradient = rotatedDir.y + _HorizonOffset;
                
                fixed3 skyColor;
                if (skyGradient > 0)
                {
                    // 위쪽 (지평선 → 정상)
                    float t = pow(skyGradient, _HorizonBlend);
                    skyColor = lerp(_HorizonColor.rgb, _TopColor.rgb, t);
                }
                else
                {
                    // 아래쪽 (지평선 → 바닥)
                    float t = pow(-skyGradient, _HorizonBlend);
                    skyColor = lerp(_HorizonColor.rgb, _BottomColor.rgb, t);
                }
                
                // 태양
                float3 sunDir = normalize(_SunDirection);
                float sunDot = dot(rotatedDir, sunDir);
                float sun = smoothstep(1.0 - _SunSize, 1.0, sunDot);
                float sunGlow = pow(saturate(sunDot), 20.0) * 0.5;
                
                fixed3 sunContribution = (_SunColor.rgb * _SunIntensity) * (sun + sunGlow);
                skyColor += sunContribution;
                
                // 별
                float nightFactor = saturate(-rotatedDir.y * 2.0); // 아래쪽일수록 별이 보임
                float starValue = stars(rotatedDir) * nightFactor;
                skyColor += _StarsColor.rgb * starValue;
                
                // 구름
                if (_EnableClouds > 0.5)
                {
                    float2 cloudUV = rotatedDir.xz / (rotatedDir.y + 0.5) * _CloudScale;
                    cloudUV += _Time.y * _CloudSpeed * float2(1.0, 0.5);
                    
                    float clouds = fbm(cloudUV);
                    clouds = smoothstep(_CloudDensity - 0.1, _CloudDensity + 0.1, clouds);
                    
                    // 지평선 근처에만 구름 표시
                    float cloudFade = saturate(1.0 - abs(rotatedDir.y * 2.0));
                    clouds *= cloudFade;
                    
                    skyColor = lerp(skyColor, _CloudColor.rgb, clouds * _CloudColor.a);
                }
                
                return fixed4(skyColor, 1.0);
            }
            ENDCG
        }
    }
    
    Fallback Off
}
