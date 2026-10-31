using System;
using System.Collections.Generic;
using UnityEngine;

namespace MythOfGuotaji.Data
{
    [Serializable]
    public class HelperEntry
    {
        [Tooltip("헬퍼 고유 식별자 (e.g. Helper_00, Helper_01, Helper_02)")]
        public string helperId = "Helper_00";

        [Tooltip("헬퍼 표시 이름")]
        public string displayName = "달빛 영혼 토끼";

        [TextArea(2, 4)]
        [Tooltip("헬퍼 설명 및 특징")]
        public string description = "전투력은 없으나 플레이어 뒤를 따르며, 요괴가 없는 안전 구역의 영혼석을 3미터 진공 흡수하여 비술을 제련해주는 영물입니다.";

        [Tooltip("동료 헬퍼 본체 프리팹")]
        public GameObject companionPrefab;

        [Tooltip("동료 헬퍼 발밑 아우라 VFX 프리팹")]
        public GameObject auraPrefab;

        [Header("Movement & Range")]
        [Tooltip("보석 수집 이동 속도")]
        public float moveSpeed = 6.8f;

        [Tooltip("위험 시 플레이어로 후퇴 속도")]
        public float fleeSpeed = 8.8f;

        [Tooltip("진공 흡수 반경 (기본 3.0m)")]
        public float vacuumRadius = 3.0f;

        [Tooltip("보석 안전 구역 판정 거리 (보석 주변에 몬스터가 없는 거리)")]
        public float gemSafetyRadius = 8.5f;

        [Tooltip("헬퍼 자신에게 요괴가 접근했을 때 도망치는 거리")]
        public float fleeDistance = 5.5f;

        [Header("Visuals")]
        [Tooltip("외형 스케일 배율")]
        public float scale = 0.85f;
    }

    [CreateAssetMenu(fileName = "HelperData", menuName = "MythOfGuotaji/Data/HelperData")]
    public class HelperDataSO : ScriptableObject
    {
        [Header("Helper Catalog")]
        public List<HelperEntry> helpers = new();

        /// <summary>
        /// ID로 헬퍼 정보 검색 (없거나 null이면 첫 번째 또는 기본 엔트리 반환)
        /// </summary>
        public HelperEntry GetHelper(string id)
        {
            if (helpers != null && helpers.Count > 0)
            {
                if (!string.IsNullOrEmpty(id))
                {
                    for (int i = 0; i < helpers.Count; i++)
                    {
                        if (helpers[i] != null && helpers[i].helperId == id)
                        {
                            return helpers[i];
                        }
                    }
                    // "Helper_SpiritRabbit" 레거시 ID 호환
                    if (id == "Helper_SpiritRabbit")
                    {
                        return helpers[0];
                    }
                }
                return helpers[0];
            }
            return null;
        }

        // ==========================================
        // 기존 코드 하위 호환용 프로퍼티 (첫 번째 헬퍼 위임)
        // ==========================================
        public string helperId => helpers.Count > 0 ? helpers[0].helperId : "Helper_00";
        public string displayName => helpers.Count > 0 ? helpers[0].displayName : "달빛 영혼 토끼";
        public string description => helpers.Count > 0 ? helpers[0].description : "";
        public float moveSpeed => helpers.Count > 0 ? helpers[0].moveSpeed : 6.8f;
        public float fleeSpeed => helpers.Count > 0 ? helpers[0].fleeSpeed : 8.8f;
        public float vacuumRadius => helpers.Count > 0 ? helpers[0].vacuumRadius : 3.0f;
        public float gemSafetyRadius => helpers.Count > 0 ? helpers[0].gemSafetyRadius : 8.5f;
        public float fleeDistance => helpers.Count > 0 ? helpers[0].fleeDistance : 5.5f;
        public float scale => helpers.Count > 0 ? helpers[0].scale : 0.85f;
        public GameObject companionPrefab => helpers.Count > 0 ? helpers[0].companionPrefab : null;
        public GameObject auraPrefab => helpers.Count > 0 ? helpers[0].auraPrefab : null;
    }
}
