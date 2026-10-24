using UnityEngine;

namespace MythOfGuotaji.Data
{
    [CreateAssetMenu(fileName = "HelperData", menuName = "MythOfGuotaji/Data/HelperData")]
    public class HelperDataSO : ScriptableObject
    {
        [Header("Identity")]
        public string helperId = "Helper_SpiritRabbit";
        public string displayName = "달빛 영혼 토끼";
        [TextArea(2, 4)]
        public string description = "전투력은 없으나 플레이어 뒤를 따르며, 요괴가 없는 안전 구역의 영혼석을 3미터 진공 흡수하여 비술을 제련해주는 영물입니다.";

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
        public Color bodyColor = new Color(0.9f, 0.95f, 1.0f);
        public Color auraColor = new Color(0.2f, 0.9f, 0.8f, 0.35f);
        public float scale = 0.85f;
    }
}
