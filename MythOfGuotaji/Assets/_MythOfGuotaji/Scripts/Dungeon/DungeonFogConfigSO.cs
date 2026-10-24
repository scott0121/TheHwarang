using UnityEngine;

namespace MythOfGuotaji.Dungeon
{
    /// <summary>
    /// 던전 포그(Fog of War) 및 시야각/거리 감쇄 수치를 관리하는 ScriptableObject
    /// - 전면부 시야각: 160도
    /// - 전방 시야: 0m~30m 완전 투명, 30m~45m 선형 페이드, 45m+ 완전 암전
    /// - 후방 시야: 캐릭터 주변 10m 항상 투명, 10m~15m 선형 페이드, 15m+ 완전 암전
    /// </summary>
    [CreateAssetMenu(fileName = "DungeonFogConfig", menuName = "MythOfGuotaji/Dungeon/DungeonFogConfig")]
    public class DungeonFogConfigSO : ScriptableObject
    {
        [Header("Fog Appearance")]
        [Tooltip("포그 기본 색상")]
        public Color fogColor = new Color(0.015f, 0.015f, 0.025f, 1.0f);

        [Range(0f, 1f)]
        [Tooltip("미탐험 방 및 암전 영역의 최대 불투명도")]
        public float maxDarkness = 0.90f;

        [Range(0f, 1f)]
        [Tooltip("이미 탐험했던 방의 반어둠 불투명도")]
        public float exploredDarkness = 0.50f;

        [Header("Front Vision (160 Degrees)")]
        [Tooltip("전면부 시야각 (기본 160도)")]
        public float fovAngleDegrees = 160f;

        [Range(0.01f, 0.2f)]
        [Tooltip("160도 시야각 경계선의 부드러운 페이드 계수")]
        public float fovEdgeSoftness = 0.05f;

        [Header("Front Distance Visibility")]
        [Tooltip("전방 완전 투명 거리 (기본 30m)")]
        public float clearDistance = 30f;

        [Tooltip("전방 완전 암전 거리 (기본 45m)")]
        public float falloffDistance = 45f;

        [Header("Rear & Omnidirectional Vision Range")]
        [Tooltip("캐릭터 주변 항상 보이는 거리 (기본 10m)")]
        public float alwaysClearRadius = 10f;

        [Tooltip("뒷면 페이드 종료 및 완전 암전 거리 (기본 15m)")]
        public float rearDarkDistance = 15f;

        [Header("Doorway Vision")]
        [Tooltip("문 가림 시야각 페이드 범위 (기본 15도)")]
        public float doorFadeAngle = 15f;

        [Tooltip("문 개방 통로 반너비")]
        public float doorHalfWidth = 6.0f;
    }
}
