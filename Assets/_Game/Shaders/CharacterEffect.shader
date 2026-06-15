// Shader modular para personajes del juego. Un solo material puede mostrar multiples
// efectos de estado simultaneamente sin cambiar de material:
//   - _EffectTint:     overlay de color para stunned (amarillo), slowed (azul), etc.
//   - _OutlineEnabled: contorno coloreado para marked/target.
//   - _StealthMode:    look de ocultamiento que ve quien SI puede ver al oculto (aliado/uno
//                      mismo, o enemigo dentro del radio de camuflaje). 0=normal, 1=invisible
//                      (fantasma translucido pulsante), 2=camuflaje (shimmer desaturado). El
//                      ALPHA (quien lo ve y cuanto) lo decide StateVisibility via SpriteRenderer.color;
//                      este shader solo agrega el ASPECTO de "esta oculto".
//
// Todas las propiedades marcadas [PerRendererData] deben setearse via
// material.SetColor / material.SetFloat sobre la instancia del material del SpriteRenderer
// (obtenida via _sprite.material en Awake, que auto-instancia).
//
// Nota sobre iluminacion: este shader es Unlit (no reacciona a Light2D).
// Si el proyecto incorpora iluminacion 2D dinamica, reemplazar con un Shader Graph
// usando el Master Node "Sprite Lit" y replicar la logica del fragment aqui.

Shader "Game/CharacterEffect"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}

        [PerRendererData] _Color         ("Tint",          Color)   = (1,1,1,1)
        [PerRendererData] _EffectTint    ("Effect Tint",   Color)   = (0,0,0,0)
        [PerRendererData] _OutlineEnabled("Outline On",    Float)   = 0
        [PerRendererData] _OutlineColor  ("Outline Color", Color)   = (1,0.9,0,1)
        _OutlineThick ("Outline Thickness (px)", Float) = 2

        [PerRendererData] _StealthMode  ("Stealth Mode (0/1/2)", Float) = 0
        [PerRendererData] _StealthColor ("Stealth Color",        Color) = (0.55,0.7,1,1)

        // Setteado automaticamente por Unity en base a SpriteRenderer.maskInteraction:
        //   None              → StencilComp=Always (8), StencilRef=0
        //   VisibleInsideMask → StencilComp=Equal (3),  StencilRef=1
        //   VisibleOutsideMask→ StencilComp=NotEqual (6),StencilRef=1
        [HideInInspector] _StencilComp ("Stencil Comp", Float) = 8
        [HideInInspector] _StencilRef  ("Stencil Ref",  Float) = 0
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

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        // Soporte para SpriteMask: lee el stencil que Unity setea via maskInteraction.
        Stencil
        {
            Ref  [_StencilRef]
            Comp [_StencilComp]
        }

        Pass
        {
            HLSLPROGRAM
            #pragma vertex   Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _MainTex_TexelSize;  // (1/w, 1/h, w, h) — provisto automaticamente
                float4 _Color;
                float4 _EffectTint;         // rgb = color del efecto, a = fuerza del blend [0..1]
                float  _OutlineEnabled;
                float4 _OutlineColor;
                float  _OutlineThick;       // en pixeles del sprite
                float  _StealthMode;        // 0 normal, 1 invisible, 2 camuflaje
                float4 _StealthColor;       // tinte del look de ocultamiento
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;   // SpriteRenderer.color → vertex color
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float4 color       : COLOR;
            };

            Varyings Vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv          = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.color       = IN.color;
                return OUT;
            }

            half4 Frag(Varyings IN) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                // _Color viene de la propiedad material, IN.color es SpriteRenderer.color
                half4 col = tex * _Color * IN.color;

                // Outline: pixeles transparentes cerca del borde del sprite se colorean
                if (_OutlineEnabled > 0.5 && col.a < 0.5)
                {
                    float2 d = _MainTex_TexelSize.xy * _OutlineThick;
                    half n  = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv + float2( d.x,  0   )).a;
                         n += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv + float2(-d.x,  0   )).a;
                         n += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv + float2( 0,    d.y )).a;
                         n += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv + float2( 0,   -d.y )).a;
                    if (n > 0.5)
                        return _OutlineColor;
                }

                // Effect tint overlay: _EffectTint.a controla la intensidad del efecto
                col.rgb = lerp(col.rgb, _EffectTint.rgb, _EffectTint.a);

                // Look de ocultamiento (solo para quien puede verlo; el alpha base ya viene
                // gateado por StateVisibility en SpriteRenderer.color). _Time.y = segundos.
                if (_StealthMode > 0.5)
                {
                    float t = _Time.y;
                    if (_StealthMode < 1.5)
                    {
                        // Invisible: fantasma translucido con scanline pulsante hacia el tinte frio.
                        float scan = 0.5 + 0.5 * sin(IN.uv.y * 28.0 + t * 6.0);
                        col.rgb = lerp(col.rgb, _StealthColor.rgb, 0.5);
                        col.a  *= lerp(0.22, 0.5, scan);
                    }
                    else
                    {
                        // Camuflaje: shimmer diagonal + desaturacion parcial hacia el tinte.
                        float shimmer = 0.5 + 0.5 * sin((IN.uv.x + IN.uv.y) * 18.0 + t * 4.0);
                        float gray = dot(col.rgb, float3(0.299, 0.587, 0.114));
                        float3 camo = lerp(float3(gray, gray, gray), _StealthColor.rgb, 0.5);
                        col.rgb = lerp(col.rgb, camo, 0.6);
                        col.a  *= lerp(0.5, 0.82, shimmer);
                    }
                }
                return col;
            }
            ENDHLSL
        }
    }

    FallBack "Sprites/Default"
}
