using UnityEngine;

namespace MythOfGuotaji.Dungeon
{
    /// <summary>
    /// 방의 각 방향(N, E, S, W)에 위치하는 문/벽 소켓 제어기
    /// - 좌/우 고정 벽체(코너 보호)
    /// - 중앙 통로 벽체(DoorState.Wall 시 폐쇄)
    /// - 전투/중간보스 출구 봉인 배리어(CombatLock / BossLock 시 차단)
    /// - 맞은편 방 소켓(OppositeSocket) 및 공유 자동문(AutoDoor) 양방향 동기화 지원
    /// </summary>
    public class DoorSocket : MonoBehaviour
    {
        [Header("Socket Info")]
        [SerializeField] private Direction _direction;
        [SerializeField] private DoorState _currentState = DoorState.Wall;
        [SerializeField] private DungeonDoorType _doorType = DungeonDoorType.AutoDoor;

        [Header("Permanent Unlock (Key)")]
        [SerializeField] private bool _isPermanentlyUnlocked = false;

        [Header("Visual Components")]
        [SerializeField] private GameObject _solidCenterWall;   // 중앙을 완전히 막는 벽 (Wall 상태일 때)
        [SerializeField] private GameObject _doorBarrierVisual; // 잠금 시 중앙 통로를 막는 봉인 배리어
        [SerializeField] private GameObject _doorPillarsVisual; // 통로 좌우 문 기둥 (옵션)

        private DoorSocket _oppositeSocket;
        private DungeonAutoDoor _sharedAutoDoor;

        public Direction Direction => _direction;
        public DoorState CurrentState => _currentState;
        public DungeonDoorType DoorType => _doorType;
        public bool IsPermanentlyUnlocked => _isPermanentlyUnlocked;
        public DoorSocket OppositeSocket => _oppositeSocket;

        public DungeonAutoDoor AutoDoor
        {
            get
            {
                if (_sharedAutoDoor == null)
                {
                    _sharedAutoDoor = GetComponentInChildren<DungeonAutoDoor>();
                    if (_sharedAutoDoor == null && _oppositeSocket != null)
                    {
                        _sharedAutoDoor = _oppositeSocket.GetComponentInChildren<DungeonAutoDoor>();
                    }
                }
                return _sharedAutoDoor;
            }
        }

        public void SetDirection(Direction dir) => _direction = dir;

        public void InitializeComponents(Direction dir, GameObject centerWall, GameObject barrier, GameObject pillars = null)
        {
            _direction = dir;
            _solidCenterWall = centerWall;
            _doorBarrierVisual = barrier;
            _doorPillarsVisual = pillars;
        }

        /// <summary>
        /// 인접한 두 방의 마주보는 소켓과 공유 자동문(DungeonAutoDoor)을 양방향 연결
        /// </summary>
        public void LinkOppositeSocket(DoorSocket opposite, DungeonAutoDoor sharedDoor, DungeonDoorType doorType)
        {
            _oppositeSocket = opposite;
            _sharedAutoDoor = sharedDoor;
            _doorType = doorType;

            // [규칙 3 & 4]: 중간보스방 출구 잠긴 문은 양쪽 소켓 모두 BossLock(황금 봉인 배리어) 상태 강제
            if (doorType == DungeonDoorType.LockedDoor)
            {
                SetStateSingleSide(DoorState.BossLock);
            }
        }

        /// <summary>
        /// 소켓과 연동된 자동문을 영구 개방
        /// </summary>
        public void OpenPermanently()
        {
            _isPermanentlyUnlocked = true;
            SetState(DoorState.Open);

            var autoDoor = AutoDoor;
            if (autoDoor != null)
            {
                autoDoor.SetPermanentlyOpen(true);
            }
        }

