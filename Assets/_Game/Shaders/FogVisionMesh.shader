// Shader interno del sistema de Fog of War.
//
// Renderiza el mesh de vision (Triangle Fan) como blanco solido sobre la
// RenderTexture de vision. Usado por FogOfWarManager.BuildForEntry() via
// CommandBuffer.DrawMesh, despues de SetViewProjectionMatrices con una ortho
// que mapea los vertices [-1..1] directo al RT.
//
// NO forma parte del render pipeline normal de la escena (es Hidden/).
// El RT resultante alimenta a Game/FogOverlay como _VisionTex0.

Shader "Hidden/FogVisionMesh"
{
    SubShader
    {
        Pass
        {
            ZWrite Off
            ZTest Always
            Cull Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return 1;
            }
            ENDCG
        }
    }
}
