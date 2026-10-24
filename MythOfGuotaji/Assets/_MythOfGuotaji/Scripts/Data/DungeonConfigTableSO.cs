using UnityEngine;

namespace MythOfGuotaji.Data
{
    [CreateAssetMenu(fileName = "DungeonConfigTable", menuName = "MythOfGuotaji/Tables/DungeonConfigTable")]
    public class DungeonConfigTableSO : ScriptableObject
    {
        [Header("Maze Architecture")]
        [Tooltip("정사각형 미로 그리드 크기 (N x N, 기본 6x6)")]
        public int gridSize = 6;

        [Tooltip("개별 방 한 변의 길이 (m, 150% 확대: 39m)")]
        public float roomSize = 39f;

        [Tooltip("외벽 높이 (m)")]
        public float wallHeight = 4.5f;

        [Tooltip("출입 통로 너비 (m)")]
        public float doorWidth = 12.0f;

        [Tooltip("추가 순환로(루프) 생성 비율 (0.0 ~ 1.0)")]
        [Range(0f, 1f)]
        public float extraLoopRatio = 0.25f;

        [Header("Encounter Rules")]
        [Tooltip("시작방으로부터 열쇠방(중간보스)의 최소 거리")]
        public int minDistanceForKeyRoom = 3;

        [Tooltip("일반방 요괴 스폰 수")]
        public int normalRoomMinionCount = 6;

        [Tooltip("함정방 요괴 웨이브 수")]
        public int trapRoomMinionCount = 10;

        [Tooltip("열쇠방 보조 잡몹 수")]
        public int keyRoomMinionCount = 5;

        [Header("Wave & Door Auto-Opening Rules")]
        [Tooltip("추가 웨이브(2차, 3차) 스폰 대기 시간 (초)")]
        public float waveInterval = 30.0f;

        [Tooltip("방 중심 원형 진입 자동 개방 반경 (m)")]
        public float roomCenterOpenRadius = 10.0f;

        [Tooltip("문 접근 자동 개방 거리 (m)")]
        public float doorApproachDistance = 7.0f;

        [Tooltip("추가 웨이브 스폰 연출 후 요괴 이동 개시 대기 시간 (초)")]
        public float waveSpawnPauseDuration = 0.3f;
    }
}
