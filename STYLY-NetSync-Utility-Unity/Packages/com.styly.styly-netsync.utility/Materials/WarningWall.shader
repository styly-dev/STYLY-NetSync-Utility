Shader "NetSyncUtility/WarningWall"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Scale ("Line Scale", Float) = 10
        _Thickness ("Line Thickness", Range(0.001, 0.1)) = 0.02
        _FadeStart ("Fade Start Distance", Float) = 5
        _FadeEnd ("Fade End Distance", Float) = 15
        [Toggle] _AlwaysVisible ("Always Visible", Float) = 0
        [Enum(Off,0, Front,1, Back,2)] _CullMode ("Cull Mode", Float) = 2
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull [_CullMode]
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float3 worldNormal : TEXCOORD1;
                float3 worldPos : TEXCOORD2;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _Scale;
            float _Thickness;
            float _FadeStart;
            float _FadeEnd;
            float _AlwaysVisible;
            
            // Helper: diagonal lattice mask for one plane (HLSL/CG function)
            float LatticeMask(float2 p, float s, float t)
            {
                float a = frac((p.x + p.y) * s);
                float b = frac((p.x - p.y) * s);
                float da = min(a, 1.0 - a);
                float db = min(b, 1.0 - b);
                float fwa = fwidth(a);
                float fwb = fwidth(b);
                float ma = 1.0 - smoothstep(t, t + fwa, da);
                float mb = 1.0 - smoothstep(t, t + fwb, db);
                return max(ma, mb);
            }
            


            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Build a diagonal lattice mask using UVs
                // Two 45-degree sets: (u+v) and (u-v)
                float s = _Scale;
                float t = _Thickness;

                // Triplanar: blend XY, XZ, YZ projections by world normal so orientation doesn't affect pattern
                float3 n = normalize(i.worldNormal);
                float3 wn = abs(n);
                // Sharpen weights slightly to reduce seams
                wn = pow(wn, 2.0);
                float wsum = max(wn.x + wn.y + wn.z, 1e-5);
                float3 w = wn / wsum;

                
                float mXY = LatticeMask(i.worldPos.xy, s, t);
                float mXZ = LatticeMask(i.worldPos.xz, s, t);
                float mYZ = LatticeMask(i.worldPos.yz, s, t);

                float m = mXY * w.z + mXZ * w.y + mYZ * w.x;

                // Distance fade: as camera gets farther, fade out between _FadeStart and _FadeEnd
                float fade = 1.0;
                if (_AlwaysVisible < 0.5)
                {
                    float distCam = distance(_WorldSpaceCameraPos, i.worldPos);
                    fade = saturate(1.0 - smoothstep(_FadeStart, _FadeEnd, distCam));
                }
                m *= fade;

                // Discard fragments not on the lines
                if (m <= 0.0) discard;

                // Red color for lines
                fixed4 col = fixed4(1.0, 0.0, 0.0, 1.0) * m;

                // Apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
