using System.Collections.Generic;
using MythOfGuotaji.Actor;
using UnityEngine;

namespace MythOfGuotaji.Dungeon
{
    /// <summary>
    /// 던전 전용 URP 포그 셰이더와 연동되는 시야/안개(Fog of War) 총괄 매니저
    /// - DungeonFogConfigSO를 통해 전면 160도, 후방 10m~15m, 거리 감쇄 등 모든 수치 테이블화
    /// - 셰이더에 캐릭터 실시간 월드 위치, 전방 조준 벡터, 현재 방 중심 좌표, 방 반너비 주입
    /// - 3단계 룸 상태 (현재 방: 투명 / 탐험한 방: 반 어둠 50% / 미탐험 방: 90% 어둠)
    /// </summary>
    public class DungeonFogManager : MonoBehaviour
    {
        public static DungeonFogManager Instance { get; private set; }

        [Header("Configuration")]
        [SerializeField] private DungeonFogConfigSO _config;

        private readonly HashSet<Vector2Int> _visitedRooms = new();
        private Vector2Int _currentRoomPos = new Vector2Int(-999, -999);
        private Transform _playerTransform;
        private PlayerController _playerController;

        public Vector2Int CurrentRoomPos => _currentRoomPos;
        public IReadOnlyCollection<Vector2Int> VisitedRooms => _visitedRooms;
        public DungeonFogConfigSO Config => _config;

        private static readonly int CharacterWorldPosId = Shader.PropertyToID("_CharacterWorldPos");
        private static readonly int CharacterForwardId = Shader.PropertyToID("_CharacterForward");
        private static readonly int CurrentRoomCenterId = Shader.PropertyToID("_CurrentRoomCenter");
        private static readonly int CurrentRoomHalfSizeId = Shader.PropertyToID("_CurrentRoomHalfSize");
        private static readonly int ActiveDoorCountId = Shader.PropertyToID("_ActiveDoorCount");
        private static readonly int DoorData0Id = Shader.PropertyToID("_DoorData0");
        private static readonly int DoorData1Id = Shader.PropertyToID("_DoorData1");
        private static readonly int DoorData2Id = Shader.PropertyToID("_DoorData2");
        private static readonly int DoorData3Id = Shader.PropertyToID("_DoorData3");

        // Shader Config Property IDs
        private static readonly int FovAngleId = Shader.PropertyToID("_FovAngle");
        private static readonly int FovSoftnessId = Shader.PropertyToID("_FovSoftness");
        private static readonly int AlwaysClearRadiusId = Shader.PropertyToID("_AlwaysClearRadius");
        private static readonly int RearDarkDistanceId = Shader.PropertyToID("_RearDarkDistance");
        private static readonly int ClearDistanceId = Shader.PropertyToID("_ClearDistance");
        private static readonly int FalloffDistanceId = Shader.PropertyToID("_FalloffDistance");
        private static readonly int DoorFadeAngleId = Shader.PropertyToID("_DoorFadeAngle");
        private static readonly int FogColorId = Shader.PropertyToID("_FogColor");

        private readonly Vector4[] _cachedDoorData = new Vector4[4];

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            LoadConfigIfNull();
        }

        public void LoadConfigIfNull()
        {
            if (_config == null)
            {
                _config = Resources.Load<DungeonFogConfigSO>("Data/DungeonFogConfig");
                if (_config == null)
                {
                    _config = ScriptableObject.CreateInstance<DungeonFogConfigSO>();
                }
            }
        }

        private void Start()
        {
            FindPlayer();
            ApplyConfigToShader();
        }

        private void FindPlayer()
        {
            var p = FindAnyObjectByType<PlayerController>();
            if (p != null)
            {
                _playerController = p;
                _playerTransform = p.transform;
            }
        }

        public void ResetFog()
        {
            _visitedRooms.Clear();
            _currentRoomPos = new Vector2Int(-999, -999);
        }

        private void LateUpdate()
        {
            if (_playerTransform == null)
            {
                FindPlayer();
                if (_playerTransform == null) return;
            }

            if (MazeGenerator.Instance == null) return;

            Vector3 playerPos = _playerTransform.position;
            Vector2Int playerRoom = MazeGenerator.Instance.GetRoomCoordsAtWorldPos(playerPos);

            if (playerRoom != _currentRoomPos)
            {
                OnPlayerChangedRoom(playerRoom);
            }

            UpdateShaderGlobalParameters(playerPos);
        }

        private void OnPlayerChangedRoom(Vector2Int newRoom)
        {
            _currentRoomPos = newRoom;
            _visitedRooms.Add(newRoom);

            UpdateAllRoomFogStates();
        }

