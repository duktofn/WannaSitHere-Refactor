Shader "Custom/CircleTransition"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (0, 0, 0, 1)
        _Radius ("Circle Radius", Range(0.0, 1.5)) = 0
        _CenterX ("Center X", Range(0.0, 1.0)) = 0.5
        _CenterY ("Center Y", Range(0.0, 1.0)) = 0.5
        _Smoothness ("Edge Smoothness", Range(0.0001, 0.05)) = 0.002
    }
    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "Queue"="Transparent"
        }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            fixed4 _Color;
            float _Radius;
            float _CenterX;
            float _CenterY;
            float _Smoothness;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                if (_Radius <= 0.00001)
                {
                    return i.color;
                }

                float2 center = float2(_CenterX, _CenterY);
                float dist = distance(i.uv, center);
                float edge = max(_Smoothness, 0.0001);

                // Khi dist <= (_Radius - edge): ở trong vòng tròn -> innerAlpha = 1 (trong suốt)
                // Khi dist >= _Radius: ở ngoài vòng tròn -> innerAlpha = 0 (màu đen/phủ)
                float innerAlpha = smoothstep(_Radius, max(0.0, _Radius - edge), dist);

                return fixed4(i.color.rgb, i.color.a * (1.0 - innerAlpha));
            }
            ENDCG
        }
    }
}