Shader "EasternDynasty/WorldLandscapeLit"
{
    Properties
    {
        _Color ("Tint", Color) = (1,1,1,1)
        _Smoothness ("Smoothness", Range(0,1)) = 0.12
        _Detail ("Ground detail / Water", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows vertex:vert addshadow
        #pragma target 3.0
        fixed4 _Color; half _Smoothness; half _Detail;
        struct Input { float4 color : COLOR; float3 worldPos; };
        void vert(inout appdata_full v, out Input o)
        { UNITY_INITIALIZE_OUTPUT(Input, o); o.color = v.color; }
        float hash21(float2 p) { return frac(sin(dot(p, float2(127.1,311.7))) * 43758.5453); }
        float noise21(float2 p)
        {
            float2 i = floor(p), f = frac(p); f = f*f*(3-2*f);
            return lerp(lerp(hash21(i),hash21(i+float2(1,0)),f.x),lerp(hash21(i+float2(0,1)),hash21(i+1),f.x),f.y);
        }
        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float3 color = IN.color.rgb * _Color.rgb;
            if (_Detail > 1.5)
            {
                float ripple = sin(IN.worldPos.x*28 + IN.worldPos.z*9 + _Time.y*.65) * sin(IN.worldPos.z*18 - _Time.y*.45);
                color *= .95 + .07*ripple; o.Smoothness = .58;
            }
            else
            {
                if (_Detail > .5)
                {
                    float2 footprint = fwidth(IN.worldPos.xz);
                    float fineWeight = saturate(1 - max(footprint.x,footprint.y)*32);
                    color *= .94 + noise21(IN.worldPos.xz*.85)*.075 + noise21(IN.worldPos.xz*32)*.025*fineWeight;
                }
                o.Smoothness = _Smoothness;
            }
            o.Albedo = color; o.Metallic = 0; o.Occlusion = 1; o.Alpha = 1;
        }
        ENDCG
    }
    Fallback "Diffuse"
}
