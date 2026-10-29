Shader "EasternDynasty/YouzhouResource"
{
    Properties { _MainTex("Historical resource reference",2D)="white"{} }
    SubShader
    {
        Tags { "Queue"="AlphaTest-10" "RenderType"="Transparent" }
        Pass
        {
            // Feathered ground decals must not occlude city sprites or leave a
            // rectangular depth mask. Selection and ownership render afterwards.
            Cull Off ZWrite Off ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            sampler2D _MainTex; float4 _MainTex_TexelSize; float _Overview;
            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; float2 local:TEXCOORD1; };
            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; float2 local:TEXCOORD1; float2 ground:TEXCOORD2; };
            v2f vert(appdata v)
            {
                v2f o; o.pos=UnityObjectToClipPos(v.vertex); o.uv=v.uv;
                o.local=v.local; o.ground=mul(unity_ObjectToWorld,v.vertex).xz; return o;
            }
            fixed4 frag(v2f i):SV_Target
            {
                fixed4 c=tex2D(_MainTex,i.uv);
                // The existing reference import intentionally has no mip chain.
                // Use a small footprint filter instead of an ineffective LOD bias.
                float2 footprint=max(_MainTex_TexelSize.xy,(abs(ddx(i.uv))+abs(ddy(i.uv)))*.5);
                // This is a combined historical image, not a padded atlas. Keep
                // taps inside the measured UV inset instead of bleeding neighbours.
                float2 radius=min(footprint,_MainTex_TexelSize.xy*2)*_Overview;
                fixed4 filtered=(tex2D(_MainTex,i.uv+float2(radius.x,0))+tex2D(_MainTex,i.uv-float2(radius.x,0))
                    +tex2D(_MainTex,i.uv+float2(0,radius.y))+tex2D(_MainTex,i.uv-float2(0,radius.y)))*.25;
                c=lerp(c,filtered,_Overview*.65);
                float2 edge=min(i.local,1-i.local);
                float fringe=.012*sin(i.ground.x*41+i.ground.y*23)*sin(i.ground.y*37);
                float coverage=smoothstep(.012,.105,min(edge.x,edge.y)+fringe);
                // The small original image includes very bright yellow fields.
                // Keep their identity while restoring the city/territory hierarchy.
                float luminance=dot(c.rgb,float3(.299,.587,.114));
                c.rgb=lerp(luminance.xxx,c.rgb,.72);
                c.rgb*=float3(.94,.95,.88);
                c.a*=coverage*(1-_Overview*.16);
                return c;
            }
            ENDCG
        }
    }
}
