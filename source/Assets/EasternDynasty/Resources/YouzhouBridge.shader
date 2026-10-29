Shader "EasternDynasty/YouzhouBridge"
{
    SubShader
    {
        Tags { "Queue"="Geometry+20" "RenderType"="Opaque" }
        Pass
        {
            // Structural sides, posts and decking share real depth; territory,
            // selection and route feedback still draw in their later overlay pass.
            Cull Off ZWrite On ZTest LEqual
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex:POSITION; float4 color:COLOR; };
            struct v2f { float4 pos:SV_POSITION; float4 color:COLOR; float3 world:TEXCOORD0; };
            v2f vert(appdata v) { v2f o; o.pos=UnityObjectToClipPos(v.vertex); o.color=v.color; o.world=mul(unity_ObjectToWorld,v.vertex).xyz; return o; }
            fixed4 frag(v2f i):SV_Target
            {
                float grain=sin(i.world.z*95+sin(i.world.x*17)*2);
                float fade=1-saturate(length(fwidth(i.world.xz))*35);
                return float4(i.color.rgb*(.98+grain*.025*fade),1);
            }
            ENDCG
        }
    }
}
