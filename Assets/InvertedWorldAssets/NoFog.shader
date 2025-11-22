Shader "Custom/StandardNoFog"
{
    Properties
    {
        // 主颜色
        _Color ("Color", Color) = (1,1,1,1)
        // 漫反射纹理
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        
        // 平滑度和金属度
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
        
        // 法线贴图
        _BumpMap ("Normal Map", 2D) = "bump" {}
        
        // 遮挡贴图 (Occlusion)
        _OcclusionStrength("Strength", Range(0.0, 1.0)) = 1.0
        _OcclusionMap("Occlusion", 2D) = "white" {}
        
        // 自发光
        _EmissionColor("Emission Color", Color) = (0,0,0)
        _EmissionMap("Emission", 2D) = "white" {}
    }

    SubShader
    {
        // 渲染队列和标签设置为不透明物体
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        // 核心指令：
        // 1. surface surf: 指定表面着色器函数名为 surf
        // 2. Standard: 使用 Unity 内置的基于物理的标准光照模型
        // 3. fullforwardshadows: 支持所有的投射阴影类型
        // 4. nofog: 【关键】完全禁用雾效计算
        #pragma surface surf Standard fullforwardshadows nofog

        // Shader Model 3.0 以支持更好的光照计算
        #pragma target 3.0

        sampler2D _MainTex;
        sampler2D _BumpMap;
        sampler2D _OcclusionMap;
        sampler2D _EmissionMap;

        struct Input
        {
            float2 uv_MainTex;
            float2 uv_BumpMap;
            float2 uv_OcclusionMap;
            float2 uv_EmissionMap;
        };

        half _Glossiness;
        half _Metallic;
        half _OcclusionStrength;
        fixed4 _Color;
        fixed4 _EmissionColor;

        // 实例化支持（用于 GPU Instancing，可选但推荐保留）
        UNITY_INSTANCING_BUFFER_START(Props)
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // --- Albedo (漫反射) ---
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb;

            // --- Metallic & Smoothness (金属度与平滑度) ---
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;

            // --- Normal (法线) ---
            o.Normal = UnpackNormal (tex2D (_BumpMap, IN.uv_BumpMap));

            // --- Occlusion (环境光遮蔽) ---
            // Standard Shader 默认只使用 G 通道作为遮蔽信息
            half occ = tex2D(_OcclusionMap, IN.uv_OcclusionMap).g;
            o.Occlusion = LerpOneTo(occ, _OcclusionStrength);

            // --- Emission (自发光) ---
            o.Emission = tex2D(_EmissionMap, IN.uv_EmissionMap) * _EmissionColor;
            
            // Alpha
            o.Alpha = c.a;
        }
        ENDCG
    }
    // 如果硬件不支持上述 Shader，回滚到 Diffuse
    FallBack "Diffuse"
}