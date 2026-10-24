using MythOfGuotaji.Actor;
using MythOfGuotaji.Core;
using UnityEngine;

namespace MythOfGuotaji.Dungeon
{
    /// <summary>
    /// 플레이어 접근 시 자동으로 좌우로 부드럽게 미끄러지며 열리는 자동 개폐 문
    /// - 평상시: 닫힘 상태 (통로 차단 및 시야 차단)
    /// - 캐릭터 접근(5.5m 이내): 좌/우 도어 패널이 벽 속으로 슬라이딩 개방
    /// - 한번 열린 자동 문은 1분(60초)간 닫히지 않고 열린 상태 유지
    /// - 1분 경과 후 플레이어가 이탈(6.2m 초과)해 있을 때만 부드럽게 닫힘
    /// - 열쇠로 연 문(보스문 등)은 영원히 열려있음 (Permanently Open)
    /// </summary>
    public class DungeonAutoDoor : MonoBehaviour
    {
        [Header("Door Leaves")]
        [SerializeField] private Transform _leftLeaf;
        [SerializeField] private Transform _rightLeaf;

        [Header("Motion & Settings")]
        [SerializeField] private float _detectDistance = 7.0f;
        [SerializeField] private float _closeDistance = 8.0f;
        [SerializeField] private float _slideSpeed = 8.0f;
        [SerializeField] private float _openHoldDuration = 60.0f; // 1분간 열림 상태 유지

        [Header("Permanent State")]
        [SerializeField] private bool _isPermanentlyOpen = false;

        private Vector3 _closedPosLeft;
        private Vector3 _closedPosRight;
        private Vector3 _openPosLeft;
        private Vector3 _openPosRight;

        private bool _isOpen = false;
        private float _openedTimestamp = -999f;
        private Transform _playerTransform;

        public bool IsClosed => !_isOpen && (_leftLeaf == null || (_leftLeaf.localPosition - _closedPosLeft).sqrMagnitude < 0.05f);
        public bool IsOpen => _isOpen;
        public bool IsPermanentlyOpen => _isPermanentlyOpen;
        public float OpenHoldDuration => _openHoldDuration;

        private void Awake()
        {
            SetupDoorPanelsIfMissing();
        }

        private void Start()
        {
            FindPlayer();
        }

        public void SetupDoorPanelsIfMissing()
        {
            if (_leftLeaf != null && _rightLeaf != null) return;

            float doorWidth = 6.05f;
            float doorHeight = 4.2f;
            float doorThickness = 0.35f;

            _closedPosLeft = new Vector3(-doorWidth * 0.5f, doorHeight * 0.5f, 0f);
            _closedPosRight = new Vector3(doorWidth * 0.5f, doorHeight * 0.5f, 0f);

            _openPosLeft = new Vector3(-doorWidth * 1.55f, doorHeight * 0.5f, 0f);
            _openPosRight = new Vector3(doorWidth * 1.55f, doorHeight * 0.5f, 0f);

            // 좌측 문짝 생성
            var leftObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leftObj.name = "AutoDoor_LeftLeaf";
            leftObj.transform.SetParent(transform, false);
            leftObj.transform.localPosition = _closedPosLeft;
            leftObj.transform.localScale = new Vector3(doorWidth, doorHeight, doorThickness);
            SetDoorMaterial(leftObj, new Color(0.24f, 0.16f, 0.11f)); // 고풍스러운 흑단/목재
            _leftLeaf = leftObj.transform;

            // 우측 문짝 생성
            var rightObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rightObj.name = "AutoDoor_RightLeaf";
            rightObj.transform.SetParent(transform, false);
            rightObj.transform.localPosition = _closedPosRight;
            rightObj.transform.localScale = new Vector3(doorWidth, doorHeight, doorThickness);
            SetDoorMaterial(rightObj, new Color(0.24f, 0.16f, 0.11f));
            _rightLeaf = rightObj.transform;

            // 문짝 중앙 철제 징/장식
            AddDoorStuds(leftObj.transform, doorWidth, doorHeight);
            AddDoorStuds(rightObj.transform, doorWidth, doorHeight);
        }

        private void AddDoorStuds(Transform parent, float w, float h)
        {
            var stud = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stud.name = "Studs";
            stud.transform.SetParent(parent, false);
            stud.transform.localPosition = new Vector3(0f, 0f, -0.2f);
            stud.transform.localScale = new Vector3(0.3f, 0.8f, 0.15f);
            SetDoorMaterial(stud, new Color(0.7f, 0.55f, 0.2f)); // 황동 장식
            var col = stud.GetComponent<Collider>();
            if (col != null) Destroy(col);
        }

        private void SetDoorMaterial(GameObject go, Color color)
        {
            var rend = go.GetComponent<Renderer>();
            if (rend != null)
            {
                var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
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

        /// <summary>
        /// 열쇠로 연 문 등을 영구 개방할 때 호출: 절대 다시 닫히지 않음
        /// </summary>
        public void SetPermanentlyOpen(bool permanent = true)
        {
            _isPermanentlyOpen = permanent;
            if (_isPermanentlyOpen)
            {
                _isOpen = true;
            }
        }

        /// <summary>
        /// 보스 결전 진입 시 문 강제 폐쇄 및 잠금 시도 (한번 열린 문은 영원히 닫히지 않음)
        /// </summary>
        public void ForceCloseAndLock()
        {
            // 한번 열린 문은 열쇠가 있건 없건 이제 영원히 닫히지 않는다
            if (_isPermanentlyOpen || _isOpen) return;
            _isOpen = false;
        }

        private void Update()
        {
            if (_playerTransform == null)
            {
                FindPlayer();
                if (_playerTransform == null) return;
            }

            // 플레이어와의 거리 측정
            float dist = Vector3.Distance(transform.position, _playerTransform.position);

            if (_isPermanentlyOpen)
            {
                _isOpen = true;
            }
            else
            {
                var socket = GetComponentInParent<DoorSocket>();
                bool isSocketLocked = socket != null && (socket.CurrentState == DoorState.CombatLock || socket.CurrentState == DoorState.BossLock);

                if (isSocketLocked)
                {
                    // 소켓이 보스 잠금(BossLock) 상태이고 플레이어가 보스 열쇠를 보유한 채 접근하면 영구 개방 트리거
                    if (socket.CurrentState == DoorState.BossLock && GameManager.Instance != null && GameManager.Instance.HasBossKey && dist <= _detectDistance)
                    {
                        MazeGenerator.Instance?.UnlockBossDoors();
                        _isPermanentlyOpen = true;
                        _isOpen = true;
                        if (socket != null)
                        {
                            socket.OnAutoDoorOpened();
                        }
                    }
                    else
                    {
                        // 잠긴 문은 열리지 않음
                        _isOpen = false;
                    }
                }
                else
                {
                    // 일반 자동문 로직: 한번 열린 문은 열쇠가 있건 없건 이제 영원히 닫히지 않는다
                    if (!_isOpen && dist <= _detectDistance)
                    {
                        _isOpen = true;
                        _isPermanentlyOpen = true;
                        _openedTimestamp = Time.time;
                        if (socket != null)
                        {
                            socket.OnAutoDoorOpened();
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
    }
}
