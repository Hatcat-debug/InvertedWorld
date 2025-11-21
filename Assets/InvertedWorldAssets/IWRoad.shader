Shader "DancingLine/InvertedWorld/IWRoad"
{
    Properties
    {
        [Header(Basic)]
        _Color ("Main Color", Color) = (1,1,1,1)
        _MainTex ("Albedo", 2D) = "white" {}
        
        [Header(Outline)]
        _OutlineColor ("Outline Color", Color) = (0,0,0,1)
        _OutlineWidth ("Outline Width", Range(0, 0.2)) = 0.02
        
        [Header(Animation)]
        _AnimDuration ("Growth Duration", Float) = 0.5
        _StartOffset ("Start Vertical Offset", Float) = -10.0
        
        [Header(Curved World)]
        _CurveStrength ("Curve Strength", Float) = 0.001
        _CurveOriginZ ("Curve Origin Z", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry+10" }
        LOD 100

        CGINCLUDE
        #include "UnityCG.cginc"

        UNITY_INSTANCING_BUFFER_START(Props)
            UNITY_DEFINE_INSTANCED_PROP(float, _StartTime)
        UNITY_INSTANCING_BUFFER_END(Props)

        float _AnimDuration;
        float _StartOffset;
        float _CurveStrength;
        float _CurveOriginZ;

        float4 ApplyVertexModification(float4 vertex, float3 normal, float outlineWidth)
        {
            float spawnTime = UNITY_ACCESS_INSTANCED_PROP(Props, _StartTime);
            spawnTime = (spawnTime == 0) ? _Time.y : spawnTime;

            float progress = saturate((_Time.y - spawnTime) / _AnimDuration);
            float t = progress - 1;
            float ease = 1 + t * t * t;

            float3 pos = vertex.xyz + normal * outlineWidth;
            
            pos.y += _StartOffset * (1.0 - ease);

            float4 worldPos = mul(unity_ObjectToWorld, float4(pos, 1.0));

            float distZ = worldPos.z - _CurveOriginZ;
            float drop = distZ * distZ * _CurveStrength;
            worldPos.y -= drop;

            return worldPos;
        }
        ENDCG

        Pass
        {
            Name "StencilWrite"
            ZWrite On
            ColorMask 0
            
            Stencil
            {
                Ref 1
                Comp Always
                Pass Replace
            }
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            
            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);

                float4 worldPos = ApplyVertexModification(v.vertex, v.normal, 0);
                o.vertex = mul(UNITY_MATRIX_VP, worldPos);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return 0;
            }
            ENDCG
        }

        Pass
        {
            Name "Outline"
            Cull Front
            ZWrite On
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            
            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            float4 _OutlineColor;
            float _OutlineWidth;

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);

                float4 worldPos = ApplyVertexModification(v.vertex, v.normal, _OutlineWidth);
                o.vertex = mul(UNITY_MATRIX_VP, worldPos);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return _OutlineColor;
            }
            ENDCG
        }

        Pass
        {
            Name "Main"
            Tags { "LightMode"="ForwardBase" }
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            
            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);

                float4 worldPos = ApplyVertexModification(v.vertex, v.normal, 0);
                
                o.vertex = mul(UNITY_MATRIX_VP, worldPos);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv) * _Color;
                return col;
            }
            ENDCG
        }
    }
}