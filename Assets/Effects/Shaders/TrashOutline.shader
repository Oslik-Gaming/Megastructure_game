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
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { UNITY_VERTEX_INPUT_INSTANCE_ID
                float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings { UNITY_VERTEX_OUTPUT_STEREO
                float4 positionHCS : SV_POSITION; };
            CBUFFER_START(UnityPerMaterial) float4 _Color; float _Intensity; CBUFFER_END
            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                float3 pos = input.positionOS.xyz;
                output.positionHCS = TransformObjectToHClip(pos);
                return output;
            }
            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                return half4(_Color.rgb, _Color.a * _Intensity);
            }
            ENDHLSL
        }
    }
}
