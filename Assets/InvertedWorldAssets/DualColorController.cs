using UnityEngine;

[ExecuteInEditMode]
public class DualColorController : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("分界线位置百分比 (0 = 全是颜色2, 1 = 全是颜色1)")]
    [Range(0f, 1f)] 
    public float splitPercentage = 0.5f;

    [Tooltip("分界轴向 (物体自身坐标系，例如 Y轴向上)")]
    public Vector3 splitAxis = Vector3.up;

    // 缓存组件
    private Renderer _renderer;
    private MeshFilter _meshFilter;
    private MaterialPropertyBlock _propBlock;
    
    // Shader 属性 ID 缓存 (性能优化)
    private static readonly int SplitValID = Shader.PropertyToID("_SplitVal");
    private static readonly int SplitAxisID = Shader.PropertyToID("_SplitAxis");

    void OnEnable()
    {
        Initialize();
        UpdateColorSplit();
    }

    void OnValidate()
    {
        // Inspector 数值变动时立即刷新
        Initialize();
        UpdateColorSplit();
    }

    // 如果你的物体在运行时会缩放或改变 Mesh，可以在 Update 中调用
    // void Update() { UpdateColorSplit(); }

    void Initialize()
    {
        if (_renderer == null) _renderer = GetComponent<Renderer>();
        if (_meshFilter == null) _meshFilter = GetComponent<MeshFilter>();
        if (_propBlock == null) _propBlock = new MaterialPropertyBlock();
    }

    public void UpdateColorSplit()
    {
        if (_renderer == null || _meshFilter == null) return;

        // 获取 Mesh 原始包围盒 (Object Space, Unscaled)
        Mesh mesh = _meshFilter.sharedMesh;
        if (mesh == null) return;
        
        Bounds bounds = mesh.bounds;

        // 归一化轴向
        Vector3 axis = splitAxis.normalized;
        
        // 计算包围盒在轴向上的投影范围
        // Center 投影
        float centerProj = Vector3.Dot(bounds.center, axis);
        // Extents 投影 (取绝对值确保方向正确)
        float extentProj = Vector3.Dot(bounds.extents, new Vector3(Mathf.Abs(axis.x), Mathf.Abs(axis.y), Mathf.Abs(axis.z)));

        // 获取最高点和最低点的数值
        float minVal = centerProj - extentProj;
        float maxVal = centerProj + extentProj;

        // 计算目标分割值
        // percentage 0 -> maxVal (让分界线跑到最上面，显示全是底色)
        // percentage 1 -> minVal (让分界线跑到最下面，显示全是顶色)
        // 注意：这里插值顺序取决于你想让 "0%" 代表什么。
        // 当前逻辑：0% = 全是 BottomColor，100% = 全是 TopColor
        float targetSplitVal = Mathf.Lerp(maxVal, minVal, splitPercentage);

        // 应用到 PropertyBlock
        _renderer.GetPropertyBlock(_propBlock);
        _propBlock.SetFloat(SplitValID, targetSplitVal);
        // 为了保险，我们也把轴向传进去，防止材质球设置错误
        _propBlock.SetVector(SplitAxisID, axis); 
        _renderer.SetPropertyBlock(_propBlock);
    }
}