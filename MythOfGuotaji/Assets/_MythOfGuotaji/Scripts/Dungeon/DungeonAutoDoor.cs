using MythOfGuotaji.Actor;
using MythOfGuotaji.Core;
using UnityEngine;

namespace MythOfGuotaji.Dungeon
{
    /// <summary>
    /// 던전 자동문 및 잠긴 관문 제어기 (10대 던전 문 정책 준수)
    /// - 문의 종류: 자동으로 열리는 문(AutoDoor) / 잠긴 문(LockedDoor: 중간보스 출구 관문)
    /// - 비전투 시 접근(4.8m 이내)하면 자동으로 개방되며, 문 너머에 몬스터가 있으면 즉시 전투 시작
    /// - 중간보스 출구(LockedDoor)는 중간보스 처치 + 열쇠 획득 + 해당 전투 종료 후 문 앞에 가야만 개방
    /// - 전투 중에는 나가는 퇴로가 자동으로 차단되며, 캐릭터와 몬스터 사이의 문은 단절 방지를 위해 개방 유지
    /// - 전투 종료 시 한 번 열었던 모든 문은 다시 자동 개방
    /// </summary>
    public class DungeonAutoDoor : MonoBehaviour
    {
        [Header("Door Leaves")]
        [SerializeField] private Transform _leftLeaf;
        [SerializeField] private Transform _rightLeaf;

        [Header("Motion & Settings")]
        [SerializeField] private float _detectDistance = 4.8f;
        [SerializeField] private float _closeDistance = 6.0f;
        [SerializeField] private float _slideSpeed = 10.0f;
        [SerializeField] private float _openHoldDuration = 60.0f;

        [Header("Door Classification & Policy")]
        [SerializeField] private DungeonDoorType _doorType = DungeonDoorType.AutoDoor;
        [SerializeField] private bool _isManualApproachOnly = false;
        [SerializeField] private bool _isKeyUnlocked = false;
        [SerializeField] private bool _isPermanentlyOpen = false;

        [Header("Door Policy & Combat State")]
        [SerializeField] private bool _hasEverBeenOpened = false;
        [SerializeField] private float _lastOpenedTime = -999f;
        [SerializeField] private bool _wasClosedByCombat = false;
        private DoorSocket _cachedSocket;
        private RoomController _cachedParentRoom;
        private PlayerController _cachedPlayerCtrl;
        private float _doorCheckTimer = 0f;
        private bool _openedDuringCombat = false;

        public bool OpenedDuringCombat
        {
            get => _openedDuringCombat;
            set => _openedDuringCombat = value;
        }

        private Vector3 _closedPosLeft;
        private Vector3 _closedPosRight;
        private Vector3 _openPosLeft;
        private Vector3 _openPosRight;

        private bool _isOpen = false;
        private bool _isUnlocking = false;
        private float _lastNoKeyWarningTime = -999f;
        private Transform _playerTransform;

        public bool IsClosed => !_isOpen && (_leftLeaf == null || (_leftLeaf.localPosition - _closedPosLeft).sqrMagnitude < 0.05f);
        public bool IsOpen => _isOpen;
        public DungeonDoorType DoorType => _doorType;
        public bool IsManualApproachOnly => _isManualApproachOnly;
        public bool IsKeyUnlocked => _isKeyUnlocked;
        public bool IsPermanentlyOpen => _isPermanentlyOpen;
        public float OpenHoldDuration => _openHoldDuration;
        public bool HasEverBeenOpened => _hasEverBeenOpened;
        public float LastOpenedTime => _lastOpenedTime;
        public bool WasClosedByCombat => _wasClosedByCombat;
        public float DetectDistance => _detectDistance;
        public bool IsPlayerNearby => _playerTransform != null && Vector3.Distance(transform.position, _playerTransform.position) <= _detectDistance;

        private void Awake()
        {
            SetupDoorPanelsIfMissing();
        }

        private void Start()
        {
            _cachedSocket = GetComponentInParent<DoorSocket>();
            _cachedParentRoom = GetComponentInParent<RoomController>();
            FindPlayer();
        }

        /// <summary>
        /// [규칙 2, 3, 5, 7, 8]: 던전 생성 시 각 문의 종류(AutoDoor vs LockedDoor) 및 수동 접근 전용 여부 설정
        /// </summary>
        public void ConfigureDoorPolicy(DungeonDoorType doorType, bool manualApproachOnly)
        {
            _doorType = doorType;
            _isManualApproachOnly = manualApproachOnly;
            _isKeyUnlocked = (doorType == DungeonDoorType.AutoDoor);
        }

