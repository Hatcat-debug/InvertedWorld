Shader "Custom/DualColor"
{
    Properties
    {
        _Color ("Color Tint", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
        
        [Header(Dual Color Settings)]
        _TopColor ("Color 1 (Positive)", Color) = (1, 0, 0, 1)
        _BottomColor ("Color 2 (Negative)", Color) = (0, 0, 1, 1)
        
        [Space(10)]
        // 默认分界轴 (0,1,0)
        _SplitAxis ("Split Axis (Object Space)", Vector) = (0, 1, 0, 0) 
        
        // 由脚本控制的具体分界值
        [PerRendererData] _SplitVal ("Split Value", Float) = 0.0 
        
        _Blend ("Blend Softness", Range(0.001, 1)) = 0.05
    }
    
    SubShader
    {
        // 关键修改：DisableBatching = True 防止坐标系被合批破坏
        Tags { "RenderType"="Opaque" "DisableBatching"="True" }
        // Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        // 添加 keepalpha 以防透明度问题，虽然这里是 Opaque
        #pragma surface surf Standard fullforwardshadows vertex:vert keepalpha

        #pragma target 3.0

        sampler2D _MainTex;

        struct Input
        {
            float2 uv_MainTex;
            float3 localPos;
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;
        fixed4 _TopColor;
        fixed4 _BottomColor;
        float4 _SplitAxis;
        float _SplitVal;
        float _Blend;

        void vert (inout appdata_full v, out Input o) {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            // 因为禁用了 Batching，这里的 v.vertex 保证是物体局部坐标
            o.localPos = v.vertex.xyz;
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;

            // 1. 计算投影高度 (基于物体自身坐标系)
            float3 axis = normalize(_SplitAxis.xyz);
            float heightOnAxis = dot(IN.localPos, axis);

            // 2. 混合计算
            // 当 heightOnAxis > _SplitVal 时，mixFactor 趋向 1 (TopColor)
            // 当 heightOnAxis < _SplitVal 时，mixFactor 趋向 0 (BottomColor)
            float mixFactor = smoothstep(_SplitVal - _Blend, _SplitVal + _Blend, heightOnAxis);

            fixed4 dualColor = lerp(_BottomColor, _TopColor, mixFactor);

            o.Albedo = c.rgb * dualColor.rgb;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}