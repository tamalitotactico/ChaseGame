// Shader para el overlay de niebla de guerra (Dota 2 style).
//
// Combina dos efectos en un solo pass:
//   1. Polígono de visión (RenderTexture generada por GPU desde un mesh
//      procedural en FogOfWarManager) para oclusión por muros.
//   2. Fade suave radial en el borde del radio de visión via smoothstep.
//
// El resultado: bordes de niebla suaves que además respetan los muros (no se "filtra" luz
// por detrás de una pared, a diferencia de un simple gradient radial).
//
// FogOfWarManager actualiza _VisionTex0 (RT), _VisionPos0 y _VisionRadius0 cada frame.
// _FogColor y _FadeWidth se configuran en el material desde el Inspector.
//
// La niebla muestra alpha = _FogColor.a * (1 - visibility), donde visibility = 0 fuera de
// vision y sube suavemente a 1 en el centro del radio, respetando el polígono de muros.

Shader "Game/FogOverlay"
{
    Properties
    {
        [Header(Fog)]
        _FogColor       ("Fog Color",                   Color)   = (0, 0, 0, 0.6)

        [Header(Vision Source)]
        _VisionTex0     ("Vision Polygon Texture",      2D)      = "black" {}
        _VisionPos0     ("Vision Source Pos (XY=pos)",  Vector)  = (0, 0, 0, 0)
        _VisionRadius0  ("Vision Radius (world units)", Float)   = 8
        _FadeWidth      ("Edge Fade Width (world units)",Float)  = 1.5
    }

    SubShader
    {
        Tags
        {
            "Queue"           = "Transparent"
            "RenderType"      = "Transparent"
            "RenderPipeline"  = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "FogOverlay"
            // CRITICAL: el URP 2D Renderer solo ejecuta passes con esta LightMode.
            // Sin esto, los SpriteRenderers usan un fallback que ignora nuestro
            // vertex shader y produce worldXY rotos.
            Tags { "LightMode" = "Universal2D" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4  _FogColor;
                float4 _VisionPos0;
                float  _VisionRadius0;
                float  _FadeWidth;
            CBUFFER_END

            TEXTURE2D(_VisionTex0);
            SAMPLER(sampler_VisionTex0);

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 worldXY     : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                float3 wp = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionHCS = TransformWorldToHClip(wp);
                OUT.worldXY = wp.xy;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

                float2 worldXY = IN.worldXY;

                float2 toSource = worldXY - _VisionPos0.xy;
                float  dist     = length(toSource);

                float2 uv = toSource / (_VisionRadius0 * 2.0) + 0.5;
                #if UNITY_UV_STARTS_AT_TOP
                    uv.y = 1.0 - uv.y;
                #endif

                float inPolygon = 0;
                if (uv.x >= 0.0 && uv.x <= 1.0 && uv.y >= 0.0 && uv.y <= 1.0)
                    inPolygon = SAMPLE_TEXTURE2D(_VisionTex0, sampler_VisionTex0, uv).r;

                float edgeFade   = 1.0 - smoothstep(_VisionRadius0 - _FadeWidth, _VisionRadius0, dist);
                float visibility = inPolygon * edgeFade;

                half4 col = _FogColor;
                col.a = _FogColor.a * (1.0 - visibility);
                return col;
            }
            ENDHLSL
        }
    }
}