        public void SetupDoorPanelsIfMissing()
        {
            float doorWidth = 6.05f;
            float doorHeight = 4.2f;
            float doorThickness = 0.35f;

            _closedPosLeft = new Vector3(-doorWidth * 0.5f, doorHeight * 0.5f, 0f);
            _closedPosRight = new Vector3(doorWidth * 0.5f, doorHeight * 0.5f, 0f);

            _openPosLeft = new Vector3(-doorWidth * 1.55f, doorHeight * 0.5f, 0f);
            _openPosRight = new Vector3(doorWidth * 1.55f, doorHeight * 0.5f, 0f);

            if (_leftLeaf == null) _leftLeaf = transform.Find("AutoDoor_LeftLeaf");
            if (_rightLeaf == null) _rightLeaf = transform.Find("AutoDoor_RightLeaf");

            if (_leftLeaf != null && _rightLeaf != null)
            {
                _leftLeaf.localPosition = _closedPosLeft;
                _rightLeaf.localPosition = _closedPosRight;
                return;
            }

            // 좌측 문짝 생성 (프리팹 미존재 시 폴백)
            var leftObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leftObj.name = "AutoDoor_LeftLeaf";
            leftObj.transform.SetParent(transform, false);
            leftObj.transform.localPosition = _closedPosLeft;
            leftObj.transform.localScale = new Vector3(doorWidth, doorHeight, doorThickness);
            SetDoorMaterial(leftObj, new Color(0.24f, 0.16f, 0.11f));
            _leftLeaf = leftObj.transform;

            // 우측 문짝 생성
            var rightObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rightObj.name = "AutoDoor_RightLeaf";
            rightObj.transform.SetParent(transform, false);
            rightObj.transform.localPosition = _closedPosRight;
            rightObj.transform.localScale = new Vector3(doorWidth, doorHeight, doorThickness);
            SetDoorMaterial(rightObj, new Color(0.24f, 0.16f, 0.11f));
            _rightLeaf = rightObj.transform;

/*            // 문짝 중앙 철제 징/장식
            AddDoorStuds(leftObj.transform, doorWidth, doorHeight);
            AddDoorStuds(rightObj.transform, doorWidth, doorHeight);*/
        }

/*        private void AddDoorStuds(Transform parent, float w, float h)
        {
            var stud = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stud.name = "Studs";
            stud.transform.SetParent(parent, false);
            stud.transform.localPosition = new Vector3(0f, 0f, -0.2f);
            stud.transform.localScale = new Vector3(0.3f, 0.8f, 0.15f);
            SetDoorMaterial(stud, new Color(0.7f, 0.55f, 0.2f));
            var col = stud.GetComponent<Collider>();
            if (col != null) Destroy(col);
        }*/

        private void SetDoorMaterial(GameObject go, Color color)
        {
            var rend = go.GetComponent<Renderer>();
            if (rend != null)
            {
                var shader = Actor.ExpGem.ResolveSafeShader("Universal Render Pipeline/Lit", "Universal Render Pipeline/Unlit", "Sprites/Default", "Standard"); var mat = shader != null ? new Material(shader) : (rend.sharedMaterial != null ? new Material(rend.sharedMaterial) : null);
                mat.color = color;
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
                rend.material = mat;
            }
        }

        private void FindPlayer()
        {
            var player = FindAnyObjectByType<PlayerController>();
            if (player != null)
            {
                _playerTransform = player.transform;
            }
        }

        public void SetPermanentlyOpen(bool permanent = true)
        {
            _isPermanentlyOpen = permanent;
            if (_isPermanentlyOpen)
            {
                _isKeyUnlocked = true;
                _hasEverBeenOpened = true;
                _isOpen = true;
            }
        }

        /// <summary>
        /// [규칙 8]: 중간보스 처치 열쇠로 잠긴 문을 해제할 때 호출
        /// </summary>
        public void MarkKeyUnlockedAndOpen()
        {
            _isKeyUnlocked = true;
            _hasEverBeenOpened = true;
            _wasClosedByCombat = false;
            _lastOpenedTime = Time.time;
            _isOpen = true;
        }

        public void ForceCloseAndLock()
        {
            if (_isPermanentlyOpen) return;
            _isOpen = false;
        }

        /// <summary>
        /// [규칙 7]: 새로운 방 진입 후 잠시 뒤 연결된 몬스터 방의 문이 자동으로 열릴 때 호출
        /// </summary>
        public void OpenDoorByCenterTrigger()
        {
            _isOpen = true;
            _hasEverBeenOpened = true;
            _wasClosedByCombat = false;
            _openedDuringCombat = true;
            _lastOpenedTime = Time.time;

            var socket = _cachedSocket;
            if (socket != null && socket.CurrentState != DoorState.BossLock)
            {
                socket.SetState(DoorState.Open);
                string pairKey = socket.GetPairKey();
                if (!string.IsNullOrEmpty(pairKey))
                {
                    MazeGenerator.Instance?.RegisterCombatTriggerDoor(pairKey);
                }
            }
        }