        /// <summary>
        /// [규칙 3 & 8]: 중간보스 처치 열쇠로 중간보스방 출구(잠긴 문)의 봉인을 해제할 때 호출
        /// - 황금 봉인(BossLock)을 해제하여 일반 개방 통로(Open)로 전환하고 문을 개방
        /// - 문 너머 후반부 던전(2블록 첫 방)에 몬스터가 있다면 즉시 전투 개시 (규칙 6)
        /// - 이후 전투 중에는 퇴로 차단 정책(규칙 9) 및 전투 종료 후 재개방 정책(규칙 10)을 동일하게 적용받음
        /// </summary>
        public void UnlockWithKey()
        {
            _isPermanentlyUnlocked = true;
            if (_oppositeSocket != null)
            {
                _oppositeSocket._isPermanentlyUnlocked = true;
            }

            SetState(DoorState.Open);

            var autoDoor = AutoDoor;
            if (autoDoor != null)
            {
                autoDoor.MarkKeyUnlockedAndOpen();
            }

            OnAutoDoorOpened();
        }

        /// <summary>
        /// 이 소켓이 속한 방과 맞은편 방 사이의 고유 문 PairKey 반환
        /// </summary>
        public string GetPairKey()
        {
            var room = GetComponentInParent<RoomController>();
            if (room != null)
            {
                Vector2Int posA = room.GridPosition;
                Vector2Int posB = posA + MazeGrid.GetOffset(_direction);
                return MazeGenerator.GetDoorPairKey(posA, posB);
            }
            return string.Empty;
        }

        /// <summary>
        /// 자동문이 열렸을 때 호출: 통로 개방, 맞은편 소켓 연동 및 [규칙 6] 몬스터가 있는 방과 자동 전투 시작
        /// </summary>
        public void OnAutoDoorOpened()
        {
            string pairKey = GetPairKey();
            if (!string.IsNullOrEmpty(pairKey))
            {
                MazeGenerator.Instance?.RegisterCombatTriggerDoor(pairKey);
            }

            SetState(DoorState.Open);

            var currentRoom = GetComponentInParent<RoomController>();
            RoomController neighborRoom = null;

            if (currentRoom != null && MazeGenerator.Instance != null)
            {
                Vector2Int neighborPos = currentRoom.GridPosition + MazeGrid.GetOffset(_direction);
                neighborRoom = MazeGenerator.Instance.GetRoomAt(neighborPos);
                if (neighborRoom != null)
                {
                    Direction oppDir = MazeGrid.GetOpposite(_direction);
                    var oppSocket = neighborRoom.GetSocket(oppDir);
                    if (oppSocket != null && oppSocket.CurrentState != DoorState.Wall)
                    {
                        oppSocket.SetStateSingleSide(DoorState.Open);
                    }
                }
            }

            if (AutoDoor != null && !AutoDoor.IsOpen)
            {
                AutoDoor.OpenDoorByCenterTrigger();
            }

            // [규칙 6]: 방 문이 열렸을 때 그 방에 몬스터들이 있다면, 몬스터들과는 자동으로 전투가 시작되는 형태
            if (currentRoom != null)
            {
                currentRoom.OnDoorOpenedToCombat();
            }
            if (neighborRoom != null)
            {
                neighborRoom.OnDoorOpenedToCombat();
            }

            // [규칙 9 & 10]: 전투 상태 및 방문 개폐 상태 즉시 갱신
            MazeGenerator.Instance?.UpdateDungeonDoorPolicy();
            NotifyConnectedRoomsDoorOpened();
        }

        public void NotifyConnectedRoomsDoorOpened()
        {
            var currentRoom = GetComponentInParent<RoomController>();
            if (currentRoom != null)
            {
                currentRoom.OnDoorOpened();

                if (MazeGenerator.Instance != null)
                {
                    Vector2Int neighborPos = currentRoom.GridPosition + MazeGrid.GetOffset(_direction);
                    var neighborRoom = MazeGenerator.Instance.GetRoomAt(neighborPos);
                    if (neighborRoom != null)
                    {
                        neighborRoom.OnDoorOpened();
                    }
                }
            }
        }

