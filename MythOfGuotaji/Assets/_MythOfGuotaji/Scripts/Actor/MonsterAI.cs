using System;
using System.Collections;
using System.Collections.Generic;
using MythOfGuotaji.Cameras;
using MythOfGuotaji.Combat;
using MythOfGuotaji.Core;
using MythOfGuotaji.Dungeon;
using MythOfGuotaji.Data;
using UnityEngine;

namespace MythOfGuotaji.Actor
{
    public enum MonsterType
    {
        Minion = 0,         // 물량형 기본 돌진 잡몹
        Elite = 1,          // 정예 요괴 (열쇠 수문장)
        FoxBoss = 2,        // 여우 요괴 (던전 최종 보스)
        Minion_Archer = 3,  // 맹독충 (원거리 투사체 발사형: 사거리 15m, 쿨 0.7s, 속도 6m/s)
        Minion_BigMon = 4,  // 거북이 (중장갑 20m 가속 돌진 & 3m 충격파)
        Minion_Zhu = 5,     // 주(鴸: 사거리 20m, 쿨 3초, 5m 원형 장판, 2초 고정 포물선 폭발 투사체)
        Dokkaebi_Fire = 6,  // 도깨비 화염술사 (패링 핑퐁 역카운터 & 보호막 분쇄)
        Boar_Charger = 7,   // 저돌맹진 멧돼지 요괴 (돌진 카운터 패링 / 벽 충돌 기절)
        Spider_Tether = 8,  // 요선 거미 (영혼 사슬 견인 / 슬라이딩 무적 절단 탈출)
        Mud_Slime = 9,      // 진흙 이괴 (슬로우 늪지 장판 & 카이팅 봉쇄)
        Phantom_Fox = 10,   // 환영 여우귀 (피격 시 3방향 분신술 교란)
        Iron_Shield = 11,   // 흑철 방패병 (풀차징 관통 화살로만 방패 파괴)
        Energy_Absorber = 12,// 영력 흡수 괴물 (차징 금지, 일반 속사 전용)
        Bomb_Sparrow = 13,  // 자폭 참새 떼 (12m/s 초고속 / 화살 1방에 사망)
        Totem_Turret = 14   // 사방신 토템 (고정 포대 사방 회전 탄막 / 패링 반사)
    }

    /// <summary>
    /// 산해경 동양 요괴 AI 코어 (스마트 네비게이션, 시야 검사, 군집 최적화 및 행동 위임 오케스트레이터)
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
        [SerializeField] private float _kiteDistance = 0.0f;
        [SerializeField] private float _knockbackPower = 2.0f;
        [SerializeField] private float _telegraphDuration = 0.5f;
        [SerializeField] private float _specialAttackRadius = 2.5f;
        [SerializeField] private float _specialAttackAngle = 90.0f;
        [SerializeField] private float _postAttackDelay = 0.2f;
        private float _lastMeleeAttackTime = -99f;

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

        [SerializeField] private bool _isDormant = false;
        public bool IsDormant => _isDormant;

        // Behavior Delegate Strategy
        private MonsterBehavior _behavior;
        public MonsterBehavior Behavior => _behavior;

        public void SetDormant(bool dormant)
        {
            _isDormant = dormant;
            if (_isDormant)
            {
                PlayIdleAnimation();
            }
        }

        public static void ReloadAllMonsterData()
        {
            for (int i = 0; i < ActiveMonsters.Count; i++)
            {
                if (ActiveMonsters[i] != null)
                {
                    ActiveMonsters[i].ApplyPatternStatsFromTable();
                }
            }
        }

        // Internal State Fields (Accessed by Behavior)
        internal float _currentHp;
        internal Transform _targetPlayer;
        internal CharacterController _cc;
        internal Renderer _rend;
        internal Color _originalColor = Color.white;
        internal Vector3 _originalScale = Vector3.one;
        internal float _lastAttackTime = -99f;
        internal float _stunTimer = 0f;
        internal Coroutine _pullCoroutine;
        internal Vector3 _knockbackVelocity;
        internal string _poolKey;
        internal bool _isSweeping = false;
        internal GameObject _activeTelegraph;
        internal LayerMask _obstacleLayerMask;

