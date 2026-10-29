Shader "EasternDynasty/YouzhouGround"
{
    Properties
    {
        _GridColor("Grid", Color) = (.29,.29,.21,1)
        _BankDistance("Actual river bank distance",2D)="black"{}
        _RiverMap("Width height distance enabled",Vector)=(40,40,2,0)
    }
    SubShader
    {
        Tags { "Queue"="Geometry-100" "RenderType"="Opaque" }
        Pass
        {
            // The art contains a projected ground plane. Ground must not cut its front half away.
            ZWrite Off Cull Off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            struct appdata { float4 vertex:POSITION; float4 color:COLOR; float2 data:TEXCOORD1; };
            struct v2f { float4 pos:SV_POSITION; float3 world:TEXCOORD0; float4 color:COLOR; float2 data:TEXCOORD1; };
            float4 _GridColor;
            sampler2D _BankDistance;
            float4 _RiverMap;
            float hash(float2 p) { return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453); }
            float noise(float2 p) { float2 i=floor(p), f=frac(p); f=f*f*(3-2*f); return lerp(lerp(hash(i),hash(i+float2(1,0)),f.x),lerp(hash(i+float2(0,1)),hash(i+1),f.x),f.y); }
            v2f vert(appdata v) { v2f o; o.pos=UnityObjectToClipPos(v.vertex); o.world=mul(unity_ObjectToWorld,v.vertex).xyz; o.color=v.color; o.data=v.data; return o; }
            fixed4 frag(v2f i):SV_Target
            {
                float2 p=i.world.xz;
                float broad=noise(p*.42), detail=noise(p*9), flecks=noise(p*41);
                float3 color=i.color.rgb * (.89 + broad*.15 + detail*.055 + flecks*.025);
                // World-space soil and grass variation crosses chunk/cell seams.
                // This is a visual substrate, not another gameplay terrain type.
                float earth=smoothstep(.46,.76,noise(p*.73)+noise(p*2.1)*.13);
                color=lerp(color,color*float3(1.06,.98,.83),earth);
                float water=i.data.x*_RiverMap.w;
                if(water>.5)
                {
                    // Distances are built once from actual water/bridge cells.
                    // Every bend/chunk uses the same world-space bank field;
                    // all decorative shore irregularity stays within water cells.
                    float distance=tex2D(_BankDistance,(p+.5)/_RiverMap.xy).r*_RiverMap.z;
                    float scallop=(noise(p*5.3)-.5)*.095+(noise(p*14)-.5)*.025;
                    float depth=max(0,distance-scallop);
                    float aa=max(.012,length(fwidth(p))*.65);
                    float shore=smoothstep(.035,.10,depth);
                    float shallow=smoothstep(.14,.30,depth);
                    float channel=smoothstep(.25,1.05,depth);
                    float3 silt=float3(.56,.55,.38)*(.93+detail*.10);
                    float3 river=lerp(float3(.36,.57,.52),float3(.17,.37,.36),channel);
                    // Slow, broken surface streaks run along the river rather
                    // than restarting a high-frequency grid in each water tile.
                    float flow=p.y*15+p.x*3+noise(p*.9)*4-_Time.y*.34;
                    float ripple=pow(saturate(sin(flow)*.5+.5),9);
                    float streak=(.3+noise(float2(p.x*8,p.y*.7))*.7)*ripple;
                    float detailFade=1-smoothstep(.035,.14,length(fwidth(p)));
                    river+=streak*.052*detailFade*smoothstep(.22,.5,depth);
                    float wash=1-smoothstep(.016+aa,.047+aa,abs(depth-.17));
                    river=lerp(river,float3(.67,.70,.55),wash*.28);
                    color=lerp(color,lerp(silt,river,shallow),shore);
                }
                float2 edge=min(frac(p+.5),1-frac(p+.5));
                float2 pixel=edge/max(fwidth(p),float2(.0001,.0001));
                float gridLine=1-smoothstep(.35,1.15,min(pixel.x,pixel.y));
                // Neutral cell boundaries stay quiet; live ownership and selected
                // cell geometry provide the tactical grid above the artwork.
                color=lerp(color,_GridColor.rgb,gridLine*.075*(1-i.data.y)*(1-water));
                return float4(color,1);
            }
            ENDCG
        }
    }
}
