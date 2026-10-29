Shader "EasternDynasty/YouzhouOverlay"
{
    Properties { _Color("Tint",Color)=(1,1,1,1) }
    SubShader
    {
        Tags { "Queue"="Transparent+10" "RenderType"="Transparent" }
        Pass
        {
            Cull Off ZWrite Off ZTest Always Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float4 _Color;
            struct appdata { float4 vertex:POSITION; float4 color:COLOR; };
            struct v2f { float4 pos:SV_POSITION; float4 color:COLOR; };
            v2f vert(appdata v) { v2f o; o.pos=UnityObjectToClipPos(v.vertex); o.color=v.color*_Color; return o; }
            fixed4 frag(v2f i):SV_Target { return i.color; }
            ENDCG
        }
    }
}
