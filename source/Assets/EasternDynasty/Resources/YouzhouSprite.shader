Shader "EasternDynasty/YouzhouSprite"
{
    Properties { _MainTex("Atlas",2D)="white"{} _Cutoff("Alpha cutoff",Range(0,1))=.10 }
    SubShader
    {
        Tags { "Queue"="AlphaTest" "RenderType"="TransparentCutout" }
        Pass
        {
            Cull Off ZWrite On ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha
            AlphaToMask On
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex; float _Cutoff, _Overview;
            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; float2 data:TEXCOORD1; };
            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; float2 data:TEXCOORD1; };
            v2f vert(appdata v) { v2f o; o.pos=UnityObjectToClipPos(v.vertex); o.uv=v.uv; o.color=v.color; o.data=v.data; return o; }
            fixed4 frag(v2f i):SV_Target
            {
                fixed4 c=tex2Dbias(_MainTex,float4(i.uv,0,_Overview*1.5))*i.color;
                clip(c.a-_Cutoff);
                c.a*=1-_Overview*.7*(1-i.data.x);
                return c;
            }
            ENDCG
        }
    }
}
