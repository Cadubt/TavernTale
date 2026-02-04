Shader "Sprites/Outline"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _OutlineColor ("Outline Color", Color) = (1,0,0,1)
        _OutlineWidth ("Outline Width", Range(0, 0.1)) = 0.02
        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
    }

    SubShader
    {
        Tags
        { 
            "Queue"="Transparent" 
            "IgnoreProjector"="True" 
            "RenderType"="Transparent" 
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ PIXELSNAP_ON
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            fixed4 _Color;
            fixed4 _OutlineColor;
            float _OutlineWidth;

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color * _Color;
                #ifdef PIXELSNAP_ON
                OUT.vertex = UnityPixelSnap(OUT.vertex);
                #endif
                return OUT;
            }

            sampler2D _MainTex;
            sampler2D _AlphaTex;
            float _AlphaSplitEnabled;

            fixed4 SampleSpriteTexture(float2 uv)
            {
                fixed4 color = tex2D(_MainTex, uv);

#if UNITY_TEXTURE_ALPHASPLIT_ALLOWED
                if (_AlphaSplitEnabled)
                    color.a = tex2D(_AlphaTex, uv).r;
#endif

                return color;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                fixed4 c = SampleSpriteTexture(IN.texcoord) * IN.color;
                
                // Se o pixel é parte do sprite, retorna ele sem alteração
                if (c.a > 0.01)
                {
                    return c;
                }
                
                // Check surrounding pixels for outline
                float2 up = IN.texcoord + float2(0, _OutlineWidth);
                float2 down = IN.texcoord + float2(0, -_OutlineWidth);
                float2 left = IN.texcoord + float2(-_OutlineWidth, 0);
                float2 right = IN.texcoord + float2(_OutlineWidth, 0);
                
                float outline = 0;
                outline += SampleSpriteTexture(up).a;
                outline += SampleSpriteTexture(down).a;
                outline += SampleSpriteTexture(left).a;
                outline += SampleSpriteTexture(right).a;
                
                // Diagonals for better outline
                outline += SampleSpriteTexture(IN.texcoord + float2(_OutlineWidth, _OutlineWidth)).a;
                outline += SampleSpriteTexture(IN.texcoord + float2(-_OutlineWidth, _OutlineWidth)).a;
                outline += SampleSpriteTexture(IN.texcoord + float2(_OutlineWidth, -_OutlineWidth)).a;
                outline += SampleSpriteTexture(IN.texcoord + float2(-_OutlineWidth, -_OutlineWidth)).a;
                
                // Se detectou outline, retorna a cor do outline
                if (outline > 0.01)
                {
                    return _OutlineColor;
                }
                
                return c;
            }
            ENDCG
        }
    }
}
