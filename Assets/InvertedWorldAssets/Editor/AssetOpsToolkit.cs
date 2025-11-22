using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public class AssetOpsToolkit : EditorWindow
{
    // --- 状态变量 ---
    private int selectedTab = 0;
    private string[] tabNames = { "Mesh 替换", "材质 替换", "动画 清理" };
    private Vector2 scrollPos;

    // --- 字段数据 (序列化以便绘制 List) ---
    [SerializeField] private DefaultAsset meshSourceFolder;
    [SerializeField] private DefaultAsset materialSourceFolder;
    
    [SerializeField] private GameObject animReferenceRoot;
    [SerializeField] private List<AnimationClip> animClipsToClean = new List<AnimationClip>();

    // 用于绘制 List 的序列化对象
    private SerializedObject so;
    private SerializedProperty propMeshFolder;
    private SerializedProperty propMatFolder;
    private SerializedProperty propAnimRoot;
    private SerializedProperty propAnimClips;

    [MenuItem("Tools/Asset Ops Toolkit")]
    public static void ShowWindow()
    {
        var window = GetWindow<AssetOpsToolkit>("资产工具箱");
        window.minSize = new Vector2(400, 500);
    }

    private void OnEnable()
    {
        // 初始化序列化属性，用于绘制原生风格的 Inspector
        so = new SerializedObject(this);
        propMeshFolder = so.FindProperty("meshSourceFolder");
        propMatFolder = so.FindProperty("materialSourceFolder");
        propAnimRoot = so.FindProperty("animReferenceRoot");
        propAnimClips = so.FindProperty("animClipsToClean");
    }

    private void OnGUI()
    {
        so.Update(); // 更新序列化数据

        // --- 标题栏 ---
        DrawHeader();

        // --- 顶部 Tab 导航 ---
        GUILayout.Space(10);
        selectedTab = GUILayout.Toolbar(selectedTab, tabNames, GUILayout.Height(30));
        GUILayout.Space(10);

        // --- 内容区域 ---
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        EditorGUILayout.BeginVertical("box"); // 外层大框
        GUILayout.Space(10);

        switch (selectedTab)
        {
            case 0: DrawMeshTab(); break;
            case 1: DrawMaterialTab(); break;
            case 2: DrawAnimTab(); break;
        }

        GUILayout.Space(10);
        EditorGUILayout.EndVertical();
        EditorGUILayout.EndScrollView();

        so.ApplyModifiedProperties(); // 保存修改
    }

    // =====================================================
    // UI 绘制方法
    // =====================================================

    private void DrawHeader()
    {
        var style = new GUIStyle(EditorStyles.boldLabel);
        style.fontSize = 18;
        style.alignment = TextAnchor.MiddleCenter;
        style.normal.textColor = EditorGUIUtility.isProSkin ? new Color(0.7f, 0.8f, 1f) : Color.black;
        
        GUILayout.Space(10);
        GUILayout.Label("Asset Operations Toolkit", style);
        GUILayout.Label("Unity 资产批量处理工具", EditorStyles.centeredGreyMiniLabel);
        DrawSeparator();
    }

    private void DrawMeshTab()
    {
        DrawSectionHeader("Mesh 批量替换", "根据名称自动匹配并替换 Mesh Filter。");

        EditorGUILayout.PropertyField(propMeshFolder, new GUIContent("新 Mesh 文件夹"));
        
        GUILayout.Space(15);
        ShowInfoBox($"当前选中物体数量: {Selection.gameObjects.Length}");

        if (GUILayout.Button("执行 Mesh 替换", GUILayout.Height(40)))
        {
            RunMeshReplacement();
        }
    }

    private void DrawMaterialTab()
    {
        DrawSectionHeader("材质 批量替换", "遍历所有材质槽，根据名称匹配新材质。");

        EditorGUILayout.PropertyField(propMatFolder, new GUIContent("新材质文件夹"));

        GUILayout.Space(15);
        ShowInfoBox($"当前选中物体数量: {Selection.gameObjects.Length}");

        if (GUILayout.Button("执行材质替换", GUILayout.Height(40)))
        {
            RunMaterialReplacement();
        }
    }

    private void DrawAnimTab()
    {
        DrawSectionHeader("动画 Missing 清理", "移除动画中找不到对象的黄色 Missing 曲线。");

        EditorGUILayout.PropertyField(propAnimRoot, new GUIContent("参照根物体 (Root)"));
        GUILayout.Space(5);
        EditorGUILayout.PropertyField(propAnimClips, new GUIContent("需处理的动画片段"), true);

        GUILayout.Space(15);
        
        if (GUILayout.Button("执行清理", GUILayout.Height(40)))
        {
            RunAnimCleanup();
        }
    }

    // --- UI 辅助 ---
    private void DrawSectionHeader(string title, string desc)
    {
        GUILayout.Label(title, EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(desc, MessageType.Info);
        GUILayout.Space(10);
    }

    private void ShowInfoBox(string message)
    {
        GUI.backgroundColor = new Color(0.8f, 0.8f, 0.8f, 0.2f);
        EditorGUILayout.BeginVertical("HelpBox");
        GUILayout.Label(message, EditorStyles.miniLabel);
        EditorGUILayout.EndVertical();
        GUI.backgroundColor = Color.white;
    }

    private void DrawSeparator()
    {
        GUILayout.Space(5);
        Rect rect = EditorGUILayout.GetControlRect(false, 1);
        EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.5f));
        GUILayout.Space(5);
    }

    // =====================================================
    // 核心逻辑实现
    // =====================================================

    // Logic 1: Mesh Replacement
    private void RunMeshReplacement()
    {
        if (meshSourceFolder == null) { ShowError("请先指定 Mesh 文件夹！"); return; }
        
        string folderPath = AssetDatabase.GetAssetPath(meshSourceFolder);
        GameObject[] selectedObjects = Selection.gameObjects;
        Undo.RecordObjects(selectedObjects, "Batch Mesh Replace");

        int successCount = 0;
        foreach (GameObject obj in selectedObjects)
        {
            MeshFilter mf = obj.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) continue;

            string oldName = mf.sharedMesh.name.Replace(" Instance", "").Trim();
            string[] guids = AssetDatabase.FindAssets(oldName + " t:Mesh", new[] { folderPath });

            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                Mesh newMesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if (newMesh != null)
                {
                    mf.sharedMesh = newMesh;
                    successCount++;
                }
            }
        }
        ShowSuccess($"Mesh 替换完成！更新了 {successCount} 个物体。");
    }

    // Logic 2: Material Replacement
    private void RunMaterialReplacement()
    {
        if (materialSourceFolder == null) { ShowError("请先指定材质文件夹！"); return; }

        string folderPath = AssetDatabase.GetAssetPath(materialSourceFolder);
        
        // 构建缓存
        Dictionary<string, Material> matCache = new Dictionary<string, Material>();
        string[] matGuids = AssetDatabase.FindAssets("t:Material", new[] { folderPath });
        foreach (var guid in matGuids)
        {
            Material m = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
            if (m != null && !matCache.ContainsKey(m.name)) matCache.Add(m.name, m);
        }

        GameObject[] selectedObjects = Selection.gameObjects;
        Undo.RecordObjects(selectedObjects, "Batch Material Replace");

        int objCount = 0;
        foreach (GameObject obj in selectedObjects)
        {
            Renderer r = obj.GetComponent<Renderer>();
            if (r == null) continue;

            Material[] sharedMats = r.sharedMaterials;
            bool changed = false;

            for (int i = 0; i < sharedMats.Length; i++)
            {
                if (sharedMats[i] == null) continue;
                string cleanName = sharedMats[i].name.Replace(" (Instance)", "").Trim();

                if (matCache.TryGetValue(cleanName, out Material newMat))
                {
                    if (sharedMats[i] != newMat)
                    {
                        sharedMats[i] = newMat;
                        changed = true;
                    }
                }
            }

            if (changed)
            {
                r.sharedMaterials = sharedMats;
                objCount++;
            }
        }
        ShowSuccess($"材质替换完成！更新了 {objCount} 个物体。");
    }

    // Logic 3: Animation Cleanup
    private void RunAnimCleanup()
    {
        if (animReferenceRoot == null) { ShowError("请指定参照物体 (Root)！"); return; }
        if (animClipsToClean.Count == 0) { ShowError("请添加至少一个动画片段！"); return; }

        int removedCount = 0;
        foreach (var clip in animClipsToClean)
        {
            if (clip == null) continue;

            var bindings = AnimationUtility.GetCurveBindings(clip).ToList();
            bindings.AddRange(AnimationUtility.GetObjectReferenceCurveBindings(clip));

            foreach (var binding in bindings)
            {
                Transform target = animReferenceRoot.transform.Find(binding.path);
                bool isMissing = (target == null);

                if (!isMissing && binding.type != typeof(Transform) && binding.type != typeof(GameObject))
                {
                    if (target.GetComponent(binding.type) == null) isMissing = true;
                }

                if (isMissing)
                {
                    AnimationUtility.SetEditorCurve(clip, binding, null);
                    AnimationUtility.SetObjectReferenceCurve(clip, binding, null);
                    removedCount++;
                }
            }
        }
        AssetDatabase.SaveAssets();
        ShowSuccess($"清理完成！共移除了 {removedCount} 条无效曲线。");
    }

    // --- 提示辅助 ---
    private void ShowError(string msg) => EditorUtility.DisplayDialog("错误", msg, "确定");
    private void ShowSuccess(string msg) => EditorUtility.DisplayDialog("成功", msg, "确定");
}