        /// <summary>
        /// [규칙 9]: 전투 중 캐릭터와 몬스터 사이의 문일 때 단절 방지를 위해 강제 개방 유지
        /// </summary>
        public void ForceOpen()
        {
            _isOpen = true;
            _hasEverBeenOpened = true;
            _wasClosedByCombat = false;

            var socket = _cachedSocket;
            if (socket != null && socket.CurrentState == DoorState.CombatLock)
            {
                socket.SetState(DoorState.Open);
            }
        }

        /// <summary>
        /// [문 닫히는 규칙]:
        /// - 전투가 시작되면, 이 전투가 시작되기 3초 이내에 열린 문 이외에 모든 이전에 열었던 문은 자동으로 닫힌다.
        /// - 현재 진행 중인 전투 직전에 연 문(CombatTriggerDoor) 및 전투 중 관통되어 열린 문(OpenedDuringCombat)은 닫히지 않는다.
        /// - 플레이어가 문 근처에 있더라도 후퇴 방지를 위해 반드시 닫힌다.
        /// </summary>
        public void CloseByCombat()
        {
            if (_isPermanentlyOpen) return;

            // [사용자 핵심 규칙 1]: 현재 진행 중인 전투 직전에 연 문은 해당 전투 종료 시까지 절대 닫히지 않음
            var socket = _cachedSocket;
            var parentRoom = _cachedParentRoom;
            if (socket != null && parentRoom != null && MazeGenerator.Instance != null)
            {
                Vector2Int pA = parentRoom.GridPosition;
                Vector2Int pB = pA + MazeGrid.GetOffset(socket.Direction);
                if (MazeGenerator.Instance.IsCombatTriggerDoor(MazeGenerator.GetDoorPairKey(pA, pB)))
                {
                    return;
                }
            }

            // [사용자 핵심 규칙 2]: 전투 중에 열린 문 (방을 여러 개 관통하는 경우 포함)은 전투 끝날 때까지 닫히지 않음
            if (_openedDuringCombat) return;

            // [사용자 핵심 규칙 3]: 전투 시작 3초 이내에 열린 문 이외의 모든 이전에 열었던 문은 닫힘
            if (Time.time - _lastOpenedTime < 3.0f) return;

            // 문턱 한가운데(1.2m 이내)에 플레이어나 몬스터가 물리적으로 끼이는 것 방지
            if (IsActorInDoorway(1.2f)) return;

            if (_isOpen)
            {
                _isOpen = false;
                _wasClosedByCombat = true;

                if (socket != null && socket.CurrentState == DoorState.Open)
                {
                    socket.SetState(DoorState.CombatLock);
                }

                Debug.Log($"<color=orange>[DungeonAutoDoor] 전투 중 후퇴 차단을 위해 문이 닫혔습니다. ({gameObject.name})</color>");
            }
        }

        /// <summary>
        /// [전투 종료 시 규칙]: 전투가 종료되면 한번 열었던 문 중 전투 시작 시 닫힌 문은 모두 다시 열린다.
        /// </summary>
        public void ReopenAfterCombat()
        {
            _openedDuringCombat = false;
            if (_hasEverBeenOpened && _wasClosedByCombat)
            {
                _isOpen = true;
                _wasClosedByCombat = false;

                var socket = _cachedSocket;
                if (socket != null && socket.CurrentState == DoorState.CombatLock)
                {
                    socket.SetState(DoorState.Open);
                }
                Debug.Log($"<color=lime>[DungeonAutoDoor] 전투 종료! 전투 시작 시 닫혔던 문이 다시 열렸습니다. ({gameObject.name})</color>");
            }
        }

