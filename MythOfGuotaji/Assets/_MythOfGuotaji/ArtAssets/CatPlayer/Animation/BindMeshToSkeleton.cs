using UnityEngine;
using System.Collections.Generic;

[ExecuteAlways]
public class BindMeshToNewSkeleton : MonoBehaviour
{
    [SerializeField] private GameObject m_boneRoot = null;
    [SerializeField] private SkinnedMeshRenderer m_partSMR = null;

    [Header("Culling Fix Settings")]
    [Tooltip("화면 밖 컬링 문제를 원천 차단하려면 체크하세요.")]
    [SerializeField] private bool m_updateWhenOffscreen = true;
    [Tooltip("바운딩 박스 크기 여유 배수 (기본 1.5 ~ 2.0 권장)")]
    [SerializeField] private float m_boundsMultiplier = 2.0f;

    private void Start()
    {
        TryBind();
    }

    [ContextMenu("Bind Mesh Now")]
    public void TryBind()
    {
        if (m_partSMR == null || m_boneRoot == null)
            return;

        BindMeshToSkeleton(m_partSMR, m_boneRoot.transform);
    }

    public void BindMeshToSkeleton(SkinnedMeshRenderer partSMR, Transform mainSkeletonRoot)
    {
        if (partSMR.sharedMesh == null)
        {
            Debug.LogWarning("[BindMesh] Part sharedMesh is missing.");
            return;
        }

        // 1. 메인 골격 매핑 (중복 이름 방어: 최초 검색된 본 유지)
        var mainBones = mainSkeletonRoot.GetComponentsInChildren<Transform>(true);
        var boneMap = new Dictionary<string, Transform>();
        foreach (var b in mainBones)
        {
            if (!boneMap.ContainsKey(b.name))
                boneMap.Add(b.name, b);
        }

        // 2. 파츠 본 리스트 재매핑 (null 방어)
        Transform[] originalBones = partSMR.bones;
        Transform[] newBones = new Transform[originalBones.Length];

        for (int i = 0; i < originalBones.Length; i++)
        {
            if (originalBones[i] == null)
            {
                newBones[i] = null;
                continue;
            }

            string boneName = originalBones[i].name;
            if (boneMap.TryGetValue(boneName, out var foundBone))
            {
                newBones[i] = foundBone;
            }
            else
            {
                Debug.LogWarning($"[BindMesh] Missing bone on target skeleton: {boneName}");
            }
        }

        // 3. 본 및 루트 본 할당
        partSMR.bones = newBones;

        if (partSMR.rootBone != null && boneMap.TryGetValue(partSMR.rootBone.name, out var newRootBone))
        {
            partSMR.rootBone = newRootBone;
        }
        else
        {
            partSMR.rootBone = mainSkeletonRoot;
        }

        // 4. 컬링 버그 해결 (중요: partSMR.bounds에 직접 대입하면 월드 원점에 고정되므로 절대 사용 금지)
        partSMR.updateWhenOffscreen = m_updateWhenOffscreen;

        if (!m_updateWhenOffscreen)
        {
            // updateWhenOffscreen을 끄고 최적화할 경우, localBounds를 루트 본 기준으로 넉넉하게 재설정
            Bounds meshBounds = partSMR.sharedMesh.bounds;
            partSMR.localBounds = new Bounds(Vector3.zero, meshBounds.size * m_boundsMultiplier);
        }
    }
}