Shader "Unlit/InteractionPointAnim"
{
    Properties
    {
        _BaseCol ("Color", Color) = (1, 1, 1, 1)
        _FadeCol ("Color", Color) = (1, 1, 1, 1)
    }
    SubShader
    {
        Tags {"Queue" = "Transparent" "RenderType" = "Transparent" }
        LOD 100
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag alpha:fade

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            float4 _MainTex_ST;
            fixed4 _BaseCol;
            fixed4 _FadeCol;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float sdCircle(float2 p, float r)
            {
                return length(p) - r;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv - 0.5;
                float modTime = fmod(_Time[1] / 2, 1.);

                float sdf = step(1., 1. - (sdCircle(uv, modTime / 2)));

                float col = sdf * (1. - modTime);
                col = smoothstep(0, 0.6, col);

                return fixed4(lerp(_BaseCol.rgb, _FadeCol.rgb, col), col);
            }
            ENDCG
        }
    }
}
