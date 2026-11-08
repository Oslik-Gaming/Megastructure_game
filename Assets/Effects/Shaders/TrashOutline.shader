Shader "Megastructure/TrashOutline"
{
    Properties { _Color ("Color", Color) = (0,1,0,1) _Intensity ("Intensity", Float) = 1 }
    SubShader
    {
        Tags { "Queue"="Transparent+100" "RenderType"="Transparent" }
        Pass
        {
            // This proxy is used as a solid highlight layer. Rendering the
            // front faces avoids the hollow halo produced by the old inverted
            // silhouette pass (Cull Front + normal extrusion).
            Cull Back
            ZTest LEqual
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings { float4 positionHCS : SV_POSITION; };
            CBUFFER_START(UnityPerMaterial) float4 _Color; float _Intensity; CBUFFER_END
            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 pos = input.positionOS.xyz;
                output.positionHCS = TransformObjectToHClip(pos);
                return output;
            }
            half4 frag(Varyings input) : SV_Target { return half4(_Color.rgb, _Color.a * _Intensity); }
            ENDHLSL
        }
    }
}
