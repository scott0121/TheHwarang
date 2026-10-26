using System.Collections;
using System.Collections.Generic;
using MythOfGuotaji.Combat;
using MythOfGuotaji.Core;
using MythOfGuotaji.Progression;
using MythOfGuotaji.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MythOfGuotaji.Actor
{
    public enum CombatCategory
    {
        Bow,
        Dagger,
        Magic
    }

    /// <summary>
    /// 거타지(플레이어) 핵심 제어기
    /// - 기본 화살 공격: 마우스 방향 자동 공격 (지속 연사)
    /// - 좌클릭 0.8초 이상 홀드: 차징 바 표시 시작 (최대 1.5초 충전)
    /// - 1.5초 충전 후 릴리즈: 2배 피해, 25m 무제한 관통, 1.5배속, 몬스터 넉백 차징샷 발사
    /// - 우클릭: 단검 위기 탈출 부채꼴 베기
    /// - Space / Shift: 무적 회피 대시
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour, IDamageable
    {
        public static PlayerController Instance { get; private set; }

        [Header("Movement Settings")]
        [SerializeField] private float _moveSpeed = 7.5f;
        [SerializeField] private float _dashSpeed = 16f;
        [SerializeField] private float _dashDuration = 0.22f;
        [SerializeField] private float _dashCooldown = 2.0f; // 슬라이딩 쿨타임 2초
        [SerializeField] private float _dashInvincibleDuration = 0.5f; // 슬라이딩 중 0.5초간 데미지 완전 무효화 (i-frame)

        [Header("Health & Stats")]
        [SerializeField] private float _maxHp = 100f;
        private float _currentHp;

        [Header("Weapons")]
        [SerializeField] private BowWeapon _bow;
        [SerializeField] private DaggerWeapon _dagger;
        [SerializeField] private Transform _visualRoot;
        [SerializeField] private Animator _animator;

        [Header("Charge & Attack Settings")]
        [SerializeField] private float _minChargeTime = 0.3f;
        [SerializeField] private float _maxChargeTime = 3.0f;
        [SerializeField] private float _sameCategoryGCD = 0.5f; // 동일 무기 카테고리 0.5초
        [SerializeField] private float _diffCategoryGCD = 0.2f; // 다른 무기/마법 카테고리 0.2초

        private CharacterController _cc;
        private Camera _cam;
        private Vector3 _moveInput;
        private Vector3 _aimDirection = Vector3.forward;

        private bool _isDashing = false;
        private float _dashTimer = 0f;
        private float _lastDashTime = -999f;
        private bool _isInvulnerable = false;
        private float _invincibleTimer = 0f;

        private bool _isCharging = false;
        private float _chargeStartTime = 0f;
        private bool _hasFullCharged = false;
        private GameObject _chargingAuraInstance;
        private int _currentChargeStep = 0;
        private bool _isInputLocked = false;
        private float _bowCooldownEndTime = 0f;
        private float _daggerCooldownEndTime = 0f;
        private float _magicCooldownEndTime = 0f;
        private Vector3 _knockbackVelocity = Vector3.zero;

        private static readonly int AnimHashMoveSpeed = Animator.StringToHash("MoveSpeed");
        private static readonly int AnimHashJump = Animator.StringToHash("Jump");
        private static readonly int AnimHashIsInteracting = Animator.StringToHash("IsInteracting");
        private static readonly int AnimHashDamage = Animator.StringToHash("Damage");
        private static readonly int AnimHashDeath = Animator.StringToHash("Death");
        private static readonly int AnimHashRespawn = Animator.StringToHash("Respawn");
        private Coroutine _interactionAnimRoutine;

        public float CurrentHp => _currentHp;
        public float MaxHp => _maxHp;
        public bool IsDead => _currentHp <= 0f;
        public bool IsDashing => _isDashing;
        public bool IsInCombat => _isInCombat;
        private bool _isInCombat = false;

        public void SetInCombat(bool inCombat)
        {
            if (_isInCombat == inCombat) return;
            _isInCombat = inCombat;
            Debug.Log($"<color={(inCombat ? "red" : "lime")}>[PlayerController] 전투 상태 변경: {(inCombat ? "⚔️ 전투 중 (In Combat)" : "🕊️ 비전투 중 (Out of Combat)")}</color>");
        }

        public Transform Transform => transform;
        public Vector3 AimDirection => _aimDirection;
        public bool IsCharging => _isCharging;
        public float ChargeHoldDuration => _isCharging ? (Time.time - _chargeStartTime) : 0f;
        public float MinChargeTime => _minChargeTime;
        public float MaxChargeTime => _maxChargeTime;

        // 0.3초 미만은 0%, 0.3초부터 3.0초까지 0.0 ~ 1.0 선형 증가
        public float CurrentChargeRatio
        {
            get
            {
                if (!_isCharging || ChargeHoldDuration < _minChargeTime) return 0f;
                float effectiveDuration = ChargeHoldDuration - _minChargeTime;
                float totalChargeSpan = Mathf.Max(0.01f, _maxChargeTime - _minChargeTime);
                return Mathf.Clamp01(effectiveDuration / totalChargeSpan);
            }
        }

        // 0.3초 0m ~ 3.0초 25m 선형 증가 사거리
        public float CurrentChargedRange => Mathf.Lerp(_bow != null ? _bow.ChargeMinRange : 0f, _bow != null ? _bow.ChargeMaxRange : 25f, CurrentChargeRatio);

        public bool IsInputLocked => _isInputLocked;
        public bool IsOnGCD => !CanUseCategory(CombatCategory.Bow);
        public float GCDRemaining => Mathf.Max(0f, _bowCooldownEndTime - Time.time);

        public bool CanUseCategory(CombatCategory cat)
        {
            return cat switch
            {
                CombatCategory.Bow => Time.time >= _bowCooldownEndTime,
                CombatCategory.Dagger => Time.time >= _daggerCooldownEndTime,
                CombatCategory.Magic => Time.time >= _magicCooldownEndTime,
                _ => true
            };
        }

        public void TriggerCategoryGCD(CombatCategory cat)
        {
            float now = Time.time;
            switch (cat)
            {
                case CombatCategory.Bow:
                    _bowCooldownEndTime = now + _sameCategoryGCD;
                    _daggerCooldownEndTime = Mathf.Max(_daggerCooldownEndTime, now + _diffCategoryGCD);
                    _magicCooldownEndTime = Mathf.Max(_magicCooldownEndTime, now + _diffCategoryGCD);
                    break;
                case CombatCategory.Dagger:
                    _daggerCooldownEndTime = now + _sameCategoryGCD;
                    _bowCooldownEndTime = Mathf.Max(_bowCooldownEndTime, now + _diffCategoryGCD);
                    _magicCooldownEndTime = Mathf.Max(_magicCooldownEndTime, now + _diffCategoryGCD);
                    break;
                case CombatCategory.Magic:
                    _magicCooldownEndTime = now + _sameCategoryGCD;
                    _bowCooldownEndTime = Mathf.Max(_bowCooldownEndTime, now + _diffCategoryGCD);
                    _daggerCooldownEndTime = Mathf.Max(_daggerCooldownEndTime, now + _diffCategoryGCD);
                    break;
            }
        }

        public void TriggerGCD()
        {
            TriggerCategoryGCD(CombatCategory.Bow);
        }

        public void SetInputLock(bool locked)
        {
            _isInputLocked = locked;
            if (locked)
            {
                _moveInput = Vector3.zero;
                _isCharging = false;
                _hasFullCharged = false;
            }
        }

        private void Awake()
        {
            Instance = this;
            _cc = GetComponent<CharacterController>();
            _cam = Camera.main;

            ApplyStatsFromTable();
            _currentHp = _maxHp;

            if (_bow == null) _bow = GetComponentInChildren<BowWeapon>();
            if (_dagger == null) _dagger = GetComponentInChildren<DaggerWeapon>();

            if (_animator == null) _animator = GetComponentInChildren<Animator>();
            if (_visualRoot == null)
            {
                var cat = transform.Find("CatPlayer_Job00");
                if (cat != null) _visualRoot = cat;
                else if (_animator != null) _visualRoot = _animator.transform;
            }

            if (GetComponent<ChargeIndicatorUI>() == null)
            {
                gameObject.AddComponent<ChargeIndicatorUI>();
            }
        }

        public void ApplyStatsFromTable()
        {
            if (Data.GameDataManager.Instance != null && Data.GameDataManager.Instance.PlayerStats != null)
            {
                var table = Data.GameDataManager.Instance.PlayerStats;
                _maxHp = table.maxHp;
                _moveSpeed = table.moveSpeed;
                _dashSpeed = table.dashSpeed;
                _dashDuration = table.dashDuration;
                _dashCooldown = table.dashCooldown;
                _dashInvincibleDuration = table.dashInvincibleDuration > 0f ? table.dashInvincibleDuration : 0.5f;
                _sameCategoryGCD = table.sameCategoryGCD;
                _diffCategoryGCD = table.diffCategoryGCD;
            }

            if (_bow != null)
            {
                _minChargeTime = _bow.ChargeStartTime;
                _maxChargeTime = _bow.MaxChargeTime;
            }
        }

        private void Update()
        {
            if (IsDead) return;
            if (Time.timeScale <= 0f || BoonSelectionUI.IsShowing) return; // UI 표시 및 일시정지 중 마우스/조작 입력 및 회전 일체 차단

            if (_isInputLocked)
            {
                HandleAiming();
                HandleMovement();
                return;
            }

            HandleInput();
            HandleAiming();
            HandleMovement();
            HandleCombatActions();
        }

        private void HandleInput()
        {
            float h = 0f;
            float v = 0f;

            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) v += 1f;
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) v -= 1f;
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) h -= 1f;
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) h += 1f;
            }

            var gamepad = Gamepad.current;
            if (gamepad != null && Mathf.Approximately(h, 0f) && Mathf.Approximately(v, 0f))
            {
                Vector2 stick = gamepad.leftStick.ReadValue();
                h = stick.x;
                v = stick.y;
            }

            _moveInput = new Vector3(h, 0f, v).normalized;

            // 대시 입력
            bool dashTriggered = (keyboard != null && (keyboard.spaceKey.wasPressedThisFrame || keyboard.leftShiftKey.wasPressedThisFrame))
                                 || (gamepad != null && (gamepad.buttonSouth.wasPressedThisFrame || gamepad.rightShoulder.wasPressedThisFrame));

            if (dashTriggered && Time.time >= _lastDashTime + _dashCooldown && !_isDashing)
            {
                StartDash();
            }
        }

        /// <summary>
        /// 마우스 커서 평면 좌표를 기반으로 실시간 조준 회전 (롤백된 조작계)
        /// </summary>
        private void HandleAiming()
        {
            if (Time.timeScale <= 0f || BoonSelectionUI.IsShowing) return;
            if (_cam == null) _cam = Camera.main;
            if (_cam == null) return;

            var mouse = Mouse.current;
            if (mouse != null)
            {
                Vector2 mouseScreen = mouse.position.ReadValue();
                Ray ray = _cam.ScreenPointToRay(mouseScreen);
                Plane plane = new Plane(Vector3.up, transform.position);

                if (plane.Raycast(ray, out float enter))
                {
                    Vector3 worldHit = ray.GetPoint(enter);
                    Vector3 lookDir = worldHit - transform.position;
                    lookDir.y = 0f;

                    if (lookDir.sqrMagnitude > 0.01f)
                    {
                        _aimDirection = lookDir.normalized;
                        if (_visualRoot != null)
                        {
                            _visualRoot.rotation = Quaternion.LookRotation(_aimDirection, Vector3.up);
                        }
                        else
                        {
                            transform.rotation = Quaternion.LookRotation(_aimDirection, Vector3.up);
                        }
                    }
                }
            }
            else
            {
                var gamepad = Gamepad.current;
                if (gamepad != null)
                {
                    Vector2 rightStick = gamepad.rightStick.ReadValue();
                    if (rightStick.sqrMagnitude > 0.15f)
                    {
                        _aimDirection = new Vector3(rightStick.x, 0f, rightStick.y).normalized;
                        if (_visualRoot != null) _visualRoot.rotation = Quaternion.LookRotation(_aimDirection, Vector3.up);
                        else transform.rotation = Quaternion.LookRotation(_aimDirection, Vector3.up);
                    }
                }
            }
        }

        private void HandleMovement()
        {
            Vector3 finalMove = Vector3.zero;

            // 슬라이딩 무적(0.5초 데미지 무효화 i-frame) 타이머 관리
            if (_invincibleTimer > 0f)
            {
                _invincibleTimer -= Time.deltaTime;
                if (_invincibleTimer <= 0f)
                {
                    _isInvulnerable = false;
                }
            }

            if (_isDashing)
            {
                _dashTimer -= Time.deltaTime;
                Vector3 dashDir = _moveInput.sqrMagnitude > 0.01f ? _moveInput : _aimDirection;
                finalMove = dashDir * _dashSpeed;

                if (_dashTimer <= 0f)
                {
                    _isDashing = false;
                }
            }
            else
            {
                finalMove = _moveInput * _moveSpeed;
            }

            // 넉백 벡터 결합 및 감쇄
            if (_knockbackVelocity.sqrMagnitude > 0.05f)
            {
                finalMove += _knockbackVelocity;
                _knockbackVelocity = Vector3.Lerp(_knockbackVelocity, Vector3.zero, Time.deltaTime * 7f);
            }

            // 중력 적용
            finalMove.y = -9.81f;
            _cc.Move(finalMove * Time.deltaTime);

            // 이동 애니메이션 파라미터(MoveSpeed) 갱신
            if (_animator != null)
            {
                float currentSpeed = _isDashing 
                    ? _dashSpeed 
                    : (_moveInput.sqrMagnitude > 0.01f ? _moveSpeed : 0f);
                _animator.SetFloat(AnimHashMoveSpeed, currentSpeed);
            }
        }

        private void StartDash()
        {
            _isDashing = true;
            _dashTimer = _dashDuration;
            _lastDashTime = Time.time;

            // [기획 규칙]: 슬라이딩 발동 시 0.5초간 데미지 완전 무효화
            _invincibleTimer = _dashInvincibleDuration > 0f ? _dashInvincibleDuration : 0.5f;
            _isInvulnerable = true;
            _knockbackVelocity = Vector3.zero; // 대시로 넉백 캔슬 탈출 가능

            Vector3 dashDir = _moveInput.sqrMagnitude > 0.01f ? _moveInput.normalized : _aimDirection;
            Core.VFXManager.Instance?.SpawnPlayerDashDust(transform.position, dashDir);

            SetAnimatorTriggerSafe(AnimHashJump);

            if (_bow != null && _bow.IsBursting)
            {
                _bow.StopBurst();
            }
        }

        private void HandleCombatActions()
        {
            var mouse = Mouse.current;
            var gamepad = Gamepad.current;

            bool leftPressedThisFrame = (mouse != null && mouse.leftButton.wasPressedThisFrame)
                                        || (gamepad != null && gamepad.rightTrigger.wasPressedThisFrame);

            bool leftIsPressed = (mouse != null && mouse.leftButton.isPressed)
                                 || (gamepad != null && gamepad.rightTrigger.isPressed);

            bool leftReleasedThisFrame = (mouse != null && mouse.leftButton.wasReleasedThisFrame)
                                         || (gamepad != null && gamepad.rightTrigger.wasReleasedThisFrame);

            bool rightClick = (mouse != null && (mouse.rightButton.wasPressedThisFrame || mouse.rightButton.isPressed))
                              || (gamepad != null && (gamepad.leftTrigger.wasPressedThisFrame || gamepad.leftTrigger.isPressed));

            // 1. 좌클릭 누름 (Bow 카테고리 쿨다운 확인 - 활 발사 후 0.5초, 단검 후 0.2초)
            if (CanUseCategory(CombatCategory.Bow) && (leftPressedThisFrame || (leftIsPressed && !_isCharging)) && _bow != null)
            {
                _isCharging = true;
                _chargeStartTime = Time.time;
                _hasFullCharged = false;
                _currentChargeStep = 0;
                _bow.StopBurst(); // 연사 도중 차징 진입 시 기존 연사를 부드럽게 전환
            }

            if (_isCharging && _bow != null)
            {
                float holdDuration = Time.time - _chargeStartTime;

                if (holdDuration >= _maxChargeTime)
                {
                    _hasFullCharged = true;
                }

                // 차징 0.3초 이상부터 단계별 VFX 오라 동적 부착
                if (holdDuration >= _minChargeTime)
                {
                    float ratio = CurrentChargeRatio;
                    int step = ratio < 0.5f ? 1 : (ratio < 0.85f ? 2 : 3);
                    if (_currentChargeStep != step)
                    {
                        _currentChargeStep = step;
                        if (_chargingAuraInstance != null) Destroy(_chargingAuraInstance);
                        _chargingAuraInstance = Core.VFXManager.Instance?.SpawnBowChargingAura(transform, step);
                    }
                }

                // 마우스 버튼에서 손가락을 뗐을 때 (Release) 발사!
                if (leftReleasedThisFrame)
                {
                    if (_chargingAuraInstance != null)
                    {
                        Destroy(_chargingAuraInstance);
                        _chargingAuraInstance = null;
                    }

                    if (holdDuration >= _minChargeTime)
                    {
                        // 0.3초 이상 차징 후 릴리즈: 사거리 선형 증가(0m~25m) 차징샷 발사 (2배 피해, 무제한 관통, 1.5배속, 강력 넉백)
                        float range = CurrentChargedRange;
                        _bow.FireCharged(_aimDirection, gameObject, range);
                        Core.VFXManager.Instance?.SpawnBowChargeReleaseBurst(transform.position, _aimDirection);
                        TriggerAttackAnimation();
                    }
                    else
                    {
                        // 0.3초 미만 퀵 클릭: 1.5초간 5회 연사 발사
                        _bow.FireBurst5(() => _aimDirection, gameObject);
                        TriggerAttackAnimation();
                    }

                    // 같은 카테고리 0.5초, 다른 카테고리 0.2초 글로벌 쿨다운 적용
                    TriggerCategoryGCD(CombatCategory.Bow);

                    _isCharging = false;
                    _hasFullCharged = false;
                    _currentChargeStep = 0;
                }
            }
            else if (leftReleasedThisFrame)
            {
                if (_chargingAuraInstance != null)
                {
                    Destroy(_chargingAuraInstance);
                    _chargingAuraInstance = null;
                }
                _isCharging = false;
                _hasFullCharged = false;
                _currentChargeStep = 0;
            }

            // 2. 우클릭: 단검 부채꼴 베기 (Dagger 카테고리 쿨다운 확인 - 활 발사 후 0.2초 뒤 바로 사용 가능!)
            if (rightClick && CanUseCategory(CombatCategory.Dagger) && _dagger != null)
            {
                if (_dagger.TrySlash(_aimDirection, gameObject))
                {
                    if (_bow != null) _bow.StopBurst();
                    if (_chargingAuraInstance != null)
                    {
                        Destroy(_chargingAuraInstance);
                        _chargingAuraInstance = null;
                    }
                    _isCharging = false;
                    _hasFullCharged = false;
                    _currentChargeStep = 0;
                    TriggerCategoryGCD(CombatCategory.Dagger);
                    TriggerAttackAnimation();
                }
            }
        }

        /// <summary>
        /// 애니메이터 Upper Layer의 Attack1 0 스테이트 및 IsInteracting 파라미터 연동
        /// </summary>
        public void TriggerAttackAnimation()
        {
            if (_animator == null) return;

            SetAnimatorBoolSafe(AnimHashIsInteracting, true);

            int upperLayerIndex = _animator.GetLayerIndex("Upper Layer");
            if (upperLayerIndex >= 0)
            {
                _animator.CrossFadeInFixedTime("Attack1 0", 0.05f, upperLayerIndex);
            }
            else
            {
                _animator.CrossFadeInFixedTime("Attack1 0", 0.05f);
            }

            if (_interactionAnimRoutine != null)
            {
                StopCoroutine(_interactionAnimRoutine);
            }
            _interactionAnimRoutine = StartCoroutine(ResetInteractingRoutine(0.25f));
        }

        private HashSet<int> _animParamHashes;

        private bool HasParameter(int hash)
        {
            if (_animator == null) return false;
            if (_animParamHashes == null)
            {
                _animParamHashes = new HashSet<int>();
                foreach (var p in _animator.parameters)
                {
                    _animParamHashes.Add(p.nameHash);
                }
            }
            return _animParamHashes.Contains(hash);
        }

        private void SetAnimatorBoolSafe(int hash, bool value)
        {
            if (!HasParameter(hash)) return;
            _animator.SetBool(hash, value);
        }

        private void SetAnimatorTriggerSafe(int hash)
        {
            if (!HasParameter(hash)) return;
            _animator.SetTrigger(hash);
        }

        private void ResetAnimatorTriggerSafe(int hash)
        {
            if (!HasParameter(hash)) return;
            _animator.ResetTrigger(hash);
        }

        private System.Collections.IEnumerator ResetInteractingRoutine(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (_animator != null)
            {
                SetAnimatorBoolSafe(AnimHashIsInteracting, false);
            }
            _interactionAnimRoutine = null;
        }

        public void TakeDamage(DamageInfo info)
        {
            if (IsDead) return;

            // [기획 규칙]: 슬라이딩 무적 (i-frame) 동안 데미지 완전 무효화
            if (_isInvulnerable)
            {
                Debug.Log("<color=cyan><b>[회피 성공!]</b> 슬라이딩 무적(0.5초 i-frame)으로 공격을 흘려보냈습니다!</color>");
                Core.VFXManager.Instance?.Spawn("HCFX_Shine_01", transform.position + Vector3.up * 0.8f, Quaternion.identity, scale: 0.9f, autoDestroyTime: 0.6f);
                return;
            }

            _currentHp -= info.amount;
            Debug.Log($"<color=red>[거타지 피격!] 남은 체력: {_currentHp:F0}/{_maxHp}</color>");

            // 화면 주변 붉은 피격 비네트 플래시 발동
            UI.ScreenDamageVignetteUI.Instance?.TriggerDamageFlash(0.85f);

            // 카메라 피격 셰이크
            if (Cameras.QuarterViewCamera.Instance != null)
            {
                Cameras.QuarterViewCamera.Instance.TriggerShake(0.35f, 0.20f);
            }

            SetAnimatorTriggerSafe(AnimHashDamage);

            // 넉백 적용 (예: 거북이 휩쓸기 등)
            if (info.knockbackForce > 0.1f)
            {
                _knockbackVelocity = info.hitDirection * info.knockbackForce;
            }

            if (_currentHp <= 0f)
            {
                _currentHp = 0f;
                Die();
            }
        }

        public void ApplyKnockback(Vector3 velocity)
        {
            _knockbackVelocity = velocity;
        }

        private Coroutine _stunCoroutine;
        public bool IsStunned => _stunCoroutine != null;

        public void ApplyStun(float duration)
        {
            if (IsDead) return;
            if (_stunCoroutine != null) StopCoroutine(_stunCoroutine);
            _stunCoroutine = StartCoroutine(StunRoutine(duration));
        }

        private IEnumerator StunRoutine(float duration)
        {
            SetInputLock(true);
            yield return new WaitForSeconds(duration);
            SetInputLock(false);
            _stunCoroutine = null;
        }

        public void ApplyPull(Vector3 targetPos, float duration = 0.35f)
        {
            if (IsDead) return;
            StartCoroutine(PullRoutine(targetPos, duration));
        }

        private IEnumerator PullRoutine(Vector3 targetPos, float duration)
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
                yield return null;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Die()
        {
            Debug.LogError("<color=red>[거타지 사망] 스테이지 공략 실패...</color>");
            SetInCombat(false);
            SetInputLock(true);
            _isCharging = false;
            _hasFullCharged = false;
            _currentChargeStep = 0;
            if (_chargingAuraInstance != null)
            {
                Destroy(_chargingAuraInstance);
                _chargingAuraInstance = null;
            }

            SetAnimatorTriggerSafe(AnimHashDeath);
            if (GameManager.Instance != null)
            {
                GameManager.Instance.FailStage();
            }
        }

        /// <summary>
        /// 사망 또는 스테이지 재시작 시 플레이어 생명력, 상태 및 애니메이션 완전 복구
        /// </summary>
        public void Respawn()
        {
            _currentHp = _maxHp;
            _isDashing = false;
            _isInvulnerable = false;
            _dashTimer = 0f;
            _isCharging = false;
            _hasFullCharged = false;
            _currentChargeStep = 0;
            _knockbackVelocity = Vector3.zero;
            _moveInput = Vector3.zero;
            SetInCombat(false); // 부활 시 반드시 비전투 상태로 리셋!
            SetInputLock(false);

            if (_chargingAuraInstance != null)
            {
                Destroy(_chargingAuraInstance);
                _chargingAuraInstance = null;
            }

            if (_stunCoroutine != null)
            {
                StopCoroutine(_stunCoroutine);
                _stunCoroutine = null;
            }

            if (_interactionAnimRoutine != null)
            {
                StopCoroutine(_interactionAnimRoutine);
                _interactionAnimRoutine = null;
            }

            ResetAnimation();

            Debug.Log("<color=green>[PlayerController] 플레이어가 정상 부활하고 애니메이션이 리셋되었습니다.</color>");
        }

        /// <summary>
        /// 사망/피격 등 애니메이터 상태를 기본 대기/이동(Move) 상태로 강제 복구
        /// </summary>
        public void ResetAnimation()
        {
            if (_animator == null) return;

            // 1. 트리거 소거
            ResetAnimatorTriggerSafe(AnimHashDeath);
            ResetAnimatorTriggerSafe(AnimHashDamage);
            ResetAnimatorTriggerSafe(AnimHashJump);
            SetAnimatorBoolSafe(AnimHashIsInteracting, false);
            _animator.SetFloat(AnimHashMoveSpeed, 0f);

            // 2. AnyState의 Respawn 트리거 발동
            SetAnimatorTriggerSafe(AnimHashRespawn);

            // 3. 확실한 상태 머신 리셋 (Rebind 및 Move 기본 상태 보장)
            _animator.Rebind();
            _animator.Update(0f);

            int baseLayerIndex = _animator.GetLayerIndex("Base Layer");
            _animator.Play("Move", baseLayerIndex >= 0 ? baseLayerIndex : 0, 0f);

            int upperLayerIndex = _animator.GetLayerIndex("Upper Layer");
            if (upperLayerIndex >= 0)
            {
                _animator.Play("New State", upperLayerIndex, 0f);
            }
        }

        public void ResetRunStats()
        {
            ApplyStatsFromTable();
            _currentHp = _maxHp;

            if (_bow != null)
            {
                _bow.ApplyStatsFromTable();
            }

            if (_dagger != null)
            {
                _dagger.ApplyStatsFromTable();
            }

            // 캐릭터 트리의 영구 스탯 보너스를 재적용
            CharacterTreeManager.Instance?.ApplySkillsToPlayer();

            ResetAnimation();

            Debug.Log("<color=cyan>[PlayerController] 런 스탯이 초기화되었습니다.</color>");
        }

        public void ApplyBoon(Progression.InRunBoonSO boon)
        {
            if (boon == null) return;

            // 생존 & 기동 (기존 대비 50% 밸런싱)
            _maxHp += boon.bonusHp;
            _currentHp = Mathf.Min(_currentHp + boon.bonusHp, _maxHp);
            _moveSpeed += boon.bonusMoveSpeed;
            if (boon.dashCooldownReduction > 0f)
            {
                _dashCooldown = Mathf.Max(0.4f, _dashCooldown - boon.dashCooldownReduction);
            }

            // GCD & 하트 회복
            if (boon.gcdReduction > 0f)
            {
                _sameCategoryGCD = Mathf.Max(0.2f, _sameCategoryGCD - boon.gcdReduction);
                _diffCategoryGCD = Mathf.Max(0.1f, _diffCategoryGCD - boon.gcdReduction);
            }
            if (boon.heartHealBonus > 0f)
            {
                RecoveryHeart.BonusHealAmount += boon.heartHealBonus;
            }
            if (boon.heartDropRateBonus > 0f)
            {
                RecoveryHeart.DropRateMultiplier += boon.heartDropRateBonus;
            }

            // 활 강화
            if (_bow != null)
            {
                _bow.arrowCount += boon.extraArrows;
                _bow.pierceCount += boon.extraPierce;
                if (boon.bonusAttackDamage > 0f) _bow.AddBaseDamage(boon.bonusAttackDamage);
                if (boon.bonusAttackRate > 0f) _bow.AddAttackRate(boon.bonusAttackRate);
                if (boon.bonusChargeDamageMultiplier > 0f) _bow.AddChargeDamageMultiplier(boon.bonusChargeDamageMultiplier);
                if (boon.bonusChargeRange > 0f) _bow.AddChargeMaxRange(boon.bonusChargeRange);
                if (boon.chargeSpeedBonus > 0f)
                {
                    _bow.ReduceChargeDuration(boon.chargeSpeedBonus);
                    _maxChargeTime = _bow.MaxChargeTime;
                }
            }

            // 단검 강화
            if (_dagger != null)
            {
                if (boon.bonusDaggerDamage > 0f) _dagger.AddBaseDamage(boon.bonusDaggerDamage);
                if (boon.bonusDaggerRadius > 0f) _dagger.AddAttackRadius(boon.bonusDaggerRadius);
                if (boon.bonusDaggerReflectDamage > 0f) _dagger.AddReflectDamageMultiplier(boon.bonusDaggerReflectDamage);
            }

            Debug.Log($"<color=yellow><b>[축복 적용 완료]</b> {boon.boonTitle} ({boon.description})</color>");
        }

        private float _appliedTreeHp = 0f;
        private float _appliedTreeSpeed = 0f;
        private int _appliedTreeArrows = 0;
        private int _appliedTreePierce = 0;

        public void ApplyStatBonus(float bonusHp, float bonusMoveSpeed, int extraArrows, int extraPierce)
        {
            // 기존 누적치 차감 후 갱신
            _maxHp -= _appliedTreeHp;
            _moveSpeed -= _appliedTreeSpeed;
            if (_bow != null)
            {
                _bow.arrowCount -= _appliedTreeArrows;
                _bow.pierceCount -= _appliedTreePierce;
            }

            _appliedTreeHp = bonusHp;
            _appliedTreeSpeed = bonusMoveSpeed;
            _appliedTreeArrows = extraArrows;
            _appliedTreePierce = extraPierce;

            _maxHp += _appliedTreeHp;
            _currentHp = Mathf.Min(_currentHp + _appliedTreeHp, _maxHp);
            _moveSpeed += _appliedTreeSpeed;
            if (_bow != null)
            {
                _bow.arrowCount += _appliedTreeArrows;
                _bow.pierceCount += _appliedTreePierce;
            }
        }

        public void Heal(float amount)
        {
            if (IsDead) return;
            float prevHp = _currentHp;
            _currentHp = Mathf.Min(_currentHp + amount, _maxHp);
            float actualHealed = _currentHp - prevHp;
            Debug.Log($"<color=green><b>[거타지 체력 회복]</b> +{actualHealed:F0} HP (현재: {_currentHp:F0}/{_maxHp})</color>");
            Core.VFXManager.Instance?.SpawnHealBuff(transform);
        }
    }
}
