// OverDrive 2026 — visionOS "liquid glass" for UI surfaces (ODHomepageSurface in glass mode).
// Based on Unity's UI/Default (stencil, RectMask2D clipping, single-pass stereo).
//
// Where the mesh is glass fill (uv0.x = 1), each pixel looks up the frosted room
// in the direction right behind it: a world-anchored panorama of blurred
// passthrough frames (_ODPanoTex, built by ODPassthroughBlur), seen from the
// user's head (_ODPanoOrigin). Directions the camera hasn't seen yet use the
// panorama's average colour, and before any frame (Editor, permission refused,
// other headsets) a neutral room colour: the glass never turns clear, wherever
// the panel sits in the field of view. The result is tinted with the vertex
// colour (grey glass + championship tint + sheen). Rim and shadow vertices
// (uv0.x = 0) keep their plain vertex colour.
Shader "OD/UI Glass"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _TintStrength ("Glass tint strength", Range(0, 1)) = 0.42
        _Desaturate ("Background desaturation", Range(0, 1)) = 0.15
        _Brightness ("Background brightness", Range(0, 2)) = 1.1
        _Fallback ("Room colour before the first camera frame", Color) = (0.5, 0.5, 0.52, 1)

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
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

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex        : SV_POSITION;
                fixed4 color         : COLOR;
                float2 texcoord      : TEXCOORD0;
                float4 localPosition : TEXCOORD1;
                float3 worldPosition : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            fixed4 _Color;
            float4 _ClipRect;
            float _TintStrength;
            float _Desaturate;
            float _Brightness;
            fixed4 _Fallback;

            // Published by ODPassthroughBlur (Core).
            sampler2D _ODPanoTex;     // premultiplied: rgb = colour * coverage, a = coverage
            float4 _ODPanoOrigin;     // head position the panorama is looked up from (world)
            float _ODBlurReady;       // 1 once the panorama holds at least one camera frame

            v2f vert (appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.localPosition = v.vertex;
                OUT.worldPosition = mul(unity_ObjectToWorld, v.vertex).xyz;
                OUT.vertex = UnityObjectToClipPos(v.vertex);
                OUT.texcoord = v.texcoord;
                OUT.color = v.color * _Color;
                return OUT;
            }

            fixed4 frag (v2f IN) : SV_Target
            {
                fixed4 col = IN.color;
                float glassMask = saturate(IN.texcoord.x);   // 1 on the fill, 0 on rim / shadow

                if (glassMask > 0.001)
                {
                    float3 room = _Fallback.rgb;
                    if (_ODBlurReady > 0.5)
                    {
                        // Equirectangular lookup; explicit LOD (no seam at the yaw wrap).
                        float3 d = normalize(IN.worldPosition - _ODPanoOrigin.xyz);
                        float2 uv = float2(atan2(d.x, d.z) * (0.5 / UNITY_PI) + 0.5, asin(clamp(d.y, -1.0, 1.0)) / UNITY_PI + 0.5);
                        float4 seen = tex2Dlod(_ODPanoTex, float4(uv, 0.0, 0.0));
                        float4 mean = tex2Dlod(_ODPanoTex, float4(0.5, 0.5, 0.0, 12.0));   // 1x1 mip: average of everything seen
                        room = lerp(mean.rgb / max(mean.a, 1e-4), seen.rgb / max(seen.a, 1e-4), saturate(seen.a));
                    }

                    float3 bg = room * _Brightness;
                    float luma = dot(bg, float3(0.299, 0.587, 0.114));
                    bg = lerp(bg, luma.xxx, _Desaturate);

                    float3 glass = lerp(bg, col.rgb, saturate(col.a * _TintStrength));
                    col.rgb = lerp(col.rgb, glass, glassMask);
                    col.a = lerp(col.a, 1.0, glassMask);
                }

                #ifdef UNITY_UI_CLIP_RECT
                col.a *= UnityGet2DClipping(IN.localPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip (col.a - 0.001);
                #endif

                return col;
            }
            ENDCG
        }
    }
}
