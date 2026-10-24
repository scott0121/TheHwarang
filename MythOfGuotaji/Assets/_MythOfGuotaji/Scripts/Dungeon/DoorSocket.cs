using UnityEngine;

namespace MythOfGuotaji.Dungeon
{
    /// <summary>
    /// 방의 각 방향(N, E, S, W)에 위치하는 문/벽 소켓 제어기
    /// - 좌/우 고정 벽체(코너 보호)
    /// - 중앙 통로 벽체(DoorState.Wall 시 폐쇄)
    /// - 전투/보스 봉인 배리어(CombatLock / BossLock 시 차단)
    /// - 열쇠로 연 문은 영원히 열려있도록(IsPermanentlyUnlocked) 지원
    /// </summary>
    public class DoorSocket : MonoBehaviour
    {
        [Header("Socket Info")]
        [SerializeField] private Direction _direction;
        [SerializeField] private DoorState _currentState = DoorState.Wall;

        [Header("Permanent Unlock (Key)")]
        [SerializeField] private bool _isPermanentlyUnlocked = false;

        [Header("Visual Components")]
        [SerializeField] private GameObject _solidCenterWall;   // 중앙을 완전히 막는 벽 (Wall 상태일 때)
        [SerializeField] private GameObject _doorBarrierVisual; // 잠금 시 중앙 통로를 막는 봉인 배리어
        [SerializeField] private GameObject _doorPillarsVisual; // 통로 좌우 문 기둥 (옵션)

        public Direction Direction => _direction;
        public DoorState CurrentState => _currentState;
        public bool IsPermanentlyUnlocked => _isPermanentlyUnlocked;

        public void SetDirection(Direction dir) => _direction = dir;

        public void InitializeComponents(Direction dir, GameObject centerWall, GameObject barrier, GameObject pillars = null)
        {
            _direction = dir;
            _solidCenterWall = centerWall;
            _doorBarrierVisual = barrier;
            _doorPillarsVisual = pillars;
        }

        /// <summary>
        /// 소켓과 연동된 자동문을 완전히 영구 개방
        /// </summary>
        public void OpenPermanently()
        {
            _isPermanentlyUnlocked = true;
            SetState(DoorState.Open);

            var autoDoor = GetComponentInChildren<DungeonAutoDoor>();
            if (autoDoor != null)
            {
                autoDoor.SetPermanentlyOpen(true);
            }
        }

        /// <summary>
        /// 열쇠로 문을 열었을 때 호출: 문을 영원히 열린 상태(Open)로 고정 및 현재 방 모든 문 일괄 개방
        /// </summary>
        public void UnlockWithKey()
        {
            OpenPermanently();

            var currentRoom = GetComponentInParent<RoomController>();
            if (currentRoom != null)
            {
                currentRoom.OpenAllDoorsInRoom();
                if (MazeGenerator.Instance != null)
                {
                    Vector2Int neighborPos = currentRoom.GridPosition + MazeGrid.GetOffset(_direction);
                    var neighborRoom = MazeGenerator.Instance.GetRoomAt(neighborPos);
                    if (neighborRoom != null)
                    {
                        Direction oppDir = MazeGrid.GetOpposite(_direction);
                        var oppSocket = neighborRoom.GetSocket(oppDir);
                        if (oppSocket != null && oppSocket.CurrentState != DoorState.Wall)
                        {
                            oppSocket.OpenPermanently();
                        }
                        neighborRoom.OnDoorOpened();
                    }
                }
            }

            NotifyConnectedRoomsDoorOpened();
        }

        /// <summary>
        /// 자동문이 플레이어 접근으로 열렸을 때 호출: 영구 개방 및 현재 방의 모든 문 일괄 자동 개방
        /// </summary>
        public void OnAutoDoorOpened()
        {
            OpenPermanently();

            var currentRoom = GetComponentInParent<RoomController>();
            if (currentRoom != null)
            {
                currentRoom.OpenAllDoorsInRoom();

                if (MazeGenerator.Instance != null)
                {
                    Vector2Int neighborPos = currentRoom.GridPosition + MazeGrid.GetOffset(_direction);
                    var neighborRoom = MazeGenerator.Instance.GetRoomAt(neighborPos);
                    if (neighborRoom != null)
                    {
                        Direction oppDir = MazeGrid.GetOpposite(_direction);
                        var oppSocket = neighborRoom.GetSocket(oppDir);
                        if (oppSocket != null && oppSocket.CurrentState != DoorState.Wall)
                        {
                            oppSocket.OpenPermanently();
                        }
                        neighborRoom.OnDoorOpened();
                    }
                }
            }

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

        /// <summary>
        /// 보스방 진입 시 결전 시작 호출 (한번 열린 문은 열쇠가 있건 없건 이제 영원히 닫히지 않는다)
        /// </summary>
        public void LockBossEncounter()
        {
            if (_isPermanentlyUnlocked) return;
            SetState(DoorState.CombatLock);

            var autoDoor = GetComponentInChildren<DungeonAutoDoor>();
            if (autoDoor != null)
            {
                autoDoor.ForceCloseAndLock();
            }
        }

        public void SetState(DoorState state)
        {
            // 한번 열린 문은 열쇠가 있건 없건 이제 영원히 닫히지 않고 항상 열린 상태 유지
            if (_isPermanentlyUnlocked && (state == DoorState.CombatLock || state == DoorState.BossLock || state == DoorState.Wall))
            {
                return;
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
                    // 개방 통로: 중앙 벽 제거, 배리어 제거 (자유 통과)
                    if (_solidCenterWall != null) _solidCenterWall.SetActive(false);
                    if (_doorBarrierVisual != null) _doorBarrierVisual.SetActive(false);
                    if (_doorPillarsVisual != null) _doorPillarsVisual.SetActive(true);
                    break;

                case DoorState.CombatLock:
                    // 전투 봉인: 중앙 벽 비활성화, 붉은 배리어 활성화
                    if (_solidCenterWall != null) _solidCenterWall.SetActive(false);
                    if (_doorBarrierVisual != null)
                    {
                        _doorBarrierVisual.SetActive(true);
                        SetBarrierColor(new Color(0.9f, 0.2f, 0.2f, 0.85f));
                    }
                    if (_doorPillarsVisual != null) _doorPillarsVisual.SetActive(true);
                    break;

                case DoorState.BossLock:
                    // 보스방 황금 봉인: 중앙 벽 비활성화, 황금빛 배리어 활성화
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
