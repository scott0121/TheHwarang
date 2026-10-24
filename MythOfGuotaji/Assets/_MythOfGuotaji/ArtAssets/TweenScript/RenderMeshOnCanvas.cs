using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class RenderMeshOnCanvas : MaskableGraphic
{
    [Header("Mesh Sources")]
    public MeshFilter meshFilter;
    public SkinnedMeshRenderer skinnedMeshRenderer;

    private Mesh staticMesh;
    private Mesh bakedMesh;

    protected override void Awake()
    {
        base.Awake();
        Initialize();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        Initialize();
        SetAllDirty();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        if (bakedMesh != null)
        {
            if (Application.isPlaying) Destroy(bakedMesh);
            else DestroyImmediate(bakedMesh);
        }
    }

    private void Initialize()
    {
        if (meshFilter == null) meshFilter = GetComponentInChildren<MeshFilter>();
        if (skinnedMeshRenderer == null) skinnedMeshRenderer = GetComponentInChildren<SkinnedMeshRenderer>();

        if (meshFilter != null)
        {
            staticMesh = meshFilter.sharedMesh;
        }

        if (skinnedMeshRenderer != null && bakedMesh == null)
        {
            bakedMesh = new Mesh();
            bakedMesh.name = "UI_Baked_Skinned_Mesh";
        }

        if (skinnedMeshRenderer != null)
        {
            // [핵심 수정 1] enabled = false 대신 forceRenderingOff 사용
            // Animator가 강제로 렌더러를 켜는 것을 방지합니다. 
            // 3D 카메라에서는 절대 그려지지 않지만, 애니메이션 뼈대는 정상 작동합니다.
            skinnedMeshRenderer.forceRenderingOff = true;
        }
    }

    // [핵심 수정 2] Update 대신 LateUpdate 사용
    private void LateUpdate()
    {
        if (skinnedMeshRenderer != null && Application.isPlaying)
        {
            // Animator가 애니메이션 연산을 모두 끝낸 직후(LateUpdate)에
            // UI 시스템에게 메쉬를 갱신하라고 요청해야 정확한 형태가 구워집니다.
            SetVerticesDirty();
            SetMaterialDirty();
        }
    }

    protected override void UpdateGeometry()
    {
        if (canvasRenderer == null) return;

        if (skinnedMeshRenderer != null)
        {
            // UI가 다시 그려지는 순간(Canvas Rebuild)에 최신 형태를 굽고 전달합니다.
            skinnedMeshRenderer.BakeMesh(bakedMesh);
            canvasRenderer.SetMesh(bakedMesh);
        }
        else if (meshFilter != null && staticMesh != null)
        {
            canvasRenderer.SetMesh(staticMesh);
        }
        else
        {
            base.UpdateGeometry();
        }
    }
}