        public Transform TargetPlayer => _targetPlayer;
        public CharacterController CharacterController => _cc;
        public LayerMask ObstacleLayerMask => _obstacleLayerMask;
        public GameObject ActiveTelegraph { get => _activeTelegraph; set => _activeTelegraph = value; }
        public bool IsSweeping { get => _isSweeping; set => _isSweeping = value; }

        public float CurrentHpField { get => _currentHp; set => _currentHp = value; }
        public float MaxHpField { get => _maxHp; set => _maxHp = value; }
        public float MoveSpeed { get => _moveSpeed; set => _moveSpeed = value; }
        public float AttackDamage { get => _attackDamage; set => _attackDamage = value; }
        public float AttackRange { get => _attackRange; set => _attackRange = value; }
        public float AttackCooldown { get => _attackCooldown; set => _attackCooldown = value; }
        public float KiteDistance { get => _kiteDistance; set => _kiteDistance = value; }
        public float KnockbackPower { get => _knockbackPower; set => _knockbackPower = value; }
        public float TelegraphDuration { get => _telegraphDuration; set => _telegraphDuration = value; }
        public float SpecialAttackRadius { get => _specialAttackRadius; set => _specialAttackRadius = value; }
        public float SpecialAttackAngle { get => _specialAttackAngle; set => _specialAttackAngle = value; }
        public float PostAttackDelay { get => _postAttackDelay; set => _postAttackDelay = value; }
        public float DashDistance { get => _dashDistance; set => _dashDistance = value; }
        public float DashWidth { get => _dashWidth; set => _dashWidth = value; }
        public float DashDuration { get => _dashDuration; set => _dashDuration = value; }
        public float LastAttackTime { get => _lastAttackTime; set => _lastAttackTime = value; }
        public float LastMeleeAttackTime { get => _lastMeleeAttackTime; set => _lastMeleeAttackTime = value; }

        // 동일 볼리 피격 데미지 점감
        private readonly Dictionary<int, int> _volleyHitCounts = new();
        private readonly Dictionary<int, float> _volleyFirstHitTimes = new();

        // 길찾기
        private Vector3 _currentTargetWaypoint;
        private float _nextPathUpdateTime = 0f;
        private GameObject _moveLoopSoundInstance;

        // 애니메이터
        private Animator _animator;
        private string _currentAnimState = "";

        public event Action<MonsterAI> OnDied;

        public bool IsDead => _currentHp <= 0f;
        public Transform Transform => transform;
        public MonsterType Type => _type;
        public float CurrentHp => _currentHp;
        public float MaxHp => _maxHp;
        public int Level => _level;
        public bool DropsBossKeyOnDeath => _dropsBossKeyOnDeath;

        public Combat.ProjectileParryType ProjectileParryType
        {
            get
            {
                var stat = Data.GameDataManager.Instance?.MonsterStats?.GetStat(_type);
                if (stat != null && stat.specialSkillIds != null && stat.specialSkillIds.Count > 0)
                {
                    var skill = GetMonsterSkillData(stat.specialSkillIds[0]);
                    if (skill != null) return skill.projectileParryType;
                }
                return _type == MonsterType.Totem_Turret ? Combat.ProjectileParryType.Enhanced : Combat.ProjectileParryType.Normal;
            }
        }

        public float MeleeAttackRange
        {
            get
            {
                var stat = Data.GameDataManager.Instance?.MonsterStats?.GetStat(_type);
                return (stat?.normalAttack != null && stat.normalAttack.attackRange > 0f) ? stat.normalAttack.attackRange : 1.8f;
            }
        }

        public float MeleeAttackCooldown
        {
            get
            {
                var stat = Data.GameDataManager.Instance?.MonsterStats?.GetStat(_type);
                return (stat?.normalAttack != null && stat.normalAttack.attackCooldown > 0f) ? stat.normalAttack.attackCooldown : 1.2f;
            }
        }

