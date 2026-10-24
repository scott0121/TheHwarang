using System;
using System.Collections;
using System.Collections.Generic;
using MythOfGuotaji.Combat;
using MythOfGuotaji.Core;
using MythOfGuotaji.Dungeon;
using UnityEngine;

namespace MythOfGuotaji.Actor
{
    public enum MonsterType
    {
        Minion = 0,         // 물량형 기본 돌진 잡몹
        Elite = 1,          // 정예 요괴 (열쇠 수문장)
        FoxBoss = 2,        // 여우 요괴 (던전 최종 보스)
        Minion_Archer = 3,  // 맹독충 (원거리 투사체 발사형: 사거리 15m, 쿨 0.7s, 속도 6m/s)
        Minion_BigMon = 4,  // 거북이 (느린 탱커형: 전방 90도 0.5초 전조 후 휩쓸기 3m 넉백)
        Minion_Zhu = 5      // 주(鴸: 사거리 20m, 쿨 3초, 5m 원형 장판, 2초 고정 포물선 폭발 투사체)
    }

    /// <summary>
    /// 산해경 동양 요괴 AI (스마트 네비게이션 & 군집 최적화)
    /// - 벽 너머의 플레이어를 미로 통로(Doorway)를 통해 영리하게 우회 추격
    /// - 장애물 감지 시 휘스커(Whisker) 스티어링으로 벽을 돌아가는 자율 주행
    /// - 로컬 분리(Flocking)로 몬스터 간 자연스러운 포위 형성
    /// </summary>
    public class MonsterAI : MonoBehaviour, IDamageable
    {
        [Header("Monster Configuration")]
        [SerializeField] private MonsterType _type = MonsterType.Minion;
        [SerializeField] private float _maxHp = 40f;
        [SerializeField] private float _moveSpeed = 4.2f;
        [SerializeField] private float _attackDamage = 8f;
        [SerializeField] private float _attackRange = 5.0f;
        [SerializeField] private float _attackCooldown = 1.0f;
        [SerializeField] private float _separationRadius = 0.9f;

        [Header("Attack Pattern Settings (Synced with MonsterStatsTable)")]
        [SerializeField] private float _projectileSpeed = 6.0f;
        [SerializeField] private float _kiteDistance = 0.0f;
        [SerializeField] private float _knockbackPower = 2.0f;
        [SerializeField] private float _telegraphDuration = 0.5f;
        [SerializeField] private float _specialAttackRadius = 2.5f;
        [SerializeField] private float _specialAttackAngle = 90.0f;
        [SerializeField] private float _flightDuration = 2.0f;
        [SerializeField] private float _arcHeight = 6.0f;
        [SerializeField] private float _postAttackDelay = 0.2f;

        [Header("Drop Rewards & Dash")]
        [SerializeField] private int _gemDropCount = 1;
        [SerializeField] private int _expPerGem = 1;
        [SerializeField] private float _heartDropChance = 0.25f;
        [SerializeField] private float _heartHealAmount = 25.0f;
        [SerializeField] private float _dashDistance = 5.0f;
        [SerializeField] private float _dashWidth = 0.5f;
        [SerializeField] private float _dashDuration = 0.3f;

        [Header("Key Drop (Elite / Key Keeper)")]
        [SerializeField] private bool _dropsBossKeyOnDeath = false;

        [Header("Perception & Aggro")]
        public static readonly List<MonsterAI> ActiveMonsters = new();

        [SerializeField] private bool _isAggroed = false;
        private Vector3 _spawnOrigin;
        private Vector3 _wanderTarget;
        private float _nextWanderTime = 0f;

        public bool IsAggroed => _isAggroed;

        private float _currentHp;
        private Transform _targetPlayer;
        private CharacterController _cc;
        private Renderer _rend;
        private Color _originalColor = Color.white;
        private Vector3 _originalScale = Vector3.one;
        private float _lastAttackTime = -99f;
        private float _lastEliteSkillTime = -99f;
        private float _lastBossSkill1Time = -99f;
        private float _lastBossSkill2Time = -99f;
        private float _stunTimer = 0f;
        private Coroutine _pullCoroutine;
        private Vector3 _knockbackVelocity;
        private string _poolKey;
        private bool _isSweeping = false;
        private GameObject _activeTelegraph;

        // 동일 볼리(한 틱에 발사된 다발 화살) 동시 피격 데미지 점감 관리
        private readonly Dictionary<int, int> _volleyHitCounts = new();
        private readonly Dictionary<int, float> _volleyFirstHitTimes = new();

        // 길찾기 및 장애물 회피용 변수
        private Vector3 _currentTargetWaypoint;
        private float _nextPathUpdateTime = 0f;
        private LayerMask _obstacleLayerMask;
        private bool _dashHitWallObstacle = false;
        private bool _dashHitPlayer = false;

        // 애니메이터 연동
        private Animator _animator;
        private string _currentAnimState = "";

        public event Action<MonsterAI> OnDied;

        public bool IsDead => _currentHp <= 0f;
        public Transform Transform => transform;
        public MonsterType Type => _type;
        public float CurrentHp => _currentHp;
        public float MaxHp => _maxHp;
        public int Level => _level;

        private int _level = 0;

        private void Awake()
        {
            _cc = GetComponent<CharacterController>();
            _animator = GetComponentInChildren<Animator>();
            _rend = GetComponent<Renderer>();
            if (_rend != null)
            {
                _originalColor = _rend.material.color;
            }
            _originalScale = transform.localScale;
            _currentHp = _maxHp;

            _obstacleLayerMask = ~LayerMask.GetMask("Ignore Raycast");
        }

        private void OnEnable()
        {
            if (!ActiveMonsters.Contains(this))
            {
                ActiveMonsters.Add(this);
            }
            if (_animator == null) _animator = GetComponentInChildren<Animator>();
            _currentAnimState = "";
            _currentHp = _maxHp;
            if (_cc != null) _cc.enabled = true;
            var col = GetComponent<Collider>();
            if (col != null) col.enabled = true;
            _knockbackVelocity = Vector3.zero;
            _lastEliteSkillTime = -99f;
            _lastBossSkill1Time = -99f;
            _lastBossSkill2Time = -99f;
            _stunTimer = 0f;
            _isAggroed = false;
            _spawnOrigin = transform.position;
            _wanderTarget = transform.position;
            _volleyHitCounts.Clear();
            _volleyFirstHitTimes.Clear();
            transform.localScale = _originalScale;
            SetRendererColor(_originalColor);
            FindPlayerTarget();
            UI.MonsterHealthBarManager.Register(this);
        }

        private void OnDisable()
        {
            CleanupTelegraph();
            ActiveMonsters.Remove(this);
            UI.MonsterHealthBarManager.Unregister(this);
        }

        private void OnDestroy()
        {
            CleanupTelegraph();
        }

        public void Setup(MonsterType type, float hp, float speed, float damage, bool dropsKey, string poolKey, float attackRange = -1f, float attackCooldown = -1f, int level = 0)
        {
            CleanupTelegraph();
            _level = level;
            _type = type;
            _maxHp = hp;
            _currentHp = hp;
            _moveSpeed = speed;
            _attackDamage = damage;
            _dropsBossKeyOnDeath = dropsKey;
            _poolKey = poolKey;
            if (attackRange > 0f) _attackRange = attackRange;
            if (attackCooldown > 0f) _attackCooldown = attackCooldown;

            // MonsterStatsTable에서 공격 패턴(원거리 탄속, 쿨다운, 카이팅 거리 등) 동기화
            ApplyPatternStatsFromTable();

            _isAggroed = false;
            _isSweeping = false;
            _lastEliteSkillTime = -99f;
            _lastBossSkill1Time = -99f;
            _lastBossSkill2Time = -99f;
            _stunTimer = 0f;
            _spawnOrigin = transform.position;
            _wanderTarget = transform.position;

            _originalScale = transform.localScale;
            if (_rend != null && _rend.material != null)
            {
                _originalColor = _rend.material.color;
            }
        }

        public void ApplyPatternStatsFromTable()
        {
            var stat = Data.GameDataManager.Instance != null && Data.GameDataManager.Instance.MonsterStats != null
                ? Data.GameDataManager.Instance.MonsterStats.GetStat(_type)
                : null;

            if (stat != null)
            {
                if (stat.attackRange > 0f) _attackRange = stat.attackRange;
                if (stat.attackCooldown > 0f) _attackCooldown = stat.attackCooldown;
                _projectileSpeed = stat.projectileSpeed;
                _kiteDistance = stat.kiteDistance;
                _knockbackPower = stat.knockbackPower;
                _telegraphDuration = stat.telegraphDuration;
                _specialAttackRadius = stat.specialAttackRadius;
                _specialAttackAngle = stat.specialAttackAngle;
                _flightDuration = stat.flightDuration;
                _arcHeight = stat.arcHeight;
                _postAttackDelay = stat.postAttackDelay;
                _gemDropCount = stat.gemDropCount;
                _expPerGem = stat.expPerGem;
                _heartDropChance = stat.heartDropChance > 0f ? stat.heartDropChance : 0.25f;
                _heartHealAmount = stat.heartHealAmount > 0f ? stat.heartHealAmount : 25.0f;
                _dashDistance = stat.dashDistance > 0f ? stat.dashDistance : 5.0f;
                _dashWidth = stat.dashWidth > 0f ? stat.dashWidth : 0.5f;
                _dashDuration = stat.dashDuration > 0f ? stat.dashDuration : 0.3f;
            }
        }

        private void FindPlayerTarget()
        {
            var player = FindAnyObjectByType<PlayerController>();
            if (player != null)
            {
                _targetPlayer = player.transform;
            }
        }

        private float _spawnDelayTimer = 0f;

        /// <summary>
        /// 추가 웨이브 스폰 연출 동안 대기 시간 부여 (HCFX_Buff_Down 연출 후 이동)
        /// </summary>
        public void SetSpawnDelay(float delay)
        {
            _spawnDelayTimer = delay;
            PlayIdleAnimation();
        }

        private void Update()
        {
            if (IsDead) return;

            // 추가 웨이브 스폰 연출 대기
            if (_spawnDelayTimer > 0f)
            {
                _spawnDelayTimer -= Time.deltaTime;
                PlayIdleAnimation();
                return;
            }

            if (_targetPlayer == null)
            {
                FindPlayerTarget();
                if (_targetPlayer == null) return;
            }

            // 1. 비인식(Unaggroed) 상태 처리: 시야에 플레이어가 없으면 쫓지 않고 제자리 배회
            if (!_isAggroed)
            {
                if (CheckPlayerInSight())
                {
                    TriggerAggro();
                }
                else
                {
                    HandleUnaggroedWander();
                    return;
                }
            }

            // 2. 넉백 감쇠
            if (_knockbackVelocity.sqrMagnitude > 0.05f)
            {
                _knockbackVelocity = Vector3.Lerp(_knockbackVelocity, Vector3.zero, Time.deltaTime * 6f);
            }

            // 스턴 상태 처리 (스턴 지속 시간 동안 이동 및 공격 불가)
            if (_stunTimer > 0f)
            {
                _stunTimer -= Time.deltaTime;
                PlayIdleAnimation();
                if (_knockbackVelocity.sqrMagnitude > 0.05f)
                {
                    if (_cc != null && _cc.enabled) _cc.Move((_knockbackVelocity + Vector3.down * 9.81f) * Time.deltaTime);
                }
                return;
            }

            // 스킬 시전 및 공격 모션 선후딜 동안은 이동 정지
            if (_isSweeping)
            {
                if (_knockbackVelocity.sqrMagnitude > 0.05f)
                {
                    _cc.Move(_knockbackVelocity * Time.deltaTime);
                }
                return;
            }

            Vector3 toPlayer = _targetPlayer.position - transform.position;
            toPlayer.y = 0f;
            float distToPlayer = toPlayer.magnitude;

            // 정예 요괴 (MonsterType.Elite) 전용 스킬 & 자동 근접 공격
            if (_type == MonsterType.Elite)
            {
                // 특수스킬: 플레이어가 5m 이상 20m 이내에 위치 시 바닥에 폭 50cm 길이 20m 박스 생성 후 돌덩이 굴림 (쿨 2초)
                if (distToPlayer >= 5.0f && distToPlayer <= 20.0f && Time.time >= _lastEliteSkillTime + 2.0f)
                {
                    StartCoroutine(EliteBoulderSkillRoutine());
                    return;
                }

                // 캐릭터 근접 시 자동 근접 공격 실행
                if (distToPlayer <= _attackRange)
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(toPlayer.normalized), Time.deltaTime * 10f);
                    TryAttackPlayer();
                    PlayIdleAnimation();
                    return;
                }

