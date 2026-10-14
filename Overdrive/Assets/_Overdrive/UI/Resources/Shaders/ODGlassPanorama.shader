// OverDrive 2026 — Accumulates the blurred passthrough camera frames into a
// world-anchored panorama (equirectangular: u = yaw, v = pitch) for the glass
// panels. Each texel is a direction around the user; if the current camera
// frame sees that direction, its blurred colour is blended in, otherwise the
// previous value is kept. The panorama therefore remembers every direction the
// camera has seen, so a panel stays frosted when it leaves the camera's field
// of view (narrower than the headset display).
//
// Stored premultiplied: rgb = colour * coverage, a = coverage (0 = never seen).
// Blit from the previous panorama (_MainTex) into the next one.
Shader "Hidden/OD/GlassPanorama"
{
    Properties
    {
        _MainTex ("Previous panorama", 2D) = "black" {}
        _CamTex ("Blurred camera frame", 2D) = "black" {}
        _Weight ("Blend weight of the new frame", Range(0, 1)) = 0.65
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            sampler2D _CamTex;
            float _Weight;
            float4x4 _CamWorldToLocal;   // world -> camera rotation (translation ignored: the panorama is at infinity)
            float4 _CamIntrinsics;       // focal x, focal y, principal x, principal y (sensor pixels)
            float4 _CamCrop;             // sensor crop x, y, width, height (sensor pixels)
            float _CamFlipY;             // 1 if the camera texture is stored top-down

            float4 frag (v2f_img i) : SV_Target
            {
                float4 previous = tex2D(_MainTex, i.uv);

                float yaw   = (i.uv.x - 0.5) * UNITY_TWO_PI;
                float pitch = (i.uv.y - 0.5) * UNITY_PI;
                float3 dir  = float3(sin(yaw) * cos(pitch), sin(pitch), cos(yaw) * cos(pitch));

                float3 p = mul((float3x3)_CamWorldToLocal, dir);
                if (p.z < 0.05) return previous;

                float2 sensor = p.xy / p.z * _CamIntrinsics.xy + _CamIntrinsics.zw;
                float2 uv = (sensor - _CamCrop.xy) / _CamCrop.zw;
                uv.y = lerp(uv.y, 1.0 - uv.y, _CamFlipY);

                // Feathered border so frames stitch without seams.
                float edge = saturate(min(min(uv.x, 1.0 - uv.x), min(uv.y, 1.0 - uv.y)) * 8.0);
                if (edge <= 0.0) return previous;

                float3 cam = tex2D(_CamTex, saturate(uv)).rgb;
                return lerp(previous, float4(cam, 1.0), edge * _Weight);
            }
            ENDCG
        }
    }
}
