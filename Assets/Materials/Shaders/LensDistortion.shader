Shader "VisComp/LensDistortion"
{
    Properties
    {
        _MainTex("Base (RGB) Trans (A)", 2D) = "white" {}
        _k1("radial (1)", Range(-1,1)) = 0.0
        _k2("radial (2)", Range(-2,2)) = 0.0
        _p1("tangential (x)", Range(-0.25,0.25)) = 0.0
        _p2("tangential (y)", Range(-0.25,0.25)) = 0.0
        _k3("radial (3)", Range(-4,4)) = 0.0
    }
    SubShader
    {
        Tags {"Queue" = "AlphaTest" "IgnoreProjector" = "True" "RenderType" = "TransparentCutout"}
        LOD 100
        Cull Off ZWrite Off ZTest Always

        Lighting Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

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

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex;
            float _k1;
            float _k2;
            float _p1;
            float _p2;
            float _k3;

            float2 distort(float2 uv)
            {
                // uv [0.0:1.0]^2 -> xy [-0.5:+0.5]^2
                float x = uv.x - 0.5;
                float y = uv.y - 0.5;

                // 5-th order lens distortion model
                float r2 = x * x + y * y;
                float r4 = r2 * r2;
                float r6 = r2 * r4;
                float x2 = x * x;
                float y2 = y * y;
                float xy = x * y;

                float K = _k1 * r2 + _k2 * r4 + _k3 * r6;

                // xy [-1.0:+1.0]^2 -> uv [0.0:1.0]^2
                float x_d = x + x * K + _p2 * (r2 + 2.0 * x2) + 2.0 * _p1 * xy;
                float y_d = y + y * K + _p1 * (r2 + 2.0 * y2) + 2.0 * _p2 * xy;

                return float2(x_d + 0.5, y_d + 0.5);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv_distorted = distort(i.uv);

                fixed4 col = tex2D(_MainTex, uv_distorted);
                col.rgb = col.rgb;
                return col;
            }
            ENDCG
        }
    }
}