                // 그 외 거리에서는 플레이어를 향해 이동 추적
                NavigateTowardsTarget(distToPlayer);
                PlayMoveAnimation();
                return;
            }

            // 여우 보스 (MonsterType.FoxBoss) 전용 스킬 & 자동 근접 공격
            if (_type == MonsterType.FoxBoss)
            {
                // 특수스킬 1: 5m ~ 25m, 쿨 5초 (부채꼴 45도, 길이 25m, 1초 후 끌어옴 + 0.5초 스턴)
                // 특수스킬 2: 10m 이내, 쿨 5초 (부채꼴 180도, 길이 10m, 1초 후 20 피해 + 5m 넉백)
                // 우선순위 규칙: 5m ~ 10m 구간에서 둘 다 사용 가능할 경우, 특수스킬 1을 우선적으로 사용!
                bool canSkill1 = (distToPlayer >= 5.0f && distToPlayer <= 25.0f && Time.time >= _lastBossSkill1Time + 5.0f);
                bool canSkill2 = (distToPlayer <= 10.0f && Time.time >= _lastBossSkill2Time + 5.0f);

                if (distToPlayer >= 5.0f && distToPlayer <= 10.0f)
                {
                    if (canSkill1)
                    {
                        StartCoroutine(BossVacuumSkillRoutine());
                        return;
                    }
                    else if (canSkill2)
                    {
                        StartCoroutine(BossCleaveSkillRoutine());
                        return;
                    }
                }
                else if (distToPlayer > 10.0f && distToPlayer <= 25.0f)
                {
                    if (canSkill1)
                    {
                        StartCoroutine(BossVacuumSkillRoutine());
                        return;
                    }
                }
                else if (distToPlayer < 5.0f)
                {
                    if (canSkill2)
                    {
                        StartCoroutine(BossCleaveSkillRoutine());
                        return;
                    }
                }

                // 캐릭터 근접 시 자동 근접 공격 실행
                if (distToPlayer <= _attackRange)
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(toPlayer.normalized), Time.deltaTime * 10f);
                    TryAttackPlayer();
                    PlayIdleAnimation();
                    return;
                }

                // 그 외 거리에서는 플레이어를 향해 이동 추적
                NavigateTowardsTarget(distToPlayer);
                PlayMoveAnimation();
                return;
            }

            // 공격 사거리 내 도달 시 공격
            if (distToPlayer <= _attackRange)
            {
                // 거북이(BigMon): 벽 뒤에 있는 플레이어에게는 돌진할 수 없으므로 시선/돌진 경로가 막혀있으면 우회 추격 지속
                if (_type == MonsterType.Minion_BigMon && !HasClearChargePathToPlayer())
                {
                    NavigateTowardsTarget(distToPlayer);
                    PlayMoveAnimation();
                }
                else
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(toPlayer.normalized), Time.deltaTime * 10f);
                    TryAttackPlayer();

                    // 원거리 카이팅 후퇴 (테이블 kiteDistance 기준)
                    if (_kiteDistance > 0f && distToPlayer < _kiteDistance)
                    {
                        Vector3 backDir = -toPlayer.normalized;
                        _cc.Move(backDir * (_moveSpeed * 0.75f) * Time.deltaTime);
                        PlayMoveAnimation();
                    }
                    else if (!_isSweeping)
                    {
                        PlayIdleAnimation();
                    }
                }
            }
            else
            {
                // 지능적 길찾기 및 장애물 우회 이동 (한번 인식되면 죽을 때까지 추적!)
                NavigateTowardsTarget(distToPlayer);
                PlayMoveAnimation();
            }
        }

        /// <summary>
        /// 방 경계면 및 직시선(Line of Sight) 기반 플레이어 발견 검사
        /// - 같은 방에 플레이어가 진입했을 때
        /// - 인접 방에서 열린 통로를 통해 직시선이 확보되었을 때
        /// </summary>
        private bool CheckPlayerInSight()
        {
            if (_targetPlayer == null || Dungeon.MazeGenerator.Instance == null) return false;

            Vector2Int myRoom = Dungeon.MazeGenerator.Instance.GetRoomCoordsAtWorldPos(transform.position);
            Vector2Int playerRoom = Dungeon.MazeGenerator.Instance.GetRoomCoordsAtWorldPos(_targetPlayer.position);

            // 조건 1: 같은 방에 플레이어 진입 -> 즉시 발견 (단, 시작방(0,0)은 요괴가 존재하지 않는 안전 구역이므로 제외)
            if (myRoom == playerRoom)
            {
                if (playerRoom == Vector2Int.zero)
                {
                    return false;
                }
                return true;
            }

            // 조건 2: 인접 방에 위치하고 방 경계 통로가 열려 있으며 시야가 확보된 경우
            int manhattanDist = Mathf.Abs(myRoom.x - playerRoom.x) + Mathf.Abs(myRoom.y - playerRoom.y);
            if (manhattanDist == 1)
            {
                Dungeon.Direction dirToPlayer = Dungeon.MazeGrid.GetOffsetDirection(playerRoom - myRoom);
                var currentCell = Dungeon.MazeGenerator.Instance.Grid?.GetCell(myRoom);
                if (currentCell != null && currentCell.IsOpen(dirToPlayer))
                {
                    // 양쪽 방의 소켓 및 통로 문 개방 상태 엄격 검사
                    if (Dungeon.MazeGenerator.Instance.InstantiatedRooms.TryGetValue(myRoom, out var myRoomCtrl) &&
                        Dungeon.MazeGenerator.Instance.InstantiatedRooms.TryGetValue(playerRoom, out var playerRoomCtrl))
                    {
                        var mySocket = myRoomCtrl.GetSocket(dirToPlayer);
                        var oppSocket = playerRoomCtrl.GetSocket(Dungeon.MazeGrid.GetOpposite(dirToPlayer));

                        if (mySocket == null || oppSocket == null) return false;

                        // 벽이거나 잠긴 상태(전투 봉인, 보스 봉인 등)면 시야 완전 차단
                        if (mySocket.CurrentState != Dungeon.DoorState.Open || oppSocket.CurrentState != Dungeon.DoorState.Open)
                        {
                            return false;
                        }

                        // 자동문(AutoDoor) 개폐 상태 검사 (양쪽 소켓 중 설치된 문짝 확인)
                        var autoDoor = mySocket.GetComponentInChildren<Dungeon.DungeonAutoDoor>() ?? oppSocket.GetComponentInChildren<Dungeon.DungeonAutoDoor>();
                        if (autoDoor != null && !autoDoor.IsOpen)
                        {
                            return false; // 문이 닫혀있으면 절대 발견 불가
                        }
                    }
                    else
                    {
                        return false;
                    }

                    // 직시선(Line of Sight) 레이캐스트 검사 (최대 24m)
                    Vector3 eye = transform.position + Vector3.up * 1.0f;
                    Vector3 playerEye = _targetPlayer.position + Vector3.up * 1.0f;
                    Vector3 diff = playerEye - eye;

                    if (diff.magnitude <= 24f)
                    {
                        if (Physics.Raycast(eye, diff.normalized, out RaycastHit hit, diff.magnitude, _obstacleLayerMask, QueryTriggerInteraction.Ignore))
                        {
                            if (hit.collider.gameObject == _targetPlayer.gameObject || hit.collider.GetComponentInParent<PlayerController>() != null)
                            {
                                return true;
                            }
                        }
                        else
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        public void TriggerAggro()
        {
            if (_isAggroed) return;
            _isAggroed = true;

            if (_type == MonsterType.FoxBoss && HasAnimState("Roar"))
            {
                StartCoroutine(BossRoarRoutine());
            }

            // 같은 방 내의 다른 요괴들에게도 어그로 전파
            AlertNearbyMonstersInSameRoom();
        }

        private IEnumerator BossRoarRoutine()
        {
            _isSweeping = true;
            PlayAnimation("Roar", 0.1f);
            if (Cameras.QuarterViewCamera.Instance != null)
            {
                Cameras.QuarterViewCamera.Instance.TriggerShake(0.5f, 0.4f);
            }
            yield return new WaitForSeconds(1.1f);
            _isSweeping = false;
        }

        private void AlertNearbyMonstersInSameRoom()
        {
            if (Dungeon.MazeGenerator.Instance == null) return;
            Vector2Int myRoom = Dungeon.MazeGenerator.Instance.GetRoomCoordsAtWorldPos(transform.position);

            Collider[] hits = Physics.OverlapSphere(transform.position, 14f);
            foreach (var hit in hits)
            {
                if (hit.gameObject != gameObject && hit.TryGetComponent<MonsterAI>(out var other))
                {
                    if (!other.IsAggroed)
                    {
                        Vector2Int otherRoom = Dungeon.MazeGenerator.Instance.GetRoomCoordsAtWorldPos(other.transform.position);
                        if (myRoom == otherRoom)
                        {
                            other._isAggroed = true;
                        }
                    }
                }
            }
        }

        private void HandleUnaggroedWander()
        {
            if (_spawnOrigin == Vector3.zero) _spawnOrigin = transform.position;

            if (Time.time >= _nextWanderTime)
            {
                _nextWanderTime = Time.time + UnityEngine.Random.Range(2.5f, 5.0f);
                Vector2 randomCircle = UnityEngine.Random.insideUnitCircle * 5.0f;
                _wanderTarget = _spawnOrigin + new Vector3(randomCircle.x, 0f, randomCircle.y);
            }

            Vector3 toTarget = _wanderTarget - transform.position;
            toTarget.y = 0f;

            if (toTarget.sqrMagnitude > 0.4f)
            {
                Vector3 moveDir = toTarget.normalized;
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(moveDir), Time.deltaTime * 3f);
                Vector3 move = moveDir * (_moveSpeed * 0.35f);
                move.y = -9.81f;
                if (_cc != null && _cc.enabled) _cc.Move(move * Time.deltaTime);
                PlayMoveAnimation();
            }
            else
            {
                PlayIdleAnimation();
            }
        }

        /// <summary>
        /// 벽 너머의 플레이어를 미로 문(Doorway)과 휘스커 회피를 통해 영리하게 추격
        /// </summary>
        private void NavigateTowardsTarget(float distToPlayer)
        {
            // 0.2초마다 상위 경로(목표 방으로 향하는 문 Waypoint) 갱신
            if (Time.time >= _nextPathUpdateTime)
            {
                _nextPathUpdateTime = Time.time + 0.2f;
                UpdateTargetWaypoint();
            }

            Vector3 toWaypoint = _currentTargetWaypoint - transform.position;
            toWaypoint.y = 0f;

            Vector3 desiredDir = toWaypoint.sqrMagnitude > 0.01f ? toWaypoint.normalized : transform.forward;

            // 장애물 감지 시 좌/우로 부드럽게 돌아가는 스티어링 계산
            Vector3 steeredDir = ComputeObstacleAvoidanceSteering(desiredDir);

            // 주변 몬스터 분리(Separation) 벡터 결합
            Vector3 separationVector = ComputeSeparationVector();

            Vector3 finalMoveDir = (steeredDir + separationVector * 0.6f).normalized;
            Vector3 finalMove = finalMoveDir * _moveSpeed;
            finalMove += _knockbackVelocity;
            finalMove.y = -9.81f; // 중력

            if (finalMoveDir.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(finalMoveDir), Time.deltaTime * 8f);
            }

            if (_cc != null && _cc.enabled)
            {
                _cc.Move(finalMove * Time.deltaTime);
            }
            else
            {
                transform.position += finalMove * Time.deltaTime;
            }
        }

        private void UpdateTargetWaypoint()
        {
            if (MazeGenerator.Instance == null)
            {
                _currentTargetWaypoint = _targetPlayer.position;
                return;
            }

            Vector2Int myRoom = MazeGenerator.Instance.GetRoomCoordsAtWorldPos(transform.position);
            Vector2Int playerRoom = MazeGenerator.Instance.GetRoomCoordsAtWorldPos(_targetPlayer.position);

            // 1. 같은 방에 있고 벽으로 시야가 차단되지 않았는지 검사
            if (myRoom == playerRoom)
            {
                Vector3 eye = transform.position + Vector3.up * 1.0f;
                Vector3 playerEye = _targetPlayer.position + Vector3.up * 1.0f;
                Vector3 diff = playerEye - eye;

                // 레이캐스트로 직시선(Line of Sight) 검사
                if (Physics.Raycast(eye, diff.normalized, out RaycastHit hit, diff.magnitude, _obstacleLayerMask, QueryTriggerInteraction.Ignore))
                {
                    if (hit.collider.gameObject == _targetPlayer.gameObject || hit.collider.GetComponentInParent<PlayerController>() != null)
                    {
                        _currentTargetWaypoint = _targetPlayer.position;
                        return;
                    }
                }
                else
                {
                    _currentTargetWaypoint = _targetPlayer.position;
                    return;
                }
            }

            // 2. 다른 방에 있거나 벽 너머에 있는 경우: BFS 최단 경로상의 통로 문(Doorway)을 경유지로 설정
            _currentTargetWaypoint = MazeGenerator.Instance.GetNextWaypointTowards(myRoom, playerRoom);
        }

        /// <summary>
        /// 전방에 벽/장애물이 있을 경우 다각도 레이캐스트(Whiskers)로 우회로를 찾아 부드럽게 돌아감
        /// </summary>
        private Vector3 ComputeObstacleAvoidanceSteering(Vector3 desiredDir)
        {
            desiredDir.y = 0f;
            if (desiredDir.sqrMagnitude < 0.001f) return transform.forward;
            desiredDir.Normalize();

            Vector3 rayStart = transform.position + Vector3.up * 0.9f;
            float checkDist = 1.8f; // 전방 1.8m 감지

            // 정면에 벽/장애물이 있는지 검사
            if (Physics.SphereCast(rayStart, 0.4f, desiredDir, out RaycastHit hit, checkDist, _obstacleLayerMask, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider.gameObject != _targetPlayer.gameObject && hit.collider.GetComponentInParent<PlayerController>() == null)
                {
                    // 좌/우 다각도 휘스커(35°, -35°, 65°, -65°, 90°, -90°, 120°, -120°)로 뚫린 방향 탐색
                    float[] angles = { 35f, -35f, 65f, -65f, 90f, -90f, 120f, -120f };
                    Vector3 bestAltDir = Vector3.zero;
                    float maxClearance = -1f;

                    foreach (float angle in angles)
                    {
                        Vector3 testDir = Quaternion.Euler(0f, angle, 0f) * desiredDir;
                        if (!Physics.SphereCast(rayStart, 0.35f, testDir, out RaycastHit testHit, checkDist, _obstacleLayerMask, QueryTriggerInteraction.Ignore))
                        {
                            // 완전히 뚫린 최적의 우회로 발견
                            return testDir.normalized;
                        }
                        else if (testHit.distance > maxClearance)
                        {
                            maxClearance = testHit.distance;
                            bestAltDir = testDir;
                        }
                    }

                    if (bestAltDir != Vector3.zero)
                    {
                        return bestAltDir.normalized;
                    }
                }
            }

            return desiredDir;
        }

        private Vector3 ComputeSeparationVector()
        {
            Vector3 separation = Vector3.zero;
            Collider[] neighbors = Physics.OverlapSphere(transform.position, _separationRadius);

            foreach (var col in neighbors)
            {
                if (col.gameObject != gameObject)
                {
                    var otherMonster = col.GetComponent<MonsterAI>();
                    if (otherMonster != null || col.CompareTag("Monster"))
                    {
                        Vector3 diff = transform.position - col.transform.position;
                        diff.y = 0f;
                        float mag = diff.magnitude;
                        if (mag > 0.01f)
                        {
                            separation += diff.normalized * (1f - (mag / _separationRadius));
                        }
                    }
                }
            }
            return separation;
        }

        private void TryAttackPlayer()
        {
            if (_isSweeping) return;
            if (Time.time < _lastAttackTime + _attackCooldown) return;

            if (_type == MonsterType.Minion)
            {
                // minion 잡몹: 5m 이내 시 0.3초간 예고 후 최초 목표지점까지 돌진 -> 70cm 광역 -> 2m 내 근접 연계
                StartCoroutine(MinionDashAttackRoutine());
            }
            else if (_type == MonsterType.Minion_Archer)
            {
                // 맹독충(Minion_archer): 사거리 15m 내 플레이어에게 투사체 발사 (쿨 0.7초, 피해 7, 탄속 6m/s)
                Vector3 toPlayer = _targetPlayer.position - transform.position;
                toPlayer.y = 0f;
                float dist = toPlayer.magnitude;

                // 벽 차단 검사 (직시선이 벽에 막혀있으면 사각으로 우회 이동)
                Vector3 eye = transform.position + Vector3.up * 1.0f;
                Vector3 playerEye = _targetPlayer.position + Vector3.up * 1.0f;
                Vector3 diff = playerEye - eye;

                if (Physics.Raycast(eye, diff.normalized, out RaycastHit hit, diff.magnitude, _obstacleLayerMask, QueryTriggerInteraction.Ignore))
                {
                    if (hit.collider.gameObject != _targetPlayer.gameObject && hit.collider.GetComponentInParent<PlayerController>() == null)
                    {
                        NavigateTowardsTarget(dist);
                        return;
                    }
                }

                _lastAttackTime = Time.time;
                ShootVenomProjectile(toPlayer.normalized);
            }
            else if (_type == MonsterType.Minion_BigMon)
            {
                // 거북이(Minion_BigMon): 최대 20m 거리에서 플레이어 발견 시 20m x 1.5m 가이드라인 후 점진 가속 돌진, 충돌/도착 시 반경 3m 충격파
                Vector3 toPlayer = _targetPlayer.position - transform.position;
                toPlayer.y = 0f;
                float dist = toPlayer.magnitude;

                // 벽 차단 검사: 최초 돌진 목표를 잡을 때 플레이어와의 사이에 벽이 있으면 돌진하지 않고 우회 이동
                if (!HasClearChargePathToPlayer())
                {
                    NavigateTowardsTarget(dist);
                    return;
                }

                StartCoroutine(BigMonChargeAttackRoutine());
            }
            else if (_type == MonsterType.Minion_Zhu)
            {
                // 주(鴸): 사거리 20m, 쿨 3초, 공격 시 바닥에 5m 지름 원 그리고 2초 고정 포물선 투사체 발사
                Vector3 toPlayer = _targetPlayer.position - transform.position;
                toPlayer.y = 0f;
                float dist = toPlayer.magnitude;

                // 벽 차단 검사
                Vector3 eye = transform.position + Vector3.up * 1.0f;
                Vector3 playerEye = _targetPlayer.position + Vector3.up * 1.0f;
                Vector3 diff = playerEye - eye;

                if (Physics.Raycast(eye, diff.normalized, out RaycastHit hit, diff.magnitude, _obstacleLayerMask, QueryTriggerInteraction.Ignore))
                {
                    if (hit.collider.gameObject != _targetPlayer.gameObject && hit.collider.GetComponentInParent<PlayerController>() == null)
                    {
                        NavigateTowardsTarget(dist);
                        return;
                    }
                }

                StartCoroutine(ZhuAttackRoutine());
            }
            else if (_type == MonsterType.Elite)
            {
                // 정예 요괴(수문장): 근접 공격 모션 시전 및 타격
                StartCoroutine(EliteMeleeAttackRoutine());
            }
            else if (_type == MonsterType.FoxBoss)
            {
                // 여우 보스: 콤보 및 스매시 공격 모션 시전
                StartCoroutine(BossMeleeAttackRoutine());
            }
            else
            {
                // 기타 기본 근접 공격
                _lastAttackTime = Time.time;
                PlayAttackAnimation();
                if (_targetPlayer.TryGetComponent<IDamageable>(out var damageable))
                {
                    DamageInfo dmg = new DamageInfo(_attackDamage, false, transform.position, transform.forward, _knockbackPower, ElementType.Physical, gameObject);
                    damageable.TakeDamage(dmg);
                }
            }
        }

        /// <summary>
        /// minion 잡몹 전용 5m 돌진 공격 & 도착 후 70cm 광역 및 2m 근접 연계:
        /// 1. 돌진 시작거리: 플레이어 주변 5m 이내
        /// 2. 플레이어 위치를 최초 목표지점으로 락온, 플레이어가 이탈하더라도 최초 목표지점까지 일단 돌진
        /// 3. 돌진 직후 반경 70cm 체크해서 광역 공격
        /// 4. 이후 반경 2m 안에 플레이어가 있으면 후속 근접 공격
        /// </summary>
        private IEnumerator MinionDashAttackRoutine()
        {
            _isSweeping = true;
            _lastAttackTime = Time.time;

            if (_targetPlayer == null)
            {
                _isSweeping = false;
                yield break;
            }

            // 1. 최초 목표지점 락온 (플레이어가 이탈하더라도 최초 목표지점까지 일단 돌진!)
            Vector3 startPos = transform.position;
            Vector3 targetPos = _targetPlayer.position;
            targetPos.y = startPos.y;

            Vector3 dashVec = targetPos - startPos;
            dashVec.y = 0f;
            float dashDist = dashVec.magnitude;
            Vector3 dashDir = dashDist > 0.001f ? dashVec.normalized : transform.forward;

            // 돌진 거리: 최소 1.0m, 최대 _dashDistance (테이블 기본 5.0m)
            float maxDash = _dashDistance > 0f ? _dashDistance : 5.0f;
            dashDist = Mathf.Clamp(dashDist, 1.0f, maxDash);

            transform.rotation = Quaternion.LookRotation(dashDir);

            float dashWidth = _dashWidth > 0f ? _dashWidth : 0.5f;

            // 0.3초간 예고 가이드라인 (최초 목표지점 방향 사각형) 생성
            GameObject telegraph = CreateDashTelegraph(startPos, dashDir, dashDist, dashWidth);
            PlayIdleAnimation();

            float elapsed = 0f;
            float prepDuration = _telegraphDuration > 0f ? _telegraphDuration : 0.3f;
            while (elapsed < prepDuration)
            {
                if (IsDead)
                {
                    CleanupTelegraph();
                    _isSweeping = false;
                    yield break;
                }
                elapsed += Time.deltaTime;
                yield return null;
            }

            CleanupTelegraph();

            // 2. 0.3초만에 최초 목표지점까지 돌진 (플레이어가 이탈하더라도 목표지점까지 일단 돌진)
            PlayAttackAnimation();
            Core.VFXManager.Instance?.SpawnMinionDashDust(startPos, dashDir);
            float dashDuration = _dashDuration > 0f ? _dashDuration : 0.3f;
            float dashElapsed = 0f;
            float dashSpeed = dashDist / dashDuration;

            while (dashElapsed < dashDuration)
            {
                if (IsDead)
                {
                    _isSweeping = false;
                    yield break;
                }

                float dt = Time.deltaTime;
                dashElapsed += dt;

                Vector3 move = dashDir * (dashSpeed * dt);
                move.y = -9.81f * dt;
                if (_cc != null && _cc.enabled)
                {
                    _cc.Move(move);
                }
                else
                {
                    transform.position += move;
                }

                yield return null;
            }

            // 3. 돌진 직후 반경 70cm (0.7m) 체크해서 광역 공격
            Vector3 arrivalPos = transform.position;
            arrivalPos.y = 0f;

            if (_targetPlayer != null && !IsDead)
            {
                Vector3 pPos = _targetPlayer.position;
                pPos.y = 0f;
                float arrivalDist = Vector3.Distance(arrivalPos, pPos);
                float aoeRadius = _specialAttackRadius > 0f ? _specialAttackRadius : 0.7f;

                // 70cm 광역 판정 (피격 반경 0.35m 포함 1.05m 이내)
                Core.VFXManager.Instance?.SpawnMinionArrivalImpact(arrivalPos, aoeRadius);
                if (arrivalDist <= aoeRadius + 0.35f)
                {
                    if (_targetPlayer.TryGetComponent<IDamageable>(out var dmgReceiver))
                    {
                        DamageInfo dmg = new DamageInfo(_attackDamage, false, transform.position, dashDir, _knockbackPower, ElementType.Physical, gameObject);
                        dmgReceiver.TakeDamage(dmg);
                        Debug.Log($"<color=red><b>[잡몹 돌진 적중!]</b> 70cm 광역 판정! 거타지에게 {_attackDamage} 피해 부여!</color>");
                    }
                }
                else
                {
                    Debug.Log("<color=green><b>[잡몹 돌진 회피!]</b> 거타지가 70cm 범위를 벗어나 돌진 데미지 무효화!</color>");
                }

                // 4. 이후 반경 2미터 안에 플레이어가 있으면 후속 근접 공격
                Vector3 toPlayerAfter = _targetPlayer.position - transform.position;
                toPlayerAfter.y = 0f;
                float distAfter = toPlayerAfter.magnitude;

                if (distAfter <= 2.0f + 0.35f)
                {
                    if (toPlayerAfter.sqrMagnitude > 0.001f)
                    {
                        transform.rotation = Quaternion.LookRotation(toPlayerAfter.normalized);
                    }
                    PlayAttackAnimation();
                    yield return new WaitForSeconds(0.15f); // 근접 타격 모션 딜레이

                    if (!IsDead && _targetPlayer != null)
                    {
                        Vector3 currentPos = transform.position;
                        currentPos.y = 0f;
                        Vector3 curPPos = _targetPlayer.position;
                        curPPos.y = 0f;
                        if (Vector3.Distance(currentPos, curPPos) <= 2.2f && _targetPlayer.TryGetComponent<IDamageable>(out var meleeReceiver))
                        {
                            DamageInfo meleeDmg = new DamageInfo(_attackDamage, false, transform.position, transform.forward, _knockbackPower, ElementType.Physical, gameObject);
                            meleeReceiver.TakeDamage(meleeDmg);
                            Debug.Log($"<color=red><b>[잡몹 후속 근접공격 적중!]</b> 2미터 내 거타지에게 {_attackDamage} 추가 피해 부여!</color>");
                        }
                    }
                }
            }

            _isSweeping = false;
        }

        public void CleanupTelegraph()
        {
            if (_activeTelegraph != null)
            {
                Destroy(_activeTelegraph);
                _activeTelegraph = null;
            }
        }

        private GameObject CreateDashTelegraph(Vector3 startPos, Vector3 dashDir, float length, float width)
        {
            CleanupTelegraph();
            GameObject telegraph = new GameObject("[Minion_DashTelegraph]");
            _activeTelegraph = telegraph;
            telegraph.transform.position = startPos + dashDir * (length * 0.5f) + Vector3.up * 0.05f;
            telegraph.transform.rotation = Quaternion.LookRotation(dashDir);

            // 1. 바닥 면 (Semi-transparent inner fill)
            var fillObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            fillObj.name = "Fill";
            fillObj.transform.SetParent(telegraph.transform, false);
            fillObj.transform.localPosition = Vector3.zero;
            fillObj.transform.localScale = new Vector3(width, 0.005f, length);
            var col = fillObj.GetComponent<Collider>();
            if (col != null) Destroy(col);

            var mr = fillObj.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.material = ZhuParabolicProjectile.CreateTelegraphMaterial(new Color(1f, 0.25f, 0.1f, 0.35f));
            }

            // 2. 외곽선 테두리 (좌, 우, 전방)
            CreateBorderLine(fillObj.transform, new Vector3(-0.48f, 0.5f, 0f), new Vector3(0.06f, 1.2f, 1f));
            CreateBorderLine(fillObj.transform, new Vector3(0.48f, 0.5f, 0f), new Vector3(0.06f, 1.2f, 1f));
            CreateBorderLine(fillObj.transform, new Vector3(0f, 0.5f, 0.5f), new Vector3(1f, 1.2f, 0.04f));

            return telegraph;
        }

        private void CreateBorderLine(Transform parent, Vector3 localPos, Vector3 localScale)
        {
            var line = GameObject.CreatePrimitive(PrimitiveType.Cube);
            line.name = "BorderLine";
            line.transform.SetParent(parent, false);
            line.transform.localPosition = localPos;
            line.transform.localScale = localScale;

            var c = line.GetComponent<Collider>();
            if (c != null) Destroy(c);

            var r = line.GetComponent<Renderer>();
            if (r != null)
            {
                r.material = ZhuParabolicProjectile.CreateTelegraphMaterial(new Color(1f, 0.4f, 0.15f, 0.85f));
            }
        }

        /// <summary>
        /// 맹독충 전용 원거리 투사체 발사 (HCFX_ElementOrb_04 프리팹 적용)
        /// </summary>
        private void ShootVenomProjectile(Vector3 dir)
        {
            PlayAttackAnimation();
            Vector3 spawnPos = transform.position + Vector3.up * 1.0f + dir * 1.1f;
            Core.VFXManager.Instance?.SpawnVenomMuzzle(spawnPos, dir);

            GameObject orb = null;
            var orbPrefab = Core.VFXManager.Instance?.GetVFXPrefab("HCFX_ElementOrb_04");
            if (orbPrefab != null)
            {
                orb = Instantiate(orbPrefab, spawnPos, Quaternion.LookRotation(dir));
                orb.transform.localScale = Vector3.one * 0.75f;
            }
            else
            {
                orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                orb.transform.position = spawnPos;
                orb.transform.rotation = Quaternion.LookRotation(dir);
                orb.transform.localScale = Vector3.one * 0.45f;

                var rend = orb.GetComponent<Renderer>();
                if (rend != null)
                {
                    var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color"));
                    Color venomColor = new Color(0.2f, 0.95f, 0.25f);
                    mat.color = venomColor;
                    if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", venomColor);
                    rend.material = mat;
                }
            }

            orb.name = "VenomProjectile_ToxicBug";
            orb.tag = "Monster";

            var col = orb.GetComponent<Collider>();
            if (col == null) col = orb.AddComponent<SphereCollider>();
            col.isTrigger = true;

            var rb = orb.GetComponent<Rigidbody>();
            if (rb == null) rb = orb.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            var proj = orb.AddComponent<Projectile>();
            DamageInfo dmg = new DamageInfo(_attackDamage, false, spawnPos, dir, _knockbackPower, ElementType.Poison, gameObject);
            proj.Initialize(dmg, pierceCount: 0, ~LayerMask.GetMask("Ignore Raycast"), poolKey: "", maxRange: _attackRange, unlimitedPierce: false, speed: _projectileSpeed);
            proj.ApplyTopDepthPriority();
        }

        /// <summary>
        /// 주(鴸) 전용 공격 코루틴 (HCFX_ElementOrb_01 프리팹 적용)
        /// </summary>
        private IEnumerator ZhuAttackRoutine()
        {
            _isSweeping = true;
            _lastAttackTime = Time.time;
            PlayAttackAnimation();

            Vector3 spawnPos = transform.position + Vector3.up * 1.2f + transform.forward * 0.8f;
            Vector3 targetFloorPos = _targetPlayer != null ? _targetPlayer.position : transform.position + transform.forward * 10f;
            targetFloorPos.y = 0.05f;

            GameObject zhuProjObj = null;
            var zhuPrefab = Core.VFXManager.Instance?.GetVFXPrefab("HCFX_ElementOrb_01");
            if (zhuPrefab != null)
            {
                zhuProjObj = Instantiate(zhuPrefab, spawnPos, Quaternion.identity);
                zhuProjObj.transform.localScale = Vector3.one * 0.85f;
            }
            else
            {
                zhuProjObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                zhuProjObj.transform.position = spawnPos;
                zhuProjObj.transform.localScale = Vector3.one * 0.65f;
            }

            zhuProjObj.name = "ZhuParabolicProjectile";
            zhuProjObj.tag = "Monster";

            var col = zhuProjObj.GetComponent<Collider>();
            if (col == null) col = zhuProjObj.AddComponent<SphereCollider>();
            col.isTrigger = true;

            var rb = zhuProjObj.GetComponent<Rigidbody>();
            if (rb == null) rb = zhuProjObj.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            var script = zhuProjObj.AddComponent<ZhuParabolicProjectile>();
            script.Initialize(gameObject, spawnPos, targetFloorPos, _attackDamage, _flightDuration, _arcHeight, _specialAttackRadius);
            script.ApplyTopDepthPriority();

            // 몬스터는 투사체를 발사 후 설정된 후딜레이 뒤 바로 이동 가능
            yield return new WaitForSeconds(_postAttackDelay);
            _isSweeping = false;
        }

        /// <summary>
        /// 플레이어와의 직시선 및 돌진 궤적 상에 벽이나 장애물이 있는지 검사
        /// - 같은 편 몬스터는 충돌/시야 차단에서 제외
        /// - 지형/벽체가 가로막고 있으면 false 반환
        /// </summary>
        public bool HasClearChargePathToPlayer()
        {
            if (_targetPlayer == null) return false;

            Vector3 myEye = transform.position + Vector3.up * 1.0f;
            Vector3 playerEye = _targetPlayer.position + Vector3.up * 1.0f;
            Vector3 toPlayer = playerEye - myEye;
            float dist = toPlayer.magnitude;

            if (dist < 0.2f) return true;
            Vector3 dir = toPlayer / dist;

            // 1. 레이캐스트 검사: 플레이어와 몬스터 사이 직시선 상에 벽이 있는지 확인 (같은 편 몬스터는 관통)
            RaycastHit[] hits = Physics.RaycastAll(myEye, dir, dist, _obstacleLayerMask, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (var hit in hits)
            {
                if (hit.collider == null || hit.collider.gameObject == gameObject) continue;

                // 같은 편 몬스터는 시선 차단하지 않음
                if (hit.collider.CompareTag("Monster") || hit.collider.GetComponentInParent<MonsterAI>() != null)
                {
                    continue;
                }

                // 플레이어에 먼저 닿았으면 벽에 막히지 않음
                if (hit.collider.CompareTag("Player") || hit.collider.GetComponentInParent<PlayerController>() != null)
                {
                    break;
                }

                // 지형, 벽, 문 등 장애물에 먼저 부딪힌 경우 돌진 불가
                return false;
            }

            // 2. 스피어캐스트 검사: 거북이의 덩치(반경 0.45m)가 지나갈 수 있는 너비에 벽이 가로막고 있는지 확인
            RaycastHit[] sphereHits = Physics.SphereCastAll(myEye, 0.45f, dir, dist, _obstacleLayerMask, QueryTriggerInteraction.Ignore);
            System.Array.Sort(sphereHits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (var sHit in sphereHits)
            {
                if (sHit.collider == null || sHit.collider.gameObject == gameObject) continue;

                if (sHit.collider.CompareTag("Monster") || sHit.collider.GetComponentInParent<MonsterAI>() != null)
                {
                    continue;
                }

                if (sHit.collider.CompareTag("Player") || sHit.collider.GetComponentInParent<PlayerController>() != null)
                {
                    break;
                }

                return false;
            }

            return true;
        }

        /// <summary>
        /// 거북이(Minion_BigMon) 전용 20m 점진 가속 돌진 및 3m 충격파 공격:
        /// 1. 최대 20m 거리에서 플레이어 발견 시 시전 (벽 차단 시 취소)
        /// 2. 바닥에 길이 20m, 폭 1.5m 사각형 가이드라인(Telegraph) 생성
        /// 3. 돌진 시작 시 시선 재검사 후 3초간 20m 끝까지 점진적 가속운동(곡선 가속)으로 돌진
        /// 4. 중간에 플레이어나 지형 장애물에 부딪히면 멈춤 (같은 편 몬스터는 충돌 무시)
        /// 5. 20m 도착 또는 플레이어/벽 충돌로 멈추면 "쿵!" 사운드 이펙트와 반경 3m 원형 충격파 공격
        /// </summary>
        private IEnumerator BigMonChargeAttackRoutine()
        {
            _isSweeping = true;
            _lastAttackTime = Time.time;

            if (_targetPlayer == null || !HasClearChargePathToPlayer())
            {
                _isSweeping = false;
                yield break;
            }

            // 1. 플레이어 방향 조준 및 가이드라인 생성
            Vector3 startPos = transform.position;
            Vector3 toPlayer = _targetPlayer.position - startPos;
            toPlayer.y = 0f;
            Vector3 dashDir = toPlayer.sqrMagnitude > 0.001f ? toPlayer.normalized : transform.forward;
            transform.rotation = Quaternion.LookRotation(dashDir);

            float totalDistance = _dashDistance > 0f ? _dashDistance : 20.0f;
            float dashWidth = _dashWidth > 0f ? _dashWidth : 1.5f; // 바닥 인디케이터 넓이 1.5m
            float totalDuration = _dashDuration > 0f ? _dashDuration : 3.0f; // 5초 -> 3초로 축소

            // 바닥에 길이 20미터, 폭 1.5미터 사각형 가이드라인 생성 (돌격을 멈출 때까지 유지!)
            GameObject telegraph = CreateDashTelegraph(startPos, dashDir, totalDistance, dashWidth);
            PlayIdleAnimation();

            float prepDuration = _telegraphDuration > 0f ? _telegraphDuration : 0.4f;
            float prepElapsed = 0f;
            while (prepElapsed < prepDuration)
            {
                if (IsDead)
                {
                    CleanupTelegraph();
                    SetAnimSpeed(1.0f);
                    _isSweeping = false;
                    yield break;
                }
                prepElapsed += Time.deltaTime;
                yield return null;
            }

            // 돌진 시작 직전 시선 재검사: 0.4초 예고 시간 동안 플레이어가 벽 뒤로 숨었으면 즉시 돌진 취소
            if (!HasClearChargePathToPlayer())
            {
                CleanupTelegraph();
                SetAnimSpeed(1.0f);
                _isSweeping = false;
                yield break;
            }

            // 2. 즉시 3초간 20미터 끝까지 점진적 가속운동으로 돌진 (가이드라인 유지, Walk 애니메이션 3배속 재생)
            // 같은 편 몬스터와의 충돌 무시 설정 (오직 플레이어와 지형 장애물에만 충돌하도록 CC 충돌 무시)
            var allMonsters = FindObjectsByType<MonsterAI>();
            List<CharacterController> ignoredCCs = new List<CharacterController>();
            if (_cc != null)
            {
                foreach (var m in allMonsters)
                {
                    if (m != null && m != this && m.TryGetComponent<CharacterController>(out var otherCC))
                    {
                        Physics.IgnoreCollision(_cc, otherCC, true);
                        ignoredCCs.Add(otherCC);
                    }
                }
            }

            void RestoreIgnoredCollisions()
            {
                if (_cc != null)
                {
                    foreach (var otherCC in ignoredCCs)
                    {
                        if (otherCC != null)
                        {
                            Physics.IgnoreCollision(_cc, otherCC, false);
                        }
                    }
                }
                ignoredCCs.Clear();
            }

            SetAnimSpeed(3.0f);
            PlayMoveAnimation();

            float elapsed = 0f;
            float prevDist = 0f;
            float dustTimer = 0f;
            bool hasCollided = false;
            bool hitPlayerDuringDash = false;

            while (elapsed < totalDuration)
            {
                if (IsDead)
                {
                    RestoreIgnoredCollisions();
                    CleanupTelegraph();
                    SetAnimSpeed(1.0f);
                    _isSweeping = false;
                    yield break;
                }

                float dt = Time.deltaTime;
                elapsed += dt;
                dustTimer += dt;
                if (dustTimer >= 0.15f)
                {
                    dustTimer = 0f;
                    Core.VFXManager.Instance?.SpawnBigMonRunDust(transform.position, dashDir);
                }

                float u = Mathf.Clamp01(elapsed / totalDuration);

                // 점진적 가속 곡선: s(u) = TotalDist * u^2 (처음엔 느리게 가지만 점차 빠르게 이동하는 곡선)
                float curDist = totalDistance * Mathf.Pow(u, 2.0f);
                float stepDist = curDist - prevDist;
                prevDist = curDist;

                _dashHitWallObstacle = false;
                _dashHitPlayer = false;

                // A. 전방 지형/벽체 장애물 및 플레이어 충돌 검사 (같은 편 몬스터는 제외!)
                Vector3 origin = transform.position + Vector3.up * 0.8f;
                RaycastHit[] fwdHits = Physics.SphereCastAll(origin, 0.45f, dashDir, stepDist + 0.35f, _obstacleLayerMask, QueryTriggerInteraction.Ignore);
                System.Array.Sort(fwdHits, (a, b) => a.distance.CompareTo(b.distance));

                foreach (var hit in fwdHits)
                {
                    if (hit.collider == null || hit.collider.gameObject == gameObject) continue;

                    // 같은 편 몬스터는 충돌 무시 (통과)
                    if (hit.collider.CompareTag("Monster") || hit.collider.GetComponentInParent<MonsterAI>() != null)
                    {
                        continue;
                    }

                    // 플레이어 충돌 -> 멈춤
                    if (hit.collider.CompareTag("Player") || hit.collider.GetComponentInParent<PlayerController>() != null)
                    {
                        hasCollided = true;
                        break;
                    }

                    // 지형 / 벽체 장애물 충돌 -> 멈춤
                    hasCollided = true;
                    _dashHitWallObstacle = true;
                    break;
                }

                // B. 돌진 경로 및 주변 캐릭터(플레이어) 충돌 및 3m 넉백 판정 (인디케이터 폭 1.5m 범위)
                if (!hasCollided && _targetPlayer != null)
                {
                    Vector3 pVec = _targetPlayer.position - transform.position;
                    pVec.y = 0f;
                    float forwardDist = Vector3.Dot(dashDir, pVec);
                    Vector3 lateralVec = pVec - dashDir * forwardDist;
                    float lateralDist = lateralVec.magnitude;

                    // 인디케이터 폭 1.5m (중심선 기준 반경 0.75m + 피격 반경 0.5m = 1.25m) 내이고, 전방 stepDist 내 진입 시
                    if (lateralDist <= (dashWidth * 0.5f + 0.5f) && forwardDist >= -0.5f && forwardDist <= (stepDist + 1.2f))
                    {
                        if (_targetPlayer.TryGetComponent<IDamageable>(out var dmgReceiver) && !dmgReceiver.IsDead)
                        {
                            Vector3 knockDir = (dashDir * 0.8f + lateralVec.normalized * 0.4f);
                            knockDir.y = 0f;
                            if (knockDir.sqrMagnitude < 0.001f) knockDir = dashDir;
                            knockDir.Normalize();

                            DamageInfo dmg = new DamageInfo(_attackDamage, false, _targetPlayer.position, knockDir, 18.0f, ElementType.Physical, gameObject);
                            dmgReceiver.TakeDamage(dmg);
                            hitPlayerDuringDash = true;
                            Debug.Log($"<color=red><b>[거북이 1.5m 돌진 직격!]</b> 플레이어에게 {_attackDamage} 피해 및 3미터 넉백 부여!</color>");
                        }
                        hasCollided = true;
                    }
                }

                // 이동 적용
                Vector3 move = dashDir * stepDist;
                move.y = -9.81f * dt;
                if (_cc != null && _cc.enabled)
                {
                    _cc.Move(move);
                    // OnControllerColliderHit에서 감지된 벽 또는 플레이어 충돌 처리
                    if (_dashHitWallObstacle || _dashHitPlayer)
                    {
                        hasCollided = true;
                    }
                }
                else
                {
                    transform.position += move;
                }

                if (hasCollided)
                {
                    break;
                }

                yield return null;
            }

            // 충돌 무시 상태 복원
            RestoreIgnoredCollisions();

            // 벽에 부딪혀 멈춘 경우, 벽 콜라이더 내부에 끼여 비비지 않도록 살짝 반대 방향으로 후퇴
            if (_dashHitWallObstacle && _cc != null && _cc.enabled)
            {
                _cc.Move(-dashDir * 0.15f);
            }

            // 3. 돌격을 멈출 때까지 유지했던 가이드라인을 이제 제거 및 애니메이션 속도 원복
            CleanupTelegraph();
            SetAnimSpeed(1.0f);

            // 4. 20미터 도착 또는 플레이어/벽 충돌로 멈춤: "쿵!" 사운드 및 반경 3미터 충격파 공격
            Vector3 stopPos = transform.position;
            PlayAttackAnimation(); // 스매시 / 내려치기 공격 모션
            PlayImpactSound();     // "쿵!" 사운드 이펙트
            if (Cameras.QuarterViewCamera.Instance != null)
            {
                Cameras.QuarterViewCamera.Instance.TriggerShake(0.5f, 0.35f);
            }

            float shockwaveRadius = _specialAttackRadius > 0f ? _specialAttackRadius : 3.0f;
            SpawnShockwaveVisualEffect(stopPos, shockwaveRadius);
            Core.VFXManager.Instance?.SpawnBigMonGroundImpact(stopPos, shockwaveRadius);

            // 반경 3미터 이내 원형 범위 충격파 공격 판정
            if (_targetPlayer != null && !IsDead)
            {
                Vector3 pPos = _targetPlayer.position;
                pPos.y = stopPos.y;
                float distToTarget = Vector3.Distance(stopPos, pPos);

                if (!hitPlayerDuringDash && distToTarget <= shockwaveRadius + 0.35f)
                {
                    if (_targetPlayer.TryGetComponent<IDamageable>(out var dmgReceiver))
                    {
                        Vector3 knockDir = pPos - stopPos;
                        knockDir.y = 0f;
                        if (knockDir.sqrMagnitude < 0.001f) knockDir = transform.forward;
                        knockDir.Normalize();

                        DamageInfo dmg = new DamageInfo(_attackDamage, false, stopPos, knockDir, 18.0f, ElementType.Physical, gameObject);
                        dmgReceiver.TakeDamage(dmg);
                        Debug.Log($"<color=red><b>[거북이 쿵! 충격파 적중!]</b> 3m 반경 충격파로 플레이어에게 {_attackDamage} 피해 및 3m 넉백 부여!</color>");
                    }
                }
                else if (distToTarget > shockwaveRadius + 0.35f)
                {
                    Debug.Log("<color=green><b>[거북이 충격파 회피!]</b> 거타지가 3m 충격파 범위를 벗어나 무효화되었습니다.</color>");
                }
            }

            // 후딜레이
            yield return new WaitForSeconds(_postAttackDelay > 0f ? _postAttackDelay : 0.4f);
            _isSweeping = false;
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (!_isSweeping || _type != MonsterType.Minion_BigMon) return;
            if (hit.collider == null || hit.collider.gameObject == gameObject) return;

            // 같은 편 몬스터는 충돌 대상에서 완전 제외
            if (hit.collider.CompareTag("Monster") || hit.collider.GetComponentInParent<MonsterAI>() != null)
            {
                return;
            }

            // 플레이어 접촉 시 충돌 등록
            if (hit.collider.CompareTag("Player") || hit.collider.GetComponentInParent<PlayerController>() != null)
            {
                _dashHitPlayer = true;
                return;
            }

            // 발 밑 바닥/경사면(수직 성분이 큰 경우)은 전방 충돌이 아님
            if (hit.normal.y > 0.5f) return;

            // 전방 장애물/벽면과 충돌했을 때만 벽 충돌로 판정
            if (Vector3.Dot(hit.normal, -transform.forward) > 0.15f)
            {
                _dashHitWallObstacle = true;
            }
        }

        private static AudioClip s_koongThudClip = null;

        private void PlayImpactSound()
        {
            var audioSrc = GetComponent<AudioSource>();
            if (audioSrc == null)
            {
                audioSrc = gameObject.AddComponent<AudioSource>();
                audioSrc.spatialBlend = 1.0f;
                audioSrc.minDistance = 2f;
                audioSrc.maxDistance = 35f;
            }

            if (s_koongThudClip == null)
            {
                int sampleRate = 44100;
                float duration = 0.45f;
                int sampleCount = (int)(sampleRate * duration);
                float[] samples = new float[sampleCount];
                for (int i = 0; i < sampleCount; i++)
                {
                    float t = (float)i / sampleRate;
                    // 쿵! sound: deep bass thud with exponential decay
                    float freq = Mathf.Lerp(160f, 40f, t / duration);
                    float sine = Mathf.Sin(2f * Mathf.PI * freq * t);
                    float env = Mathf.Exp(-7.0f * t);
                    float noise = (UnityEngine.Random.value * 2f - 1f) * Mathf.Exp(-18.0f * t);
                    samples[i] = Mathf.Clamp((sine * 0.8f + noise * 0.4f) * env, -1f, 1f);
                }
                s_koongThudClip = AudioClip.Create("BigMon_KoongThud", sampleCount, 1, sampleRate, false);
                s_koongThudClip.SetData(samples, 0);
            }

            audioSrc.PlayOneShot(s_koongThudClip, 1.0f);
        }

        private void SpawnShockwaveVisualEffect(Vector3 center, float radius)
        {
            var fxObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            fxObj.name = "BigMon_Shockwave";
            fxObj.transform.position = center + Vector3.up * 0.05f;
            fxObj.transform.localScale = new Vector3(radius * 2f, 0.02f, radius * 2f);

            var col = fxObj.GetComponent<Collider>();
            if (col != null) Destroy(col);

            var rend = fxObj.GetComponent<Renderer>();
            if (rend != null)
            {
                var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color"));
                Color waveColor = new Color(1f, 0.45f, 0.1f, 0.6f);
                mat.color = waveColor;
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", waveColor);
                rend.material = mat;
            }

            StartCoroutine(AnimateShockwaveRing(fxObj, radius));
        }

        private IEnumerator AnimateShockwaveRing(GameObject fxObj, float maxRadius)
        {
            float dur = 0.35f;
            float elap = 0f;
            Renderer rend = fxObj != null ? fxObj.GetComponent<Renderer>() : null;
            Material mat = rend != null ? rend.material : null;

            while (elap < dur)
            {
                if (fxObj == null) yield break;
                elap += Time.deltaTime;
                float progress = elap / dur;
                float curRadius = Mathf.Lerp(0.5f, maxRadius, Mathf.Sqrt(progress));
                fxObj.transform.localScale = new Vector3(curRadius * 2f, 0.02f, curRadius * 2f);

                if (mat != null)
                {
                    Color c = mat.color;
                    c.a = Mathf.Lerp(0.7f, 0f, progress);
                    mat.color = c;
                    if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
                }
                yield return null;
            }

            if (fxObj != null) Destroy(fxObj);
        }

        private IEnumerator EliteMeleeAttackRoutine()
        {
            _isSweeping = true;
            _lastAttackTime = Time.time;

            PlayAttackAnimation();

            // 공격 모션 선딜레이 (0.3초)
            yield return new WaitForSeconds(0.3f);

            if (!IsDead && _targetPlayer != null)
            {
                float dist = Vector3.Distance(transform.position, _targetPlayer.position);
                if (dist <= _attackRange + 0.8f)
                {
                    if (_targetPlayer.TryGetComponent<IDamageable>(out var damageable))
                    {
                        DamageInfo dmg = new DamageInfo(_attackDamage, false, transform.position, transform.forward, _knockbackPower, ElementType.Physical, gameObject);
                        damageable.TakeDamage(dmg);
                        Debug.Log($"<color=red><b>[정예 요괴 공격 적중!]</b> 플레이어에게 {_attackDamage} 피해 부여!</color>");
                    }
                }
            }

            // 후딜레이 (0.3초)
            yield return new WaitForSeconds(0.3f);
            _isSweeping = false;
        }

        private int _bossComboIndex = 0;

        private IEnumerator BossMeleeAttackRoutine()
        {
            _isSweeping = true;
            _lastAttackTime = Time.time;

            // 콤보 모션 번갈아 가며 시전
            string anim = (_bossComboIndex++ % 2 == 0) ? "ClawsAttack3HitCombo" : "SmashAttack";
            if (HasAnimState(anim))
            {
                PlayAnimation(anim, 0.05f);
            }
            else
            {
                PlayAttackAnimation();
            }

            // 공격 모션 선딜레이 (0.35초)
            yield return new WaitForSeconds(0.35f);

            if (!IsDead && _targetPlayer != null)
            {
                float dist = Vector3.Distance(transform.position, _targetPlayer.position);
                if (dist <= _attackRange + 1.2f)
                {
                    if (_targetPlayer.TryGetComponent<IDamageable>(out var damageable))
                    {
                        DamageInfo dmg = new DamageInfo(_attackDamage, false, transform.position, transform.forward, _knockbackPower * 1.5f, ElementType.Physical, gameObject);
                        damageable.TakeDamage(dmg);
                        if (Cameras.QuarterViewCamera.Instance != null)
                        {
                            Cameras.QuarterViewCamera.Instance.TriggerShake(0.3f, 0.25f);
                        }
                        Debug.Log($"<color=red><b>[여우 보스 공격 적중!]</b> 거타지에게 {_attackDamage} 강력한 일격 부여!</color>");
                    }
                }
            }

            // 후딜레이 (0.4초)
            yield return new WaitForSeconds(0.4f);
            _isSweeping = false;
        }

        private GameObject CreateSweepTelegraph(float radius, float arcAngle)
        {
            CleanupTelegraph();
            GameObject obj = new GameObject("SweepTelegraph");
            _activeTelegraph = obj;
            obj.transform.SetParent(transform, false);
            obj.transform.localPosition = new Vector3(0f, 0.06f, 0f);
            obj.transform.localRotation = Quaternion.identity;

            MeshFilter mf = obj.AddComponent<MeshFilter>();
            MeshRenderer mr = obj.AddComponent<MeshRenderer>();

            Mesh mesh = new Mesh();
            int segments = 16;
            Vector3[] vertices = new Vector3[segments + 2];
            int[] triangles = new int[segments * 3];

            vertices[0] = Vector3.zero; // 중심점
            float halfAngle = arcAngle * 0.5f;

            for (int i = 0; i <= segments; i++)
            {
                float currentAngle = Mathf.Lerp(-halfAngle, halfAngle, (float)i / segments);
                float rad = currentAngle * Mathf.Deg2Rad;
                vertices[i + 1] = new Vector3(Mathf.Sin(rad) * radius, 0f, Mathf.Cos(rad) * radius);

                if (i < segments)
                {
                    triangles[i * 3] = 0;
                    triangles[i * 3 + 1] = i + 1;
                    triangles[i * 3 + 2] = i + 2;
                }
            }

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mf.mesh = mesh;

            Material mat = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color"));
            mat.color = new Color(1f, 0.25f, 0.1f, 0.45f);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", new Color(1f, 0.25f, 0.1f, 0.45f));
            mr.material = mat;

            return obj;
        }

        private void SpawnSweepVisualEffect(Vector3 center, Vector3 forward, float radius)
        {
            var fxObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            fxObj.name = "SweepHitWave";
            fxObj.transform.position = center + forward * (radius * 0.5f) + Vector3.up * 0.1f;
            fxObj.transform.rotation = Quaternion.LookRotation(forward);
            fxObj.transform.localScale = new Vector3(radius * 1.6f, 0.05f, radius * 0.8f);

            var col = fxObj.GetComponent<Collider>();
            if (col != null) Destroy(col);

            var rend = fxObj.GetComponent<Renderer>();
            if (rend != null)
            {
                var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color"));
                mat.color = new Color(1f, 0.35f, 0.1f, 0.8f);
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", new Color(1f, 0.35f, 0.1f, 0.8f));
                rend.material = mat;
            }
            Destroy(fxObj, 0.18f);
        }

        #region Stun & Pull Mechanics

        public void ApplyStun(float duration)
        {
            if (IsDead) return;
            _stunTimer = Mathf.Max(_stunTimer, duration);
            PlayHitAnimation();
        }

        public void ApplyPull(Vector3 targetPos, float duration = 0.35f)
        {
            if (IsDead) return;
            if (_pullCoroutine != null) StopCoroutine(_pullCoroutine);
            _pullCoroutine = StartCoroutine(MonsterPullRoutine(targetPos, duration));
        }

        private IEnumerator MonsterPullRoutine(Vector3 targetPos, float duration)
        {
            float elapsed = 0f;
            Vector3 start = transform.position;
            targetPos.y = start.y;
            while (elapsed < duration)
            {
                if (IsDead) yield break;
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                Vector3 newPos = Vector3.Lerp(start, targetPos, t);
                Vector3 delta = newPos - transform.position;
                delta.y = -9.81f * Time.deltaTime;
                if (_cc != null && _cc.enabled) _cc.Move(delta);
                else transform.position += delta;
                yield return null;
            }
            _pullCoroutine = null;
        }

        #endregion

        #region Elite & Boss Special Skill Routines

        /// <summary>
        /// 정예 요괴(Elite KeyKeeper) 특수스킬:
        /// - 플레이어가 5m 이상 20m 이내일 때 시전
        /// - 바닥에 폭 50cm, 길이 20m 박스를 그린 뒤
        /// - 박스 위로 돌덩이(RollingBoulder)를 굴려보낸다 (2초간 20m 등속 운동 10m/s)
        /// - 부딪힌 플레이어에게 데미지 부여, 20m 도달 전 플레이어 또는 벽에 부딪히면 큰 소리를 내고 파괴
        /// - 바닥 가이드 박스도 돌덩이 파괴와 함께 동시에 사라짐
        /// - 쿨타임 2초
        /// </summary>
        private IEnumerator EliteBoulderSkillRoutine()
        {
            _isSweeping = true;
            _lastEliteSkillTime = Time.time;

            if (_targetPlayer == null)
            {
                _isSweeping = false;
                yield break;
            }

            Vector3 startPos = transform.position;
            Vector3 toPlayer = _targetPlayer.position - startPos;
            toPlayer.y = 0f;
            Vector3 throwDir = toPlayer.sqrMagnitude > 0.001f ? toPlayer.normalized : transform.forward;
            transform.rotation = Quaternion.LookRotation(throwDir);

            // 1. 바닥에 폭 50cm(0.5m), 길이 20m 가이드 박스 생성 (돌덩이가 파괴될 때 함께 파괴되도록 RollingBoulder에 전달)
            Color telegraphColor = new Color(0.95f, 0.55f, 0.15f, 0.35f);
            GameObject guideBox = CreateFloorGuideBox(startPos, throwDir, 20.0f, 0.5f, telegraphColor);

            PlayAttackAnimation();

            // 짧은 시전 선딜레이 (0.2초)
            yield return new WaitForSeconds(0.2f);

            if (IsDead)
            {
                if (guideBox != null) Destroy(guideBox);
                _isSweeping = false;
                yield break;
            }

            // 2. 박스 위로 등속(10m/s) 20m 돌덩이 굴려보냄
            GameObject boulderObj = new GameObject("RollingBoulder_Elite");
            var boulder = boulderObj.AddComponent<RollingBoulder>();
            float boulderDamage = _attackDamage * 1.5f;
            boulder.Initialize(gameObject, startPos + throwDir * 0.8f, throwDir, boulderDamage, guideBox);

            // 후딜레이 (0.3초)
            yield return new WaitForSeconds(0.3f);
            _isSweeping = false;
        }

        /// <summary>
        /// 여우 보스(FoxBoss) 특수스킬 1:
        /// - 플레이어가 5m 이상 25m 이내일 때 시전
        /// - 플레이어 방향으로 바닥에 커다란 부채꼴 생성 (길이 25m, 45도 각도)
        /// - 1초 후 부채꼴 안의 모든 플레이어 및 몬스터를 보스 앞으로 끌어온다. 0.5초 스턴.
        /// - 쿨타임 5초
        /// </summary>
        private IEnumerator BossVacuumSkillRoutine()
        {
            _isSweeping = true;
            _lastBossSkill1Time = Time.time;

            if (_targetPlayer == null)
            {
                _isSweeping = false;
                yield break;
            }

            Vector3 startPos = transform.position;
            Vector3 toPlayer = _targetPlayer.position - startPos;
            toPlayer.y = 0f;
            Vector3 fanDir = toPlayer.sqrMagnitude > 0.001f ? toPlayer.normalized : transform.forward;
            transform.rotation = Quaternion.LookRotation(fanDir);

            // 1. 플레이어 방향 바닥에 부채꼴 생성 (길이 25m, 각도 45도)
            Color vacuumColor = new Color(0.85f, 0.2f, 0.65f, 0.38f);
            GameObject sectorTelegraph = CreateWorldSectorTelegraph(startPos, fanDir, 25.0f, 45.0f, vacuumColor);

            if (HasAnimState("Roar")) PlayAnimation("Roar", 0.1f);
            else PlayAttackAnimation();

            // 1초간 예고 대기
            float elapsed = 0f;
            while (elapsed < 1.0f)
            {
                if (IsDead)
                {
                    if (sectorTelegraph != null) Destroy(sectorTelegraph);
                    _isSweeping = false;
                    yield break;
                }
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (sectorTelegraph != null) Destroy(sectorTelegraph);

            // 2. 1초 후 부채꼴 안의 모든 플레이어 및 몬스터를 자기 앞으로 끌어옴 + 0.5초 스턴
            Vector3 pullTargetPos = transform.position + transform.forward * 2.5f;

            SpawnVacuumPullEffect(transform.position, fanDir, 25.0f);
            PlayImpactSound();
            if (Cameras.QuarterViewCamera.Instance != null)
            {
                Cameras.QuarterViewCamera.Instance.TriggerShake(0.45f, 0.35f);
            }

            // A. 플레이어 판정
            if (_targetPlayer != null)
            {
                Vector3 pVec = _targetPlayer.position - transform.position;
                pVec.y = 0f;
                float pDist = pVec.magnitude;
                if (pDist <= 25.0f)
                {
                    float angle = Vector3.Angle(fanDir, pVec.normalized);
                    if (angle <= 22.5f) // 45도의 절반
                    {
                        var playerCtrl = _targetPlayer.GetComponent<PlayerController>();
                        if (playerCtrl != null)
                        {
                            playerCtrl.ApplyPull(pullTargetPos, 0.35f);
                            playerCtrl.ApplyStun(0.5f);
                            Debug.Log("<color=red><b>[여우 보스 진공 흡입!]</b> 거타지가 여우 보스 앞으로 끌려오며 0.5초 스턴에 걸렸습니다!</color>");
                        }
                    }
                }
            }

            // B. 부채꼴 범위 내 모든 몬스터 판정
            for (int i = 0; i < ActiveMonsters.Count; i++)
            {
                var monster = ActiveMonsters[i];
                if (monster != null && monster != this && !monster.IsDead)
                {
                    Vector3 mVec = monster.transform.position - transform.position;
                    mVec.y = 0f;
                    float mDist = mVec.magnitude;
                    if (mDist <= 25.0f)
                    {
                        float angle = Vector3.Angle(fanDir, mVec.normalized);
                        if (angle <= 22.5f)
                        {
                            Vector3 mTarget = pullTargetPos + UnityEngine.Random.insideUnitSphere * 0.8f;
                            mTarget.y = monster.transform.position.y;
                            monster.ApplyPull(mTarget, 0.35f);
                            monster.ApplyStun(0.5f);
                        }
                    }
                }
            }

            // 후딜레이 (0.3초)
            yield return new WaitForSeconds(0.3f);
            _isSweeping = false;
        }

        /// <summary>
        /// 여우 보스(FoxBoss) 특수스킬 2:
        /// - 플레이어가 10m 이내일 때 시전
        /// - 플레이어 방향으로 180도, 10미터 부채꼴 생성
        /// - 1초 뒤 휩쓸기 공격: 데미지 20 (+ 레벨 스케일링) + 5미터 넉백
        /// - 쿨타임 5초
        /// </summary>
        private IEnumerator BossCleaveSkillRoutine()
        {
            _isSweeping = true;
            _lastBossSkill2Time = Time.time;

            if (_targetPlayer == null)
            {
                _isSweeping = false;
                yield break;
            }

            Vector3 startPos = transform.position;
            Vector3 toPlayer = _targetPlayer.position - startPos;
            toPlayer.y = 0f;
            Vector3 cleaveDir = toPlayer.sqrMagnitude > 0.001f ? toPlayer.normalized : transform.forward;
            transform.rotation = Quaternion.LookRotation(cleaveDir);

            // 1. 플레이어 방향으로 180도, 10미터 부채꼴 생성
            Color cleaveColor = new Color(1.0f, 0.28f, 0.1f, 0.42f);
            GameObject sectorTelegraph = CreateWorldSectorTelegraph(startPos, cleaveDir, 10.0f, 180.0f, cleaveColor);

            if (HasAnimState("SmashAttackForward")) PlayAnimation("SmashAttackForward", 0.1f);
            else if (HasAnimState("SmashAttack")) PlayAnimation("SmashAttack", 0.1f);
            else PlayAttackAnimation();

            // 1초간 예고 대기
            float elapsed = 0f;
            while (elapsed < 1.0f)
            {
                if (IsDead)
                {
                    if (sectorTelegraph != null) Destroy(sectorTelegraph);
                    _isSweeping = false;
                    yield break;
                }
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (sectorTelegraph != null) Destroy(sectorTelegraph);

            // 2. 1초 뒤 휩쓸기 공격: 데미지 20 + 5미터 넉백
            PlayAttackAnimation();
            PlayImpactSound();
            SpawnSweepVisualEffect(transform.position, cleaveDir, 10.0f);
            if (Cameras.QuarterViewCamera.Instance != null)
            {
                Cameras.QuarterViewCamera.Instance.TriggerShake(0.5f, 0.35f);
            }

            float baseDamage = 20.0f;
            float finalDamage = baseDamage * (1f + _level * 0.10f); // 몬스터 레벨 공격력 반영

            if (_targetPlayer != null && !IsDead)
            {
                Vector3 pVec = _targetPlayer.position - transform.position;
                pVec.y = 0f;
                float pDist = pVec.magnitude;
                if (pDist <= 10.0f)
                {
                    float angle = Vector3.Angle(cleaveDir, pVec.normalized);
                    if (angle <= 90.0f) // 180도의 절반
                    {
                        if (_targetPlayer.TryGetComponent<IDamageable>(out var dmgReceiver))
                        {
                            Vector3 knockDir = pVec.sqrMagnitude > 0.001f ? pVec.normalized : cleaveDir;
                            DamageInfo dmg = new DamageInfo(finalDamage, false, transform.position, knockDir, 28.0f, ElementType.Physical, gameObject);
                            dmgReceiver.TakeDamage(dmg);
                            Debug.Log($"<color=red><b>[여우 보스 180도 휩쓸기 적중!]</b> 거타지에게 {finalDamage} 피해 및 5m 넉백!</color>");
                        }
                    }
                }
            }

            // 후딜레이 (0.4초)
            yield return new WaitForSeconds(0.4f);
            _isSweeping = false;
        }

        private GameObject CreateWorldSectorTelegraph(Vector3 center, Vector3 forward, float radius, float arcAngle, Color color)
        {
            CleanupTelegraph();
            GameObject obj = new GameObject("[SectorTelegraph]");
            _activeTelegraph = obj;
            obj.transform.position = center + Vector3.up * 0.04f;
            obj.transform.rotation = Quaternion.LookRotation(forward);

            MeshFilter mf = obj.AddComponent<MeshFilter>();
            MeshRenderer mr = obj.AddComponent<MeshRenderer>();

            Mesh mesh = new Mesh();
            int segments = 32;
            Vector3[] vertices = new Vector3[segments + 2];
            int[] triangles = new int[segments * 3];

            vertices[0] = Vector3.zero; // 중심점
            float halfAngle = arcAngle * 0.5f;

            for (int i = 0; i <= segments; i++)
            {
                float currentAngle = Mathf.Lerp(-halfAngle, halfAngle, (float)i / segments);
                float rad = currentAngle * Mathf.Deg2Rad;
                vertices[i + 1] = new Vector3(Mathf.Sin(rad) * radius, 0f, Mathf.Cos(rad) * radius);

                if (i < segments)
                {
                    triangles[i * 3] = 0;
                    triangles[i * 3 + 1] = i + 1;
                    triangles[i * 3 + 2] = i + 2;
                }
            }

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mf.mesh = mesh;

            mr.material = ZhuParabolicProjectile.CreateTelegraphMaterial(color);

            return obj;
        }

        public static GameObject CreateFloorGuideBox(Vector3 startPos, Vector3 dir, float length, float width, Color color)
        {
            GameObject telegraph = new GameObject("[GuideBox]");
            telegraph.transform.position = startPos + dir * (length * 0.5f) + Vector3.up * 0.04f;
            telegraph.transform.rotation = Quaternion.LookRotation(dir);

            // 1. Fill
            var fillObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            fillObj.name = "Fill";
            fillObj.transform.SetParent(telegraph.transform, false);
            fillObj.transform.localPosition = Vector3.zero;
            fillObj.transform.localScale = new Vector3(width, 0.005f, length);
            var col = fillObj.GetComponent<Collider>();
            if (col != null) Destroy(col);

            var mr = fillObj.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.material = ZhuParabolicProjectile.CreateTelegraphMaterial(color);
            }

            // 2. Borders
            CreateTelegraphBorderLine(fillObj.transform, new Vector3(-0.48f, 0.5f, 0f), new Vector3(0.06f, 1.2f, 1f), new Color(color.r, color.g, color.b, Mathf.Min(1f, color.a * 2.2f)));
            CreateTelegraphBorderLine(fillObj.transform, new Vector3(0.48f, 0.5f, 0f), new Vector3(0.06f, 1.2f, 1f), new Color(color.r, color.g, color.b, Mathf.Min(1f, color.a * 2.2f)));
            CreateTelegraphBorderLine(fillObj.transform, new Vector3(0f, 0.5f, 0.5f), new Vector3(1f, 1.2f, 0.04f), new Color(color.r, color.g, color.b, Mathf.Min(1f, color.a * 2.2f)));

            return telegraph;
        }

        private static void CreateTelegraphBorderLine(Transform parent, Vector3 localPos, Vector3 localScale, Color color)
        {
            var line = GameObject.CreatePrimitive(PrimitiveType.Cube);
            line.name = "BorderLine";
            line.transform.SetParent(parent, false);
            line.transform.localPosition = localPos;
            line.transform.localScale = localScale;

            var c = line.GetComponent<Collider>();
            if (c != null) Destroy(c);

            var r = line.GetComponent<Renderer>();
            if (r != null)
            {
                r.material = ZhuParabolicProjectile.CreateTelegraphMaterial(color);
            }
        }

        private void SpawnVacuumPullEffect(Vector3 center, Vector3 forward, float radius)
        {
            var fxObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            fxObj.name = "VacuumPullWave";
            fxObj.transform.position = center + forward * (radius * 0.4f) + Vector3.up * 0.08f;
            fxObj.transform.rotation = Quaternion.LookRotation(forward);
            fxObj.transform.localScale = new Vector3(radius * 0.8f, 0.02f, radius);

            var col = fxObj.GetComponent<Collider>();
            if (col != null) Destroy(col);

            var rend = fxObj.GetComponent<Renderer>();
            if (rend != null)
            {
                var mat = ZhuParabolicProjectile.CreateTelegraphMaterial(new Color(0.7f, 0.1f, 0.9f, 0.7f));
                rend.material = mat;
            }
            Destroy(fxObj, 0.25f);
        }

        #endregion

        public void TakeDamage(DamageInfo info)
        {
            if (IsDead) return;

            TriggerAggro(); // 피격 시 즉시 영구 어그로 발동!

            float damageToApply = info.amount;

            // 한 틱에 발사된 여러 발의 화살(동일 Volley)을 동시에 맞을 때 점감 공식 적용:
            // 1발 = 100% (1.0), 2발 = 50% (0.5), 3발 = 25% (0.25), 4발 = 12.5% (0.125)...
            // 누적 피해: 1발 = 100 / 2발 = 150 / 3발 = 175 / 4발 = 187.5
            if (info.volleyId > 0)
            {
                if (!_volleyFirstHitTimes.TryGetValue(info.volleyId, out float firstTime) || Time.time - firstTime > 0.4f)
                {
                    _volleyHitCounts[info.volleyId] = 1;
                    _volleyFirstHitTimes[info.volleyId] = Time.time;
                }
                else
                {
                    int hitCount = _volleyHitCounts[info.volleyId] + 1;
                    _volleyHitCounts[info.volleyId] = hitCount;

                    float falloff = 0.5f;
                    if (Data.GameDataManager.Instance != null && Data.GameDataManager.Instance.WeaponStats != null)
                    {
                        falloff = Data.GameDataManager.Instance.WeaponStats.bow.multiHitDamageFalloffRatio;
                    }
                    float multiplier = Mathf.Pow(falloff, hitCount - 1);
                    damageToApply *= multiplier;
                }
            }

            _currentHp -= damageToApply;
            _knockbackVelocity = info.hitDirection * info.knockbackForce;

            // 피격 애니메이션 및 피드백 (화이트 플래시 + 스케일 펀치)
            PlayHitAnimation();
            StopCoroutine(nameof(HitFeedbackRoutine));
            StartCoroutine(HitFeedbackRoutine());

            Debug.Log($"<color=yellow>[요괴 피격!] {name} ({_type}) 피격! 적용 피해: {damageToApply:F1} (원피해: {info.amount:F1}, 볼리ID: {info.volleyId}) / 남은 HP: {Mathf.Max(0, _currentHp):F0}/{_maxHp:F0}</color>");

            if (_currentHp <= 0f)
            {
                _currentHp = 0f;
                Die();
            }
        }

        private IEnumerator HitFeedbackRoutine()
        {
            transform.localScale = _originalScale * 1.2f;
            SetRendererColor(Color.white);

            yield return new WaitForSeconds(0.09f);

            transform.localScale = _originalScale;
            if (!IsDead)
            {
                SetRendererColor(_originalColor);
            }
        }

        private void SetRendererColor(Color c)
        {
            if (_rend != null && _rend.material != null)
            {
                _rend.material.color = c;
                if (_rend.material.HasProperty("_BaseColor"))
                {
                    _rend.material.SetColor("_BaseColor", c);
                }
            }
        }

        private void Die()
        {
            CleanupTelegraph();
            Debug.Log($"<color=red><b>[요괴 처치 완료!]</b> {_type} '{name}' 사망! (누적 킬수 +1)</color>");

            ActiveMonsters.Remove(this);
            UI.MonsterHealthBarManager.Unregister(this);
            OnDied?.Invoke(this);

            if (_cc != null) _cc.enabled = false;
            var col = GetComponent<Collider>();
            if (col != null) col.enabled = false;

            PlayDeathAnimation();

            // 사망 시 바닥에 영혼 보석(ExpGem) 드롭 (플레이어는 직접 줍지 않고 헬퍼가 3m 진공 흡수)
            SpawnGemsOnDeath();
            TrySpawnHeartOnDeath();

            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddKill();

                if (_dropsBossKeyOnDeath)
                {
                    GameManager.Instance.AcquireBossKey();
                }

                if (_type == MonsterType.FoxBoss)
                {
                    GameManager.Instance.CompleteStage(affinityReward: 5, goldReward: 100);
                }
            }

            // 카메라 타격감 흔들림 (정예/보스 사망 시)
            if (_type != MonsterType.Minion && Cameras.QuarterViewCamera.Instance != null)
            {
                Cameras.QuarterViewCamera.Instance.TriggerShake(0.35f, 0.3f);
            }

            // 사망 애니메이션을 보여준 뒤 오브젝트 풀 반환
            StartCoroutine(DeathDespawnRoutine(_animator != null && HasAnimState("Death") ? 0.7f : 0f));
        }

        private IEnumerator DeathDespawnRoutine(float delay)
        {
            if (delay > 0.01f) yield return new WaitForSeconds(delay);

            if (ObjectPoolManager.Instance != null && !string.IsNullOrEmpty(_poolKey))
            {
                ObjectPoolManager.Instance.Despawn(gameObject, _poolKey);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        #region Animation Controller Linkage

        public void PlayAnimation(string stateName, float transitionDuration = 0.1f)
        {
            if (_animator == null || string.IsNullOrEmpty(stateName)) return;
            if (_currentAnimState == stateName) return;

            int hash = Animator.StringToHash(stateName);
            if (_animator.HasState(0, hash))
            {
                _animator.CrossFadeInFixedTime(hash, transitionDuration, 0);
                _currentAnimState = stateName;
            }
        }

        public void SetAnimSpeed(float speed)
        {
            if (_animator != null)
            {
                _animator.speed = speed;
            }
        }

        public void PlayMoveAnimation()
        {
            if ((_isSweeping && _type != MonsterType.Minion_BigMon) || IsDead) return;
            if (HasAnimState("Run")) PlayAnimation("Run");
            else if (HasAnimState("WalkForward")) PlayAnimation("WalkForward");
            else if (HasAnimState("Walk")) PlayAnimation("Walk");
            else if (HasAnimState("WalkForward_RM")) PlayAnimation("WalkForward_RM");
            else if (HasAnimState("Run_RM")) PlayAnimation("Run_RM");
            else if (HasAnimState("Fly")) PlayAnimation("Fly");
            else if (HasAnimState("CrawlForward")) PlayAnimation("CrawlForward");
            SetTriggerSafe("walk");
        }

        public void PlayIdleAnimation()
        {
            if (_isSweeping || IsDead) return;
            if (HasAnimState("Idle")) PlayAnimation("Idle");
            else if (HasAnimState("IdleBreathe")) PlayAnimation("IdleBreathe");
            else if (HasAnimState("IdleLookAround")) PlayAnimation("IdleLookAround");
            SetTriggerSafe("idle");
        }

        public void PlayAttackAnimation()
        {
            if (IsDead) return;
            if (_type == MonsterType.Minion)
            {
                if (HasAnimState("BiteAttackForward")) PlayAnimation("BiteAttackForward", 0.05f);
                else if (HasAnimState("JumpBiteAttack")) PlayAnimation("JumpBiteAttack", 0.05f);
                else if (HasAnimState("BiteAttack")) PlayAnimation("BiteAttack", 0.05f);
            }
            else if (_type == MonsterType.Minion_BigMon)
            {
                if (HasAnimState("SmashAttackForwardLeft")) PlayAnimation("SmashAttackForwardLeft", 0.05f);
                else if (HasAnimState("SmashAttackLeft")) PlayAnimation("SmashAttackLeft", 0.05f);
                else if (HasAnimState("SpittersAttack")) PlayAnimation("SpittersAttack", 0.05f);
            }
            else if (_type == MonsterType.Minion_Archer)
            {
                if (HasAnimState("Spit")) PlayAnimation("Spit", 0.05f);
                else if (HasAnimState("SpitAttack")) PlayAnimation("SpitAttack", 0.05f);
            }
            else if (_type == MonsterType.Minion_Zhu)
            {
                if (HasAnimState("FlySpitAttack")) PlayAnimation("FlySpitAttack", 0.05f);
                else if (HasAnimState("JumpBiteAttack")) PlayAnimation("JumpBiteAttack", 0.05f);
            }
            else if (_type == MonsterType.Elite)
            {
                if (HasAnimState("Bite")) PlayAnimation("Bite", 0.05f);
                else if (HasAnimState("Attack")) PlayAnimation("Attack", 0.05f);
                else if (HasAnimState("Attack01")) PlayAnimation("Attack01", 0.05f);
                SetTriggerSafe("attack");
            }
            else if (_type == MonsterType.FoxBoss)
            {
                string anim = (_bossComboIndex++ % 2 == 0) ? "ClawsAttack3HitCombo" : "SmashAttack";
                if (HasAnimState(anim)) PlayAnimation(anim, 0.05f);
                else if (HasAnimState("ClawsAttack2HitCombo")) PlayAnimation("ClawsAttack2HitCombo", 0.05f);
                else if (HasAnimState("JumpSmashAttack")) PlayAnimation("JumpSmashAttack", 0.05f);
                else if (HasAnimState("SmashAttackForward")) PlayAnimation("SmashAttackForward", 0.05f);
                else if (HasAnimState("Attack")) PlayAnimation("Attack", 0.05f);
                SetTriggerSafe("attack");
            }
            else
            {
                if (HasAnimState("BiteAttackForward")) PlayAnimation("BiteAttackForward", 0.05f);
                else if (HasAnimState("BiteAttack")) PlayAnimation("BiteAttack", 0.05f);
                else if (HasAnimState("Bite")) PlayAnimation("Bite", 0.05f);
                else if (HasAnimState("Attack")) PlayAnimation("Attack", 0.05f);
                SetTriggerSafe("attack");
            }
        }

        public void PlayHitAnimation()
        {
            if (IsDead || _isSweeping) return;
            if (HasAnimState("GetHitFront")) PlayAnimation("GetHitFront", 0.05f);
            else if (HasAnimState("GetHit")) PlayAnimation("GetHit", 0.05f);
            else if (HasAnimState("GetHitLeft")) PlayAnimation("GetHitLeft", 0.05f);
            else if (HasAnimState("GetHitRight")) PlayAnimation("GetHitRight", 0.05f);
            else if (HasAnimState("GetHit2")) PlayAnimation("GetHit2", 0.05f);
            else if (HasAnimState("Stun")) PlayAnimation("Stun", 0.05f);
            SetTriggerSafe("stun");
        }

        public void PlayDeathAnimation()
        {
            if (HasAnimState("Death")) PlayAnimation("Death", 0.05f);
            SetTriggerSafe("dead");
        }

        private void SetTriggerSafe(string paramName)
        {
            if (_animator == null || string.IsNullOrEmpty(paramName)) return;
            try
            {
                _animator.SetTrigger(paramName);
            }
            catch { }
        }

        public bool HasAnimState(string stateName)
        {
            if (_animator == null) return false;
            return _animator.HasState(0, Animator.StringToHash(stateName));
        }

        #endregion

        private void SpawnGemsOnDeath()
        {
            int gemCount = _gemDropCount > 0 ? _gemDropCount : 1;
            int expPerGem = _expPerGem > 0 ? _expPerGem : 1;
            ExpGem.SpawnGems(transform.position, gemCount, expPerGem);
        }

        private void TrySpawnHeartOnDeath()
        {
            float chance = _heartDropChance * RecoveryHeart.DropRateMultiplier;
            if (UnityEngine.Random.value <= chance)
            {
                RecoveryHeart.SpawnHeart(transform.position, _heartHealAmount);
            }
        }
    }
}
