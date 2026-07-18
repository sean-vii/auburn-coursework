// A cheap fake "volumetric" beam for URP. URP does NOT light up mid-air, so we render a
// translucent cone MESH and make it glow. The trick is a fresnel falloff: the more edge-on we
// view the cone's surface, the denser it looks — which reads as a solid shaft of light instead
// of a thin hollow shell. Additive blending makes it add light without darkening what's behind.
//
// Used by FlashlightBeamCone.cs (which builds the cone mesh and sets these properties).
Shader "Darkless/FlashlightBeam"
{
    Properties
    {
        _Color ("Tint", Color) = (1, 0.95, 0.8, 1)
        _Intensity ("Intensity", Range(0, 5)) = 1
        _FresnelPower ("Edge Softness (Fresnel)", Range(0.25, 8)) = 2.5
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "Beam"
            Blend SrcAlpha One   // additive glow: adds light, never darkens
            ZWrite Off           // transparent: don't write depth
            Cull Off             // visible from inside and outside the cone

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 color      : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 normalWS    : TEXCOORD0;
                float3 viewWS      : TEXCOORD1;
                float4 color       : COLOR;
            };

            half4 _Color;
            half  _Intensity;
            half  _FresnelPower;

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionHCS = TransformWorldToHClip(positionWS);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.viewWS = GetWorldSpaceViewDir(positionWS);
                OUT.color = IN.color;
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                float3 N = normalize(IN.normalWS);
                float3 V = normalize(IN.viewWS);
                // Edge-on surfaces glow more -> fakes a solid volume of light.
                half fresnel = pow(1.0 - saturate(dot(N, V)), _FresnelPower);
                // Vertex alpha carries the near->far length fade (set by the mesh builder).
                half a = IN.color.a * _Color.a * _Intensity * fresnel;
                return half4(_Color.rgb, saturate(a));
            }
            ENDHLSL
        }
    }
    Fallback Off
}