        /// <summary>
        /// [규칙 9]: 문턱(Doorway) 한가운데에 플레이어나 몬스터가 걸쳐 있을 때 문이 닫혀 끼이거나 단절되는 현상 방지
        /// </summary>
        public bool IsActorInDoorway(float clearanceRadius = 2.4f)
        {
            Vector3 doorPos = transform.position;
            doorPos.y = 0f;
            float sqrClearance = clearanceRadius * clearanceRadius;

            if (_playerTransform != null)
            {
                Vector3 pPos = _playerTransform.position;
                pPos.y = 0f;
                if ((pPos - doorPos).sqrMagnitude <= sqrClearance)
                {
                    return true;
                }
            }

            var activeMonsters = MonsterAI.ActiveMonsters;
            for (int i = 0; i < activeMonsters.Count; i++)
            {
                var m = activeMonsters[i];
                if (m != null && !m.IsDead)
                {
                    Vector3 mPos = m.transform.position;
                    mPos.y = 0f;
                    if ((mPos - doorPos).sqrMagnitude <= sqrClearance)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public void OnCombatStarted()
        {
            // 중앙 정책 엔진(MazeGenerator.UpdateDungeonDoorPolicy)에서 통괄 제어
        }

        public void OnCombatEnded()
        {
            ReopenAfterCombat();
        }

        private void Update()
        {
            if (_playerTransform == null)
            {
                FindPlayer();
                if (_playerTransform == null) return;
            }


            // 거리 및 잠금 상태 검사는 0.08초 간격으로 쓰로틀링 (매 프레임 무거운 GetComponent 및 방 순회 제거)
            _doorCheckTimer -= Time.deltaTime;
            if (_doorCheckTimer > 0f) return;
            _doorCheckTimer = 0.08f;

            if (_cachedPlayerCtrl == null) _cachedPlayerCtrl = _playerTransform.GetComponent<PlayerController>();
            if (_cachedSocket == null) _cachedSocket = GetComponentInParent<DoorSocket>();
            if (_cachedParentRoom == null) _cachedParentRoom = GetComponentInParent<RoomController>();

            float dist = Vector3.Distance(transform.position, _playerTransform.position);
            var playerCtrl = _cachedPlayerCtrl;
            bool isPlayerInCombat = (playerCtrl != null && playerCtrl.IsInCombat) ||
                                    (MazeGenerator.Instance != null && MazeGenerator.Instance.HasAnyActiveCombat());

            if (!_isUnlocking)
            {
                var socket = _cachedSocket;
                bool isLockedMidBossExit = (_doorType == DungeonDoorType.LockedDoor && !_isKeyUnlocked) ||
                                           (socket != null && socket.CurrentState == DoorState.BossLock && !_isKeyUnlocked);

                if (isLockedMidBossExit)
                {
                    // [규칙 3 & 8]: 중간보스 -> 후반부 던전으로 연결되는 잠긴 문은
                    // 중간보스를 처치하고 열쇠를 획득한 후, 해당 전투가 끝난 뒤에 문 앞에 가야만 열림!
                    if (dist <= _detectDistance)
                    {
                        bool hasKey = GameManager.Instance != null && GameManager.Instance.HasBossKey;
                        // 중간보스방 자체의 전투가 끝났는지 확인 (다른 방의 몬스터로 인한 소프트락 원천 차단)
                        var parentRoom = _cachedParentRoom;
                        bool isKeyRoomCombatActive = parentRoom != null && parentRoom.RoomType == RoomType.KeyRoom &&
                                                     parentRoom.IsEncounterActive && parentRoom.LivingMonsterCount > 0;

                        if (hasKey && !isKeyRoomCombatActive)
                        {
                            StartCoroutine(BossDoorUnlockSequence(socket));
                        }
                        else
                        {
                            if (Time.time - _lastNoKeyWarningTime > 3.0f)
                            {
                                _lastNoKeyWarningTime = Time.time;
                                if (!hasKey)
                                {
                                    Debug.Log("<color=yellow>[DungeonAutoDoor] 중간보스 출구 관문이 봉인되어 있습니다. 중간보스를 처치하고 열쇠를 획득해야 합니다!</color>");
                                }
                                else if (isKeyRoomCombatActive)
                                {
                                    Debug.Log("<color=yellow>[DungeonAutoDoor] 아직 중간보스방의 전투가 끝나지 않았습니다! 방 안의 남은 요괴를 모두 소탕한 후 문 앞으로 다가오세요.</color>");
                                }
                            }
                            _isOpen = false;
                        }
                    }
                    else
                    {
                        _isOpen = false;
                    }
                }
                else
                {
                    // [규칙 5, 6, 7]:
                    // 문 가까이에 다가가면(dist <= _detectDistance) 문이 열림!
                    // - 아직 열린 적이 없는 문(!_hasEverBeenOpened): 스타트룸 첫 문, 스타트룸 옆 방 문, 중간보스/최종보스 입구 등 문 앞 접근 시 무조건 개방!
                    //   문이 열릴 때 그 방에 몬스터들이 있다면 자동으로 전투가 시작됨 (규칙 6: socket.OnAutoDoorOpened)
                    // - 이미 열렸던 문: 플레이어가 문 앞에 직접 다가왔다면 통과 허용
                    bool canOpenByProximity = (dist <= _detectDistance);

                    // [후퇴 금지]: 전투 진행 중에는 닫힌 문(후퇴 경로)에 다가가도 열리지 않음
                    // 단! 교전 중인 몬스터와 플레이어 사이의 문(진행 경로)인 경우는 단절 방지를 위해 접근 시 개방 허용
                    if (isPlayerInCombat && (_wasClosedByCombat || (socket != null && socket.CurrentState == DoorState.CombatLock)))
                    {
                        bool isConnectingToCombat = false;
                        if (DungeonRuntimeManager.Instance != null && socket != null && _cachedParentRoom != null)
                        {
                            isConnectingToCombat = DungeonRuntimeManager.Instance.IsDoorConnectingToActiveCombat(_cachedParentRoom.GridPosition, socket.Direction);
                        }

                        if (!isConnectingToCombat)
                        {
                            canOpenByProximity = false;
                        }
                    }

                    if (canOpenByProximity)
                    {
                        if (!_isOpen)
                        {
                            _isOpen = true;
                            _hasEverBeenOpened = true;
                            _wasClosedByCombat = false;
                            _lastOpenedTime = Time.time;
                            if (isPlayerInCombat)
                            {
                                _openedDuringCombat = true;
                            }
                            if (socket != null)
                            {
                                socket.SetState(DoorState.Open);
                                socket.OnAutoDoorOpened();
                            }
                            MazeGenerator.Instance?.UpdateDungeonDoorPolicy();
                        }
                    }
                }
            }

            Vector3 targetLeft = _isOpen ? _openPosLeft : _closedPosLeft;
            Vector3 targetRight = _isOpen ? _openPosRight : _closedPosRight;

            if (_leftLeaf != null)
            {
                _leftLeaf.localPosition = Vector3.MoveTowards(_leftLeaf.localPosition, targetLeft, Time.deltaTime * _slideSpeed);
            }
            if (_rightLeaf != null)
            {
                _rightLeaf.localPosition = Vector3.MoveTowards(_rightLeaf.localPosition, targetRight, Time.deltaTime * _slideSpeed);
            }
        }

        /// <summary>
        /// [규칙 8]: 중간보스 처치 열쇠 획득 및 전투 종료 후 중간보스방 출구 관문 앞에 다가갔을 때 봉인 해제 연출
        /// </summary>
        private System.Collections.IEnumerator BossDoorUnlockSequence(DoorSocket socket)
        {
            _isUnlocking = true;
            Debug.Log("<color=gold>[DungeonAutoDoor] 열쇠가 공명하며 중간보스 출구 관문 봉인 해제 연출이 시작됩니다!</color>");

            Vector3 doorCenter = transform.position + Vector3.up * 2.0f;

            // 1. 황금 마법진 및 섬광 이펙트 전개
            Core.VFXManager.Instance?.Spawn("HCFX_MagicCircle_01", doorCenter, Quaternion.identity, scale: 2.2f, autoDestroyTime: 2.0f);
            Core.VFXManager.Instance?.Spawn("HCFX_Shine_01", doorCenter, Quaternion.identity, scale: 1.8f, autoDestroyTime: 1.5f);

            // 카메라 미세 진동
            Cameras.QuarterViewCamera.Instance?.TriggerShake(0.35f, 0.6f);

            // 0.6초 동안 봉인 에너지 집중
            yield return new WaitForSeconds(0.6f);

            // 2. 봉인 파괴 폭발 이펙트 & 강한 카메라 셰이크
            Core.VFXManager.Instance?.Spawn("HCFX_Explosion_03", doorCenter, Quaternion.identity, scale: 2.5f, autoDestroyTime: 2.0f);
            Core.VFXManager.Instance?.Spawn("HCFX_Buff_Up", doorCenter, Quaternion.identity, scale: 2.0f, autoDestroyTime: 2.0f);
            Cameras.QuarterViewCamera.Instance?.TriggerShake(0.75f, 0.5f);

            // 0.3초 후 문 완전 개방
            yield return new WaitForSeconds(0.3f);

            _isKeyUnlocked = true;
            _hasEverBeenOpened = true;
            _wasClosedByCombat = false;
            _lastOpenedTime = Time.time;
            _isOpen = true;
            _isUnlocking = false;

            if (socket != null)
            {
                socket.UnlockWithKey();
            }
            else
            {
                MazeGenerator.Instance?.UnlockBossDoors();
            }

            Debug.Log("<color=lime>[DungeonAutoDoor] 중간보스 출구 관문이 웅장하게 열리며 후반부 던전 통로가 개방되었습니다!</color>");
        }
    }
}
