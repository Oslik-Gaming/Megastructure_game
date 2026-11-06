Shader "Tool Effects/Heater Cone"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (1, 0.5, 0, 1)
        _TipColor ("Tip Color", Color) = (1, 1, 0.5, 1)
        _Intensity ("Intensity", Range(0, 5)) = 1
        _Alpha ("Alpha", Range(0, 1)) = 0.3
        _NoiseScale ("Noise Scale", Float) = 5.0
        _NoiseSpeed ("Noise Speed", Float) = 1.0
        _ActiveIntensity ("Active Intensity Boost", Range(1, 3)) = 1.5
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent" 
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float fogFactor : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _TipColor;
                half _Intensity;
                half _Alpha;
                half _NoiseScale;
                half _NoiseSpeed;
                half _ActiveIntensity;
            CBUFFER_END

            // Simple noise function
            float SimpleNoise(float2 uv)
            {
                return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Gradient from tip (0) to base (1)
                half gradient = input.uv.y;
                
                // Mix colors from tip to base
                half3 color = lerp(_TipColor.rgb, _BaseColor.rgb, gradient);
                
                // Add scrolling noise
                float2 noiseUV = input.uv * _NoiseScale;
                noiseUV.y += _Time.y * _NoiseSpeed;
                half noise = SimpleNoise(noiseUV);
                
                // Soft alpha falloff from center to edge
                half edgeFade = 1.0 - abs(input.uv.x - 0.5) * 2.0;
                edgeFade = smoothstep(0.0, 1.0, edgeFade);
                
                // Fade out at tip and base
                half lengthFade = smoothstep(0.0, 0.1, gradient) * smoothstep(1.0, 0.8, gradient);
                
                // Combine alpha
                half alpha = _Alpha * edgeFade * lengthFade * (0.7 + noise * 0.3);
                
                // Apply intensity
                color *= _Intensity;
                
                // Emission boost
                half3 emission = color * _Intensity;

                // Apply fog
                half3 finalColor = MixFog(emission, input.fogFactor);

                return half4(finalColor, alpha);
            }
            ENDHLSL
        }
    }
}
