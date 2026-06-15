// Indicador de habilidad PROCEDURAL (SDF). Dibuja anillo / disco(AoE) / flecha(lane) / cono con el
// BORDE exacto en el limite (sin glow fuera de rango). Todas las dimensiones se pasan en UNIDADES DE
// MUNDO (_SizeWorld, _EdgeWorld) para que el grosor del borde sea constante a cualquier rango. Unlit
// URP, aditivo y emisivo HDR (el bloom hace el neon). El componente ProceduralIndicator escala/rota el
// quad y setea las propiedades via MaterialPropertyBlock.
Shader "Game/AbilityIndicator"
{
    Properties
    {
        [PerRendererData] _Shape       ("Shape (0 ring,1 disc,2 arrow,3 cone)", Float) = 0
        [HDR] _EdgeColor ("Edge Color", Color) = (4, 0.6, 0.3, 1)
        [HDR] _FillColor ("Fill Color", Color) = (1, 0.2, 0.1, 0.18)
        _SizeWorld     ("Size World (xy)", Vector) = (3,3,0,0)
        _EdgeWorld     ("Edge Width World", Float) = 0.12
        _ConeHalfAngle ("Cone Half Angle (rad)", Float) = 0.5
        _Fill          ("Cast Fill 0..1", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" }
        Blend SrcAlpha One        // aditivo: neon que bloomea sobre el suelo
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings   { float4 positionHCS : SV_POSITION; float2 uv : TEXCOORD0; };

            float  _Shape;
            float4 _EdgeColor;
            float4 _FillColor;
            float4 _SizeWorld;
            float  _EdgeWorld;
            float  _ConeHalfAngle;
            float  _Fill;

            Varyings vert (Attributes IN)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                o.uv = IN.uv;
                return o;
            }

            float4 frag (Varyings IN) : SV_Target
            {
                float2 p  = IN.uv * 2.0 - 1.0;     // [-1,1]
                float  edge = max(_EdgeWorld, 1e-4);
                float  fillA = 0.0;                // alpha de relleno interior
                float  edgeA = 0.0;                // alpha de borde brillante

                if (_Shape < 0.5 || _Shape < 1.5)
                {
                    // RING (0) y DISC/AoE (1): circulo de radio R = _SizeWorld.x/2
                    float R    = _SizeWorld.x * 0.5;
                    float dist = length(p) * R;                 // distancia al centro en MUNDO
                    float aa   = max(fwidth(dist), 1e-4);
                    edgeA = 1.0 - smoothstep(edge, edge + aa, abs(dist - R));   // banda en el borde
                    if (_Shape >= 0.5)                          // DISC: relleno interior
                        fillA = (1.0 - smoothstep(R - aa, R, dist)) * _FillColor.a;
                    else                                        // RING: relleno tenue hacia adentro
                        fillA = (1.0 - smoothstep(R - edge - aa, R - edge, dist)) * _FillColor.a * 0.5;
                }
                else if (_Shape < 2.5)
                {
                    // ARROW / lane: rectangulo de _SizeWorld (ancho x largo). Borde de grosor mundo constante.
                    float hx = _SizeWorld.x * 0.5;
                    float hy = _SizeWorld.y * 0.5;
                    float dEdge = min((1.0 - abs(p.x)) * hx, (1.0 - abs(p.y)) * hy); // dist al borde (mundo)
                    float aa = max(fwidth(dEdge), 1e-4);
                    fillA = (1.0 - smoothstep(0.0, aa, -dEdge)) * _FillColor.a;       // dentro del rect
                    edgeA = 1.0 - smoothstep(edge, edge + aa, dEdge);                 // banda interior del borde
                }
                else
                {
                    // CONE: sector de medio-angulo _ConeHalfAngle, radio R, apice en el centro-abajo (p=(0,-1)).
                    float R   = _SizeWorld.y;                   // largo del cono = alto del quad
                    float2 a  = p; a.y = (a.y + 1.0) * 0.5;     // apice en y=0
                    float dist = length(float2(a.x * _SizeWorld.x * 0.5, a.y * R));
                    float ang  = atan2(abs(a.x), max(a.y, 1e-4));
                    float aa   = max(fwidth(dist), 1e-4);
                    float within = step(ang, _ConeHalfAngle);
                    fillA = (1.0 - smoothstep(R - aa, R, dist)) * within * _FillColor.a;
                    edgeA = (1.0 - smoothstep(edge, edge + aa, abs(dist - R))) * within;
                }

                // Fill de canalizacion: realza un poco el relleno segun el progreso (telegrafia el cast).
                fillA *= lerp(1.0, 1.6, saturate(_Fill));

                float3 col = _EdgeColor.rgb * edgeA + _FillColor.rgb * fillA;
                float  a   = saturate(edgeA * _EdgeColor.a + fillA);
                return float4(col, a);
            }
            ENDHLSL
        }
    }
}
