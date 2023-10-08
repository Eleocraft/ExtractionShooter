Shader "Custom/Skybox"
{
    Properties
    {
        _SkyColor ("Sky Color", Color) = (1, 1, 1, 1)
        _GroundColor ("Ground Color", Color) = (1, 1, 1, 1)
        _HorizonSize ("Horizon Gradient Size", Range(0, 1)) = 1
        _UpAxis ("Up Axis", Vector) = (0, 1, 0)
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
				float4 _SkyColor;
				float4 _GroundColor;
                float _HorizonSize;
                float3 _UpAxis;
			CBUFFER_END

            struct Attributes
            {
                float4 posOS    : POSITION;
            };

            struct v2f
            {
                float4 posCS        : SV_POSITION;
                float3 viewDirWS    : TEXCOORD0;
            };

            v2f Vertex(Attributes IN)
            {
                v2f OUT = (v2f)0;
    
                VertexPositionInputs vertexInput = GetVertexPositionInputs(IN.posOS.xyz);
    
                OUT.posCS = vertexInput.positionCS;
                OUT.viewDirWS = vertexInput.positionWS;

                return OUT;
            }

            float4 Fragment (v2f IN) : SV_TARGET
            {
                float3 viewDir = normalize(IN.viewDirWS);
                float3 axis = normalize(_UpAxis);
                float viewAxisDot = dot(viewDir, axis);
                float gradientValue = saturate((viewAxisDot / _HorizonSize + 1) / 2);
                float4 mainColor = _SkyColor * gradientValue + _GroundColor * (1-gradientValue);
                return mainColor;
            }

            ENDHLSL
        }
    }
}
