Shader "EasternDynasty/MapLit"
{
    Properties { _Color ("Color", Color) = (1,1,1,1) }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            fixed4 _Color;
            struct appdata { float4 vertex:POSITION; float3 normal:NORMAL; };
            struct v2f { float4 vertex:SV_POSITION; float light:TEXCOORD0; };
            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.light = .72 + .28 * max(0, dot(UnityObjectToWorldNormal(v.normal), normalize(float3(-.5,1,-.4))));
                return o;
            }
            fixed4 frag(v2f i):SV_Target { return fixed4(_Color.rgb * i.light, _Color.a); }
            ENDCG
        }
    }
}