        public float MeleeAttackDamage
        {
            get
            {
                var stat = Data.GameDataManager.Instance?.MonsterStats?.GetStat(_type);
                return (stat?.normalAttack != null && stat.normalAttack.attackDamage > 0f) ? stat.normalAttack.attackDamage : _attackDamage;
            }
        }

        private Dungeon.RoomController _cachedRoom;
        public Dungeon.RoomController OwningRoom
        {
            get
            {
                if (_cachedRoom == null && Dungeon.MazeGenerator.Instance != null)
                {
                    Vector2Int roomCoord = Dungeon.MazeGenerator.Instance.GetRoomCoordsAtWorldPos(transform.position);
                    _cachedRoom = Dungeon.MazeGenerator.Instance.GetRoomAt(roomCoord);
                }
                return _cachedRoom;
            }
            set => _cachedRoom = value;
        }

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

            EnsureBehavior();
        }

        private void EnsureBehavior()
        {
            if (_behavior == null || _behavior.Type != _type)
            {
                _behavior = MonsterBehaviorRegistry.Create(_type, this);
            }
        }

        private void OnEnable()
        {
            if (!ActiveMonsters.Contains(this))
            {
                ActiveMonsters.Add(this);
            }
            EnsureBehavior();
            _behavior?.ResetState();

            if (_animator == null) _animator = GetComponentInChildren<Animator>();
            _animParameters = null;
            _currentAnimState = "";
            _currentHp = _maxHp;
            if (_cc != null) _cc.enabled = true;
            var col = GetComponent<Collider>();
            if (col != null) col.enabled = true;
            _knockbackVelocity = Vector3.zero;
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
            MonsterTableAssetPlayer.StopLoopSound(ref _moveLoopSoundInstance);
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

            EnsureBehavior();
            _behavior?.ResetState();

            ApplyPatternStatsFromTable();

            _isAggroed = false;
            _isSweeping = false;
            _stunTimer = 0f;
            _spawnOrigin = transform.position;
            _wanderTarget = transform.position;

            _originalScale = transform.localScale;
            if (_rend != null && _rend.material != null)
            {
                _originalColor = _rend.material.color;
            }

            var stat = Data.GameDataManager.Instance?.MonsterStats?.GetStat(_type);
            if (stat != null)
            {
                if (stat.spawnSoundPrefab != null)
                {
                    MonsterTableAssetPlayer.PlayOneShotSound(stat.spawnSoundPrefab, transform.position);
                }
                if (stat.enableMoveLoopSound && stat.moveLoopSoundPrefab != null)
                {
                    MonsterTableAssetPlayer.StartLoopSound(stat.moveLoopSoundPrefab, true, transform, ref _moveLoopSoundInstance);
                }
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
                _kiteDistance = stat.kiteDistance;
                _knockbackPower = stat.knockbackPower;
                _telegraphDuration = stat.telegraphDuration;
                _specialAttackRadius = stat.specialAttackRadius;
                _specialAttackAngle = stat.specialAttackAngle;
                _postAttackDelay = stat.postAttackDelay;
                _gemDropCount = stat.gemDropCount;
                _expPerGem = stat.expPerGem;
                _heartDropChance = stat.heartDropChance > 0f ? stat.heartDropChance : 0.25f;
                _heartHealAmount = stat.heartHealAmount > 0f ? stat.heartHealAmount : 25.0f;
                _dashDistance = stat.dashDistance > 0f ? stat.dashDistance : 5.0f;
                _dashWidth = stat.dashWidth > 0f ? stat.dashWidth : 0.5f;
                _dashDuration = stat.dashDuration > 0f ? stat.dashDuration : 0.3f;
            }

            EnsureBehavior();
            _behavior?.ApplyPatternStats();
        }

        public Data.MonsterSkillEntry GetMonsterSkillData(string skillId)
        {
            if (Data.GameDataManager.Instance != null && Data.GameDataManager.Instance.MonsterSkills != null)
            {
                return Data.GameDataManager.Instance.MonsterSkills.GetSkill(skillId);
            }
            return null;
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

        public void SetSpawnDelay(float delay)
        {
            _spawnDelayTimer = delay;
            PlayIdleAnimation();
        }

        private void Update()
        {
            if (IsDead) return;
            if (_isDormant) return;

            if (transform.position.y < -5f)
            {
                Die();
                return;
            }

            if (transform.position.y > 0.8f)
            {
                Vector3 rayOrigin = new Vector3(transform.position.x, 8f, transform.position.z);
                if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit groundHit, 16f, _obstacleLayerMask, QueryTriggerInteraction.Ignore))
                {
                    float groundY = groundHit.point.y;
                    if (transform.position.y > groundY + 0.15f)
                    {
                        Vector3 pos = transform.position;
                        pos.y = Mathf.MoveTowards(pos.y, groundY, Time.deltaTime * 20f);
                        transform.position = pos;
                    }
                }
            }

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

            if (_knockbackVelocity.sqrMagnitude > 0.05f)
            {
                _knockbackVelocity = Vector3.Lerp(_knockbackVelocity, Vector3.zero, Time.deltaTime * 6f);
            }

            if (_stunTimer > 0f)
            {
                _stunTimer -= Time.deltaTime;
                PlayIdleAnimation();
                if (_knockbackVelocity.sqrMagnitude > 0.05f)
                {
                    SafeMove((_knockbackVelocity + Vector3.down * 9.81f) * Time.deltaTime);
                }
                return;
            }

            if (_isSweeping)
            {
                if (_knockbackVelocity.sqrMagnitude > 0.05f)
                {
                    SafeMove(_knockbackVelocity * Time.deltaTime);
                }
                return;
            }

            Vector3 toPlayer = _targetPlayer.position - transform.position;
            toPlayer.y = 0f;
            float distToPlayer = toPlayer.magnitude;

            EnsureBehavior();

            // 1. Behavior 커스텀 틱 (예: 사방신 토템 고정 포대)
            if (_behavior != null && _behavior.TickCustomBehavior(distToPlayer, toPlayer))
            {
                return;
            }

            // 2. 1순위 스페셜 스킬 실행
            if (_behavior != null && _behavior.TrySpecialSkill(distToPlayer, toPlayer))
            {
                return;
            }

            // 3. 카이팅 거리 유지 (원거리 몬스터)
            if (_kiteDistance > 0f)
            {
                if (distToPlayer < _kiteDistance)
                {
                    Vector3 backDir = -toPlayer.normalized;
                    SafeMove(backDir * (_moveSpeed * 0.75f) * Time.deltaTime);
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(toPlayer.normalized), Time.deltaTime * 10f);
                    PlayMoveAnimation();
                    return;
                }
                else if (distToPlayer <= _attackRange)
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(toPlayer.normalized), Time.deltaTime * 10f);
                    PlayIdleAnimation();
                    return;
                }
                else
                {
                    NavigateTowardsTarget(distToPlayer);
                    PlayMoveAnimation();
                    return;
                }
            }

            // 4. 근접 평타 접근 및 시전
            float meleeReach = MeleeAttackRange;
            if (distToPlayer <= meleeReach)
            {
                if (!HasLineOfSightToPlayer())
                {
                    NavigateTowardsTarget(distToPlayer);
                    PlayMoveAnimation();
                    return;
                }

                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(toPlayer.normalized), Time.deltaTime * 10f);

                if (Time.time >= _lastMeleeAttackTime + MeleeAttackCooldown)
                {
                    if (_behavior != null)
                    {
                        _behavior.StartMeleeAttack();
                    }
                }
                else
                {
                    PlayIdleAnimation();
                }
            }
            else
            {
                NavigateTowardsTarget(distToPlayer);
                PlayMoveAnimation();
            }
        }

        public bool HasLineOfSightToPlayer()
        {
            if (_targetPlayer == null) return false;

            if (Dungeon.MazeGenerator.Instance != null)
            {
                Vector2Int myRoom = Dungeon.MazeGenerator.Instance.GetRoomCoordsAtWorldPos(transform.position);
                Vector2Int playerRoom = Dungeon.MazeGenerator.Instance.GetRoomCoordsAtWorldPos(_targetPlayer.position);

                if (myRoom != playerRoom)
                {
                    int manhattanDist = Mathf.Abs(myRoom.x - playerRoom.x) + Mathf.Abs(myRoom.y - playerRoom.y);
                    if (manhattanDist != 1) return false;

                    Dungeon.Direction dirToPlayer = Dungeon.MazeGrid.GetOffsetDirection(playerRoom - myRoom);
                    var currentCell = Dungeon.MazeGenerator.Instance.Grid?.GetCell(myRoom);
                    if (currentCell == null || !currentCell.IsOpen(dirToPlayer)) return false;

                    if (Dungeon.MazeGenerator.Instance.InstantiatedRooms.TryGetValue(myRoom, out var myRoomCtrl) &&
                        Dungeon.MazeGenerator.Instance.InstantiatedRooms.TryGetValue(playerRoom, out var playerRoomCtrl))
                    {
                        var mySocket = myRoomCtrl.GetSocket(dirToPlayer);
                        var oppSocket = playerRoomCtrl.GetSocket(Dungeon.MazeGrid.GetOpposite(dirToPlayer));
                        if (mySocket != null && oppSocket != null)
                        {
                            if (mySocket.CurrentState == Dungeon.DoorState.CombatLock || mySocket.CurrentState == Dungeon.DoorState.BossLock ||
                                oppSocket.CurrentState == Dungeon.DoorState.CombatLock || oppSocket.CurrentState == Dungeon.DoorState.BossLock)
                            {
                                return false;
                            }
                        }

                        var autoDoor = mySocket?.AutoDoor ?? oppSocket?.AutoDoor;
                        if (autoDoor != null && !autoDoor.IsOpen) return false;
                    }
                }
            }

            Vector3 eye = transform.position + Vector3.up * 1.0f;
            Vector3 playerEye = _targetPlayer.position + Vector3.up * 1.0f;
            Vector3 diff = playerEye - eye;
            float dist = diff.magnitude;

            if (dist > 35f) return false;
            if (dist < 0.3f) return true;

            RaycastHit[] hits = Physics.RaycastAll(eye, diff.normalized, dist, _obstacleLayerMask, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (var hit in hits)
            {
                if (hit.collider == null || hit.collider.gameObject == gameObject) continue;
                if (hit.collider.CompareTag("Monster") || hit.collider.GetComponentInParent<MonsterAI>() != null) continue;

                if (hit.collider.CompareTag("Player") || hit.collider.GetComponentInParent<PlayerController>() != null || hit.collider.gameObject == _targetPlayer.gameObject)
                {
                    return true;
                }

                if (hit.collider.GetComponentInParent<Dungeon.DungeonAutoDoor>() != null)
                {
                    var ad = hit.collider.GetComponentInParent<Dungeon.DungeonAutoDoor>();
                    if (ad.IsOpen) continue;
                }

                return false;
            }

            return true;
        }

        private bool CheckPlayerInSight()
        {
            if (_targetPlayer == null) return false;
            if (Dungeon.MazeGenerator.Instance == null) return HasLineOfSightToPlayer();

            Vector2Int myRoom = Dungeon.MazeGenerator.Instance.GetRoomCoordsAtWorldPos(transform.position);
            Vector2Int playerRoom = Dungeon.MazeGenerator.Instance.GetRoomCoordsAtWorldPos(_targetPlayer.position);

            if (myRoom == playerRoom)
            {
                return HasLineOfSightToPlayer();
            }

            int manhattanDist = Mathf.Abs(myRoom.x - playerRoom.x) + Mathf.Abs(myRoom.y - playerRoom.y);
            if (manhattanDist == 1)
            {
                return HasLineOfSightToPlayer();
            }

            return false;
        }

        public void TriggerAggro()
        {
            if (_isAggroed) return;
            _isAggroed = true;

            PlayerController.Instance?.SetInCombat(true);

            if (OwningRoom != null && !OwningRoom.IsEncounterActive && !OwningRoom.IsCleared && OwningRoom.RoomType != Dungeon.RoomType.Start)
            {
                OwningRoom.StartCombatEncounter();
            }

            _behavior?.OnAggroTriggered();
            AlertNearbyMonstersInSameRoom();
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
                SafeMove(move * Time.deltaTime);
                PlayMoveAnimation();
            }
            else
            {
                PlayIdleAnimation();
            }
        }

        public void NavigateTowardsTarget(float distToPlayer)
        {
            if (Time.time >= _nextPathUpdateTime)
            {
                _nextPathUpdateTime = Time.time + 0.2f;
                UpdateTargetWaypoint();
            }

            Vector3 desiredDir;
            if (_currentTargetWaypoint != Vector3.zero)
            {
                Vector3 toWp = _currentTargetWaypoint - transform.position;
                toWp.y = 0f;
                desiredDir = toWp.sqrMagnitude > 0.01f ? toWp.normalized : transform.forward;
            }
            else
            {
                Vector3 toP = _targetPlayer.position - transform.position;
                toP.y = 0f;
                desiredDir = toP.sqrMagnitude > 0.01f ? toP.normalized : transform.forward;
            }

            Vector3 steerDir = ComputeObstacleAvoidanceSteering(desiredDir);
            Vector3 separation = ComputeSeparationVector();
            Vector3 finalMoveDir = (steerDir + separation * 0.7f).normalized;

            if (finalMoveDir.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(finalMoveDir), Time.deltaTime * 8f);
            }

            Vector3 move = finalMoveDir * _moveSpeed;
            move += _knockbackVelocity;
            move.y = -9.81f;

            SafeMove(move * Time.deltaTime);

            PlayMoveAnimation();
        }

        public void SafeMove(Vector3 motion)
        {
            if (_cc != null && _cc.enabled && _cc.gameObject.activeInHierarchy)
            {
                _cc.Move(motion);
            }
            else
            {
                transform.position += motion;
            }
        }

        private void UpdateTargetWaypoint()
        {
            if (MazeGenerator.Instance == null || _targetPlayer == null)
            {
                _currentTargetWaypoint = _targetPlayer != null ? _targetPlayer.position : Vector3.zero;
                return;
            }

            Vector2Int myRoom = MazeGenerator.Instance.GetRoomCoordsAtWorldPos(transform.position);
            Vector2Int playerRoom = MazeGenerator.Instance.GetRoomCoordsAtWorldPos(_targetPlayer.position);

            if (myRoom == playerRoom)
            {
                Vector3 eye = transform.position + Vector3.up * 1.0f;
                Vector3 playerEye = _targetPlayer.position + Vector3.up * 1.0f;
                Vector3 diff = playerEye - eye;

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

            _currentTargetWaypoint = MazeGenerator.Instance.GetNextWaypointTowards(myRoom, playerRoom);
        }

        private Vector3 ComputeObstacleAvoidanceSteering(Vector3 desiredDir)
        {
            desiredDir.y = 0f;
            if (desiredDir.sqrMagnitude < 0.001f) return transform.forward;
            desiredDir.Normalize();

            Vector3 rayStart = transform.position + Vector3.up * 0.9f;
            float checkDist = 1.8f;

            if (Physics.SphereCast(rayStart, 0.4f, desiredDir, out RaycastHit hit, checkDist, _obstacleLayerMask, QueryTriggerInteraction.Ignore))
            {
                bool isTargetPlayer = _targetPlayer != null && (hit.collider.gameObject == _targetPlayer.gameObject || hit.collider.GetComponentInParent<PlayerController>() != null);
                bool isOtherMonster = hit.collider.gameObject != gameObject && (hit.collider.CompareTag("Monster") || hit.collider.GetComponentInParent<MonsterAI>() != null);

                if (!isTargetPlayer && !isOtherMonster)
                {
                    float[] angles = { 35f, -35f, 65f, -65f, 90f, -90f, 120f, -120f };
                    Vector3 bestAltDir = Vector3.zero;
                    float maxClearance = -1f;

                    foreach (float angle in angles)
                    {
                        Vector3 testDir = Quaternion.Euler(0f, angle, 0f) * desiredDir;
                        if (!Physics.SphereCast(rayStart, 0.35f, testDir, out RaycastHit testHit, checkDist, _obstacleLayerMask, QueryTriggerInteraction.Ignore))
                        {
                            return testDir.normalized;
                        }
                        else
                        {
                            bool testIsPlayerOrMonster = (_targetPlayer != null && (testHit.collider.gameObject == _targetPlayer.gameObject || testHit.collider.GetComponentInParent<PlayerController>() != null))
                                || (testHit.collider.gameObject != gameObject && (testHit.collider.CompareTag("Monster") || testHit.collider.GetComponentInParent<MonsterAI>() != null));

                            if (testIsPlayerOrMonster)
                            {
                                return testDir.normalized;
                            }
                            else if (testHit.distance > maxClearance)
                            {
                                maxClearance = testHit.distance;
                                bestAltDir = testDir;
                            }
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
            float effectiveRadius = Mathf.Max(_separationRadius, 2.2f);
            Collider[] neighbors = Physics.OverlapSphere(transform.position, effectiveRadius);

            for (int i = 0; i < neighbors.Length; i++)
            {
                var n = neighbors[i];
                if (n.gameObject == gameObject) continue;
                if (n.CompareTag("Monster") || n.GetComponentInParent<MonsterAI>() != null)
                {
                    Vector3 diff = transform.position - n.transform.position;
                    diff.y = 0f;
                    float mag = diff.magnitude;
                    if (mag > 0.001f && mag < effectiveRadius)
                    {
                        float weight = 1f - (mag / effectiveRadius);
                        separation += diff.normalized * (weight * weight);
                    }
                }
            }

            return separation;
        }

        public void TryAttackPlayer()
        {
            if (_isSweeping) return;
            if (Time.time < _lastAttackTime + _attackCooldown) return;

            EnsureBehavior();
            if (_behavior != null && _behavior.RequiresLineOfSight && !HasLineOfSightToPlayer()) return;

            _behavior?.PerformAttack();
        }

        public void CleanupTelegraph()
        {
            if (_activeTelegraph != null)
            {
                Destroy(_activeTelegraph);
                _activeTelegraph = null;
            }
        }

        public static GameObject CreateFloorGuideBox(Vector3 startPos, Vector3 dir, float length, float width, Color color)
        {
            return MonsterTelegraphs.CreateFloorGuideBox(startPos, dir, length, width, color);
        }

        public bool HasClearChargePathToPlayer()
        {
            if (_behavior is BigMonBehavior bmb)
            {
                return bmb.HasClearChargePathToPlayer();
            }
            return HasLineOfSightToPlayer();
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            _behavior?.OnControllerColliderHit(hit);
        }

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
                SafeMove(delta);
                yield return null;
            }
            _pullCoroutine = null;
        }

        public void TakeDamage(DamageInfo info)
        {
            if (IsDead) return;

            EnsureBehavior();
            if (_behavior != null && _behavior.PreDamage(info) == DamageResponse.Absorbed)
            {
                return;
            }

            TriggerAggro();

            float damageToApply = info.amount;

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

            MythOfGuotaji.UI.DamageCounterManager.ShowDamage(damageToApply, info, transform.position);

            PlayHitAnimation();
            var stat = Data.GameDataManager.Instance?.MonsterStats?.GetStat(_type);
            if (stat != null)
            {
                if (stat.hitVfxPrefab != null)
                {
                    VFXManager.Instance?.Spawn(stat.hitVfxPrefab, transform.position + Vector3.up * 0.8f, Quaternion.identity);
                }
                if (stat.hitSoundPrefab != null)
                {
                    MonsterTableAssetPlayer.PlayOneShotSound(stat.hitSoundPrefab, transform.position);
                }
            }
            StopCoroutine(nameof(HitFeedbackRoutine));
            StartCoroutine(HitFeedbackRoutine());

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

        public void SetRendererColor(Color c)
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

        public void Die()
        {
            CleanupTelegraph();

            var stat = Data.GameDataManager.Instance?.MonsterStats?.GetStat(_type);
            if (stat != null)
            {
                if (stat.deathVfxPrefab != null)
                {
                    VFXManager.Instance?.Spawn(stat.deathVfxPrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity, scale: 1.2f, autoDestroyTime: 2.0f);
                }
                if (stat.deathSoundPrefab != null)
                {
                    MonsterTableAssetPlayer.PlayOneShotSound(stat.deathSoundPrefab, transform.position);
                }
            }
            MonsterTableAssetPlayer.StopLoopSound(ref _moveLoopSoundInstance);

            ActiveMonsters.Remove(this);
            UI.MonsterHealthBarManager.Unregister(this);
            OnDied?.Invoke(this);

            if (_cc != null) _cc.enabled = false;
            var col = GetComponent<Collider>();
            if (col != null) col.enabled = false;

            PlayDeathAnimation();
            SpawnGemsOnDeath();
            TrySpawnHeartOnDeath();

            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddKill();

                if (_dropsBossKeyOnDeath)
                {
                    GameManager.Instance.AcquireBossKey();
                }

                if (stat != null && stat.specialDrops != null)
                {
                    foreach (var sDrop in stat.specialDrops)
                    {
                        if (sDrop != null && UnityEngine.Random.value <= sDrop.dropChance)
                        {
                            if (sDrop.itemId == "BossKey" || sDrop.itemId == "boss_key")
                            {
                                GameManager.Instance.AcquireBossKey();
                            }
                        }
                    }
                }

                EnsureBehavior();
                bool handledByBehavior = _behavior != null && _behavior.HandleDeathProgression();

                if (!handledByBehavior && _dropsBossKeyOnDeath)
                {
                    if (OwningRoom != null)
                    {
                        OwningRoom.OnKeyEliteDefeated();
                    }
                    else if (Dungeon.MazeGenerator.Instance != null)
                    {
                        foreach (var room in Dungeon.MazeGenerator.Instance.InstantiatedRooms.Values)
                        {
                            if (room != null && room.RoomType == Dungeon.RoomType.KeyRoom)
                            {
                                room.OnKeyEliteDefeated();
                                break;
                            }
                        }
                    }
                }
            }

            if ((_behavior == null || _behavior.ShakesCameraOnDeath) && Cameras.QuarterViewCamera.Instance != null)
            {
                Cameras.QuarterViewCamera.Instance.TriggerShake(0.35f, 0.3f);
            }

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
            EnsureBehavior();
            if ((_isSweeping && (_behavior == null || !_behavior.AllowMoveAnimationWhileActing)) || IsDead) return;

            string[] states = _behavior != null ? _behavior.MoveAnimationStates : MonsterBehavior.DefaultMoveAnimationStates;
            for (int i = 0; i < states.Length; i++)
            {
                if (HasAnimState(states[i]))
                {
                    PlayAnimation(states[i]);
                    return;
                }
            }
            SetTriggerSafe("walk");
        }

        public void PlayIdleAnimation()
        {
            if (_isSweeping || IsDead) return;
            if (HasAnimState("Idle")) PlayAnimation("Idle");
            else if (HasAnimState("IdleBreathe")) PlayAnimation("IdleBreathe");
            else if (HasAnimState("IdleLookAround")) PlayAnimation("IdleLookAround");
            else SetTriggerSafe("idle");
        }

        public void PlayAttackAnimation()
        {
            if (IsDead) return;
            EnsureBehavior();
            if (_behavior != null)
            {
                _behavior.PlayAttackMotion();
            }
            else
            {
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
            else SetTriggerSafe("stun");
        }

        public void PlayDeathAnimation()
        {
            if (HasAnimState("Death")) PlayAnimation("Death", 0.05f);
            else SetTriggerSafe("dead");
        }

        private HashSet<string> _animParameters;

        private void CacheAnimParameters()
        {
            if (_animator == null) return;
            _animParameters = new HashSet<string>();
            foreach (var p in _animator.parameters)
            {
                _animParameters.Add(p.name);
            }
        }

        private bool HasParameter(string paramName)
        {
            if (_animator == null || string.IsNullOrEmpty(paramName)) return false;
            if (_animParameters == null) CacheAnimParameters();
            return _animParameters != null && _animParameters.Contains(paramName);
        }

        public void SetTriggerSafe(string paramName)
        {
            if (!HasParameter(paramName)) return;
            _animator.SetTrigger(paramName);
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
