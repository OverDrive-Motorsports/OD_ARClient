// OverDrive 2026 — Kawase blur pass used by ODPassthroughBlur to frost the
// passthrough camera image behind the visionOS glass panels. Each Blit samples
// the four diagonal neighbours at _Offset texels; chaining passes with growing
// offsets on a small render texture gives a wide, cheap blur.
Shader "Hidden/OD/GlassBlur"
{
    Properties
    {
        _MainTex ("Source", 2D) = "white" {}
        _Offset ("Offset (texels)", Float) = 1
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
            float4 _MainTex_TexelSize;
            float _Offset;

            fixed4 frag (v2f_img i) : SV_Target
            {
                float2 o = _MainTex_TexelSize.xy * (_Offset + 0.5);
                fixed4 c = tex2D(_MainTex, i.uv + float2( o.x,  o.y));
                c       += tex2D(_MainTex, i.uv + float2(-o.x,  o.y));
                c       += tex2D(_MainTex, i.uv + float2( o.x, -o.y));
                c       += tex2D(_MainTex, i.uv + float2(-o.x, -o.y));
                return c * 0.25;
            }
            ENDCG
        }
    }
}