        public void UpdateAllRoomFogStates()
        {
            if (MazeGenerator.Instance == null) return;

            float maxDark = _config != null ? _config.maxDarkness : 0.90f;
            float exploredDark = _config != null ? _config.exploredDarkness : 0.50f;

            foreach (var kvp in MazeGenerator.Instance.InstantiatedRooms)
            {
                Vector2Int pos = kvp.Key;
                RoomController room = kvp.Value;
                if (room == null) continue;

                var overlay = room.GetComponent<RoomFogOverlay>();
                if (overlay == null) continue;

                if (pos == _currentRoomPos)
                {
                    overlay.SetState(FogVisibilityState.Visible, maxDark);
                }
                else if (_visitedRooms.Contains(pos))
                {
                    overlay.SetState(FogVisibilityState.Explored, exploredDark);
                }
                else
                {
                    overlay.SetState(FogVisibilityState.Unexplored, maxDark);
                }
            }
        }

        public void ApplyConfigToShader()
        {
            if (_config == null) return;

            Shader.SetGlobalColor(FogColorId, _config.fogColor);
            Shader.SetGlobalFloat(FovAngleId, _config.fovAngleDegrees);
            Shader.SetGlobalFloat(FovSoftnessId, _config.fovEdgeSoftness);
            Shader.SetGlobalFloat(AlwaysClearRadiusId, _config.alwaysClearRadius);
            Shader.SetGlobalFloat(RearDarkDistanceId, _config.rearDarkDistance);
            Shader.SetGlobalFloat(ClearDistanceId, _config.clearDistance);
            Shader.SetGlobalFloat(FalloffDistanceId, _config.falloffDistance);
            Shader.SetGlobalFloat(DoorFadeAngleId, _config.doorFadeAngle);
        }

        private void UpdateShaderGlobalParameters(Vector3 playerPos)
        {
            Shader.SetGlobalVector(CharacterWorldPosId, new Vector4(playerPos.x, playerPos.y, playerPos.z, 1f));

            // 플레이어가 조준/바라보는 방향 (마우스 방향)
            Vector3 playerForward = _playerController != null 
                ? _playerController.AimDirection 
                : _playerTransform.forward;
            playerForward.y = 0f;
            if (playerForward.sqrMagnitude < 0.001f) playerForward = Vector3.forward;
            else playerForward.Normalize();

            Shader.SetGlobalVector(CharacterForwardId, new Vector4(playerForward.x, playerForward.y, playerForward.z, 0f));

            float roomSize = MazeGenerator.Instance != null ? MazeGenerator.Instance.RoomSize : 39f;
            float halfSize = roomSize * 0.5f;
            Vector3 roomCenter = MazeGenerator.Instance != null 
                ? MazeGenerator.Instance.GetRoomCenterWorldPos(_currentRoomPos) 
                : playerPos;

            Shader.SetGlobalVector(CurrentRoomCenterId, new Vector4(roomCenter.x, roomCenter.y, roomCenter.z, 0f));
            Shader.SetGlobalFloat(CurrentRoomHalfSizeId, halfSize);

            // 현재 방의 개방된 통로/문 데이터 갱신
            int activeDoorCount = 0;
            for (int i = 0; i < 4; i++) _cachedDoorData[i] = Vector4.zero;

            if (MazeGenerator.Instance != null && 
                MazeGenerator.Instance.InstantiatedRooms.TryGetValue(_currentRoomPos, out var currentRoom) && 
                currentRoom != null)
            {
                Direction[] dirs = { Direction.North, Direction.East, Direction.South, Direction.West };
                float halfWidth = _config != null ? _config.doorHalfWidth : 6.0f;

                foreach (var dir in dirs)
                {
                    var socket = currentRoom.GetSocket(dir);
                    if (socket == null) continue;

                    // 벽이거나 잠긴 배리어인 경우 시야 차단
                    if (socket.CurrentState != DoorState.Open) continue;

                    // 자동문이 설치되어 있고 닫혀있는 경우 시야 차단 (양방향 소켓 공유 AutoDoor 참조)
                    var autoDoor = socket.AutoDoor;
                    if (autoDoor != null && autoDoor.IsClosed) continue;

                    Vector3 socketPos = socket.transform.position;

                    if (activeDoorCount < 4)
                    {
                        _cachedDoorData[activeDoorCount] = new Vector4(socketPos.x, socketPos.y, socketPos.z, halfWidth);
                        activeDoorCount++;
                    }
                }
            }

            Shader.SetGlobalFloat(ActiveDoorCountId, activeDoorCount);
            Shader.SetGlobalVector(DoorData0Id, _cachedDoorData[0]);
            Shader.SetGlobalVector(DoorData1Id, _cachedDoorData[1]);
            Shader.SetGlobalVector(DoorData2Id, _cachedDoorData[2]);
            Shader.SetGlobalVector(DoorData3Id, _cachedDoorData[3]);
        }
    }
}