        public void LockBossEncounter()
        {
            if (_isPermanentlyUnlocked) return;
            SetState(DoorState.CombatLock);

            var autoDoor = AutoDoor;
            if (autoDoor != null)
            {
                autoDoor.ForceCloseAndLock();
            }
        }

        public void SetState(DoorState state)
        {
            SetStateSingleSide(state);
            if (_oppositeSocket != null && _oppositeSocket.CurrentState != state)
            {
                _oppositeSocket.SetStateSingleSide(state);
            }
        }

        public void SetStateSingleSide(DoorState state)
        {
            // [규칙 3, 4, 8 무결성 보장]: 아직 열쇠로 해제되지 않은 중간보스 출구 잠긴 문은 열쇠 해제 전까지 절대 Open으로 풀리지 않음!
            if (!_isPermanentlyUnlocked && (_doorType == DungeonDoorType.LockedDoor || _currentState == DoorState.BossLock))
            {
                if (state != DoorState.Wall)
                {
                    state = DoorState.BossLock;
                }
            }

            // 이미 열쇠로 봉인이 해제된 문은 다시 BossLock으로 돌아가지 않음
            if (_isPermanentlyUnlocked && state == DoorState.BossLock)
            {
                state = DoorState.Open;
            }

            _currentState = state;

            switch (state)
            {
                case DoorState.Wall:
                    // 완전 폐쇄 벽: 중앙 벽 활성화, 배리어 비활성화
                    if (_solidCenterWall != null) _solidCenterWall.SetActive(true);
                    if (_doorBarrierVisual != null) _doorBarrierVisual.SetActive(false);
                    if (_doorPillarsVisual != null) _doorPillarsVisual.SetActive(false);
                    break;

                case DoorState.Open:
                    // 개방 통로: 중앙 벽 제거, 배리어 제거 (자동문 슬라이딩 개폐)
                    if (_solidCenterWall != null) _solidCenterWall.SetActive(false);
                    if (_doorBarrierVisual != null) _doorBarrierVisual.SetActive(false);
                    if (_doorPillarsVisual != null) _doorPillarsVisual.SetActive(true);
                    break;

                case DoorState.CombatLock:
                    // 전투 봉인: 중앙 벽 비활성화, 배리어 비활성화 (물리적 문짝 슬라이딩 닫힘으로 통로 차단)
                    if (_solidCenterWall != null) _solidCenterWall.SetActive(false);
                    if (_doorBarrierVisual != null) _doorBarrierVisual.SetActive(false);
                    if (_doorPillarsVisual != null) _doorPillarsVisual.SetActive(true);
                    break;

                case DoorState.BossLock:
                    // 중간보스방 출구 황금 봉인: 중앙 벽 비활성화, 황금빛 배리어 활성화
                    if (_solidCenterWall != null) _solidCenterWall.SetActive(false);
                    if (_doorBarrierVisual != null)
                    {
                        _doorBarrierVisual.SetActive(true);
                        SetBarrierColor(new Color(0.95f, 0.8f, 0.15f, 0.85f));
                    }
                    if (_doorPillarsVisual != null) _doorPillarsVisual.SetActive(true);
                    break;
            }
        }

        public void OnCombatStarted()
        {
            var autoDoor = AutoDoor;
            if (autoDoor != null)
            {
                autoDoor.OnCombatStarted();
            }
        }

        public void OnCombatEnded()
        {
            var autoDoor = AutoDoor;
            if (autoDoor != null)
            {
                autoDoor.OnCombatEnded();
            }

            if (_currentState == DoorState.CombatLock)
            {
                SetState(DoorState.Open);
            }
        }

        private void SetBarrierColor(Color color)
        {
            if (_doorBarrierVisual != null && _doorBarrierVisual.TryGetComponent<Renderer>(out var rend))
            {
                rend.material.color = color;
                if (rend.material.HasProperty("_BaseColor"))
                {
                    rend.material.SetColor("_BaseColor", color);
                }
            }
        }
    }
}
