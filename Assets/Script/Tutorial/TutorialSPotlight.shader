Shader "UI/TutorialSpotlight"
{
    Properties
    {
        _Color ("Overlay Color", Color) = (0, 0, 0, 0.75)

        _Center ("Spotlight Center", Vector) = (0.5, 0.5, 0, 0)

        _Size ("Spotlight Size", Vector) = (0.2, 0.1, 0, 0)

        _Softness ("Edge Softness", Range(0.001, 0.5)) = 0.05

        _CornerRadius ("Corner Radius", Range(0, 0.5)) = 0.05
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest Always

        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"


            // =========================
            // Vertex data
            // =========================

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };


            // =========================
            // Data passed to fragment
            // =========================

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };


            // =========================
            // Shader properties
            // =========================

            fixed4 _Color;

            float4 _Center;

            float4 _Size;

            float _Softness;

            float _CornerRadius;


            // =========================
            // Vertex shader
            // =========================

            v2f vert(appdata v)
            {
                v2f o;

                o.vertex = UnityObjectToClipPos(v.vertex);

                o.uv = v.uv;

                return o;
            }


            // =========================
            // Rounded rectangle
            // =========================

            float roundedBox(
                float2 p,
                float2 b,
                float r
            )
            {
                float2 q = abs(p) - b + r;

                return length(max(q, 0.0))
                    + min(max(q.x, q.y), 0.0)
                    - r;
            }


            // =========================
            // Fragment shader
            // =========================

            fixed4 frag(v2f i) : SV_Target
            {
                // Current pixel position
                float2 uv = i.uv;


                // Move coordinates so the
                // spotlight center becomes 0,0
                float2 p = uv - _Center.xy;


                // Half of spotlight size
                float2 halfSize = _Size.xy * 0.5;


                // Calculate distance from
                // this pixel to the rounded rectangle
                float distance =
                    roundedBox(
                        p,
                        halfSize,
                        _CornerRadius
                    );


                // Create soft transition
                float hole =
                    smoothstep(
                        0.0,
                        _Softness,
                        distance
                    );


                // Outside = dark
                // Inside = transparent
                float alpha =
                    hole * _Color.a;


                return fixed4(
                    _Color.rgb,
                    alpha
                );
            }

            ENDCG
        }
    }
}
