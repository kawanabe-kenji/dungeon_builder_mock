Shader "Custom/WaterSurface"
{
    Properties
    {
        _MainColor ("Main Color", Color) = (1,1,1,1)
        _Cube ("Cube", CUBE) = "gray" {}

        _BumpMap ("Normal Map", 2D) = "bump" {}
        _BumpScale ("Normal Scale", Range(0, 2)) = 1

        _AlphaWeight ("Alpha Weight", Range(0, 1)) = 0
    }
    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "Queue"="Transparent"
        }
        Blend SrcAlpha OneMinusSrcAlpha
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
            };

            struct v2f
            {
                //float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                float3 normal : NORMAL;
                float3 toEye : TEXCOORD2;
                float2 uvNormal : TEXCOORD3;
                float4 tangent  : TANGENT;
                float3 binormal : TEXCOORD4;
            };

            float4 _MainColor;

            samplerCUBE _Cube;
            float4 _Cube_ST;

            sampler2D _BumpMap;
            float4 _BumpMap_ST;

            float _BumpScale;

            float _AlphaWeight;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                //o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);

                o.normal = UnityObjectToWorldNormal(v.normal);
                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.toEye = normalize(UnityWorldSpaceViewDir(worldPos));

                o.uvNormal = TRANSFORM_TEX(v.uv, _BumpMap);
                o.tangent = v.tangent;
                o.tangent.xyz = UnityObjectToWorldDir(v.tangent.xyz);
                o.binormal = normalize(cross(v.normal, v.tangent.xyz) * v.tangent.w * unity_WorldTransformParams.w);
                return o;
            }

            #define F0 0.02

            fixed4 frag (v2f i) : SV_Target
            {
                float3 localNormal = UnpackNormalWithScale(tex2D(_BumpMap, i.uvNormal), _BumpScale);
                i.normal = i.tangent * localNormal.x + i.binormal * localNormal.y + i.normal * localNormal.z;

                // sample the texture
                half3 reflDir = reflect(-i.toEye, i.normal);
                float4 col = texCUBE(_Cube, reflDir) * _MainColor;

                half vdotn = dot(i.toEye, i.normal);
                half fresnel = F0 + (1 - F0) * pow(1 - vdotn, 5);
                col.a = lerp(fresnel, 1, _AlphaWeight);

                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
