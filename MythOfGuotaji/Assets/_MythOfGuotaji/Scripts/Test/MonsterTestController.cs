using System.Collections.Generic;
using MythOfGuotaji.Actor;
using MythOfGuotaji.Cameras;
using MythOfGuotaji.Data;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MythOfGuotaji.Test
{
    /// <summary>
    /// MonsterTest.scene 전용 테스트 컨트롤러
    /// - 좌/우 2개 방 고정 생성 (기본 포커스: 우측 방)
    /// - 15종 몬스터 검색 및 실시간 프리뷰 배치
    /// - "테스트시작" -> 좌측 방 플레이어 스폰, 카메라 추적, 몬스터 즉시 교전 개시
    /// - "테스트종료" -> 초기화 및 0번 상태(우측 방 뷰) 복귀
    /// - 몬스터 테이블(Stats & Skills) 실시간 런타임 리로드 지원
    /// </summary>
    public class MonsterTestController : MonoBehaviour
    {
        public static MonsterTestController Instance { get; private set; }

        private const float RoomSize = 39f;
        private static readonly Vector3 LeftRoomCenter = new Vector3(0f, 0f, 0f);
        private static readonly Vector3 RightRoomCenter = new Vector3(RoomSize, 0f, 0f);

        [Header("Room & Environment")]
        [SerializeField] private GameObject _dungeonRoot;
        [SerializeField] private Transform _rightRoomAnchor;

        [Header("Testing State")]
        [SerializeField] private bool _isTesting = false;
        private PlayerController _playerInstance;
        private readonly List<MonsterAI> _spawnedMonsters = new();

        [Header("Testing Weapons & GodMode")]
        [SerializeField] private string _selectedBowId = "Bow_00";
        [SerializeField] private string _selectedDaggerId = "Dagger_00";
        [SerializeField] private bool _isGodMode = false;
        private bool _isBowDropdownOpen = false;
        private bool _isDaggerDropdownOpen = false;
        private Vector2 _bowScrollPos;
        private Vector2 _daggerScrollPos;

        [Header("Placement State")]
        private MonsterType? _selectedMonsterType = null;
        private GameObject _previewInstance = null;
        private string _searchFilter = "";
        private Vector2 _scrollPos;

        // 15종 몬스터 정의 목록
        private struct MonsterInfo
        {
            public MonsterType type;
            public string korName;
            public string category;

            public MonsterInfo(MonsterType type, string korName, string category)
            {
                this.type = type;
                this.korName = korName;
                this.category = category;
            }
        }

        private static readonly MonsterInfo[] AllMonsterInfos = new MonsterInfo[]
        {
            new(MonsterType.Minion, "일반 요괴 (근접 돌진)", "근접 돌진"),
            new(MonsterType.Minion_Archer, "원거리 궁병 (원거리 사격)", "원거리 사격"),
            new(MonsterType.Minion_BigMon, "중형 요괴 (충격파 강타)", "충격파 강타"),
            new(MonsterType.Minion_Zhu, "곡사포 주 (원거리 곡사)", "원거리 곡사"),
            new(MonsterType.Elite, "엘리트 도깨비 (포효/돌진)", "엘리트"),
            new(MonsterType.FoxBoss, "보스 구미호 (진공파/휩쓸기)", "보스"),
            new(MonsterType.Dokkaebi_Fire, "도깨비불 (유도 화염구)", "기믹-투사체"),
            new(MonsterType.Boar_Charger, "멧돼지 돌격대 (저돌맹진)", "기믹-돌진"),
            new(MonsterType.Spider_Tether, "거미 요괴 (독 거미줄 결속)", "기믹-속박"),
            new(MonsterType.Mud_Slime, "진흙 슬라임 (감속 장판)", "기믹-장판"),
            new(MonsterType.Phantom_Fox, "환영 여우 (분신술/은신)", "기믹-은신분신"),
            new(MonsterType.Iron_Shield, "철갑 방패 요괴 (전방 가드)", "기믹-방패"),
            new(MonsterType.Energy_Absorber, "기 흡수기 (공격 반사)", "기믹-반사"),
            new(MonsterType.Bomb_Sparrow, "자폭 참새 떼 (초고속 물량)", "기믹-자폭"),
            new(MonsterType.Totem_Turret, "사방신 토템 (강화 고정 포탑)", "기믹-포탑"),
        };

        private void Awake()
        {
            Instance = this;
            EnsureSystemDependencies();
            BuildTwoDungeonRooms();
            SetupCameraForPlacement();
        }

        private void Update()
        {
            HandlePlacementInput();
        }

        private void EnsureSystemDependencies()
        {
            if (GameDataManager.Instance == null)
            {
                var gdmObj = new GameObject("[GameDataManager]");
                gdmObj.AddComponent<GameDataManager>();
            }

            // IngameSystemManagers 프리팹 로드 시도
            if (FindAnyObjectByType<Core.ObjectPoolManager>() == null)
            {
                var sysPrefab = Resources.Load<GameObject>("Prefabs/GUI/IngameSystemManagers");
                if (sysPrefab != null && GameObject.Find("IngameSystemManagers") == null)
                {
                    var sysObj = Instantiate(sysPrefab);
                    sysObj.name = "IngameSystemManagers";
                }
                else
                {
                    if (FindAnyObjectByType<Core.ObjectPoolManager>() == null)
                        new GameObject("[ObjectPoolManager]").AddComponent<Core.ObjectPoolManager>();
                }
            }

            // Directional Light 보장
            if (FindAnyObjectByType<Light>() == null)
            {
                var lightObj = new GameObject("Directional Light");
                var light = lightObj.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.2f;
                light.color = new Color(1f, 0.96f, 0.9f);
                lightObj.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            }
        }

        private void SetupCameraForPlacement()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                var camObj = new GameObject("Main Camera");
                cam = camObj.AddComponent<Camera>();
                camObj.tag = "MainCamera";
            }

            var qCam = cam.GetComponent<QuarterViewCamera>();
            if (qCam == null)
            {
                qCam = cam.gameObject.AddComponent<QuarterViewCamera>();
            }

            if (_rightRoomAnchor == null)
            {
                var anchorGo = new GameObject("[RightRoom_CameraAnchor]");
                anchorGo.transform.position = RightRoomCenter;
                _rightRoomAnchor = anchorGo.transform;
            }

            // 0번 상태: 우측 방 포커싱
            qCam.SetTarget(_rightRoomAnchor);
        }

        /// <summary>
        /// 0번 요구사항: 던전 방 두 개를 가로로 고정 배치 (Left Room at 0,0,0 & Right Room at 39,0,0)
        /// </summary>
        private void BuildTwoDungeonRooms()
        {
            if (_dungeonRoot != null) return;

            _dungeonRoot = new GameObject("[MonsterTest_Dungeon]");

            // 1. 좌측 방 (Player Start Room)
            CreateRoomVisual("LeftRoom_PlayerSpawn", LeftRoomCenter, new Color(0.20f, 0.28f, 0.22f), _dungeonRoot.transform);

            // 2. 우측 방 (Monster Battle Room)
            CreateRoomVisual("RightRoom_MonsterArena", RightRoomCenter, new Color(0.24f, 0.22f, 0.28f), _dungeonRoot.transform);

            // 3. 두 방 사이 중앙 연결 통로 바닥 (Passage)
/*            Vector3 passageCenter = new Vector3(RoomSize * 0.5f, 0f, 0f);
            GameObject passage = GameObject.CreatePrimitive(PrimitiveType.Plane);
            passage.name = "Connecting_Passage";
            passage.transform.SetParent(_dungeonRoot.transform);
            passage.transform.position = passageCenter;
            passage.transform.localScale = new Vector3(RoomSize * 0.1f, 1f, 0.8f);
            var passRend = passage.GetComponent<Renderer>();
            if (passRend != null) passRend.material.color = new Color(0.3f, 0.3f, 0.35f);
*/
            // 4. 외곽 경계 벽체 생성 (낙하 및 이탈 방지)
            BuildArenaOuterWalls(_dungeonRoot.transform);
        }

        private void CreateRoomVisual(string name, Vector3 pos, Color floorColor, Transform parent)
        {
            GameObject room = new GameObject(name);
            room.transform.SetParent(parent);
            room.transform.position = pos;

            // 바닥 Plane (10x10이 기본 Plane이므로 RoomSize/10 스케일)
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Floor";
            floor.transform.SetParent(room.transform, false);
            floor.transform.localPosition = Vector3.zero;
            floor.transform.localScale = new Vector3(RoomSize / 10f, 1f, RoomSize / 10f);
            floor.layer = LayerMask.NameToLayer("Default");

            var rend = floor.GetComponent<Renderer>();
            if (rend != null)
            {
                rend.material.color = floorColor;
                if (rend.material.HasProperty("_BaseColor")) rend.material.SetColor("_BaseColor", floorColor);
            }
        }

        private void BuildArenaOuterWalls(Transform parent)
        {
            float totalWidth = RoomSize * 2f;
            float height = 5f;
            float thickness = 1f;

            // 북쪽 벽
            CreateWall("Wall_North", new Vector3(RoomSize * 0.5f, height * 0.5f, RoomSize * 0.5f), new Vector3(totalWidth, height, thickness), parent);
            // 남쪽 벽
            CreateWall("Wall_South", new Vector3(RoomSize * 0.5f, height * 0.5f, -RoomSize * 0.5f), new Vector3(totalWidth, height, thickness), parent);
            // 서쪽 벽 (좌측 끝)
            CreateWall("Wall_West", new Vector3(-RoomSize * 0.5f, height * 0.5f, 0f), new Vector3(thickness, height, RoomSize), parent);
            // 동쪽 벽 (우측 끝)
            CreateWall("Wall_East", new Vector3(RoomSize * 1.5f, height * 0.5f, 0f), new Vector3(thickness, height, RoomSize), parent);
        }

        private void CreateWall(string name, Vector3 pos, Vector3 size, Transform parent)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.transform.SetParent(parent);
            wall.transform.position = pos;
            wall.transform.localScale = size;
            var rend = wall.GetComponent<Renderer>();
            if (rend != null) rend.material.color = new Color(0.15f, 0.15f, 0.18f);
        }

        #region Monster Placement & Preview

        private void HandlePlacementInput()
        {
            if (_isTesting)
            {
                if (_previewInstance != null)
                {
                    Destroy(_previewInstance);
                    _previewInstance = null;
                }
                return;
            }

            if (!_selectedMonsterType.HasValue)
            {
                if (_previewInstance != null)
                {
                    Destroy(_previewInstance);
                    _previewInstance = null;
                }
                return;
            }

            var mouse = Mouse.current;
            if (mouse == null) return;

            Vector2 mousePos = mouse.position.ReadValue();

            // 바닥 레이캐스트로 마우스 위치 추적
            Ray ray = Camera.main != null ? Camera.main.ScreenPointToRay(mousePos) : default;
            Plane groundPlane = new Plane(Vector3.up, Vector3.zero);

            if (Camera.main != null && groundPlane.Raycast(ray, out float enter))
            {
                Vector3 hitPoint = ray.GetPoint(enter);
                hitPoint.y = 0.5f;

                EnsurePreviewInstance(_selectedMonsterType.Value);
                if (_previewInstance != null)
                {
                    _previewInstance.transform.position = hitPoint;
                }

                // 좌클릭: 몬스터 배치 (신규 Input System)
                if (mouse.leftButton.wasPressedThisFrame)
                {
                    // 왼쪽 패널 및 상단 바 UI 영역 밖에서만 배치되도록 제한
                    if (mousePos.x > 320 && mousePos.y < Screen.height - 80)
                    {
                        PlaceMonsterAt(hitPoint, _selectedMonsterType.Value);
                    }
                }
            }

            // 우클릭: 선택 취소 (신규 Input System)
            if (mouse.rightButton.wasPressedThisFrame)
            {
                _selectedMonsterType = null;
                if (_previewInstance != null)
                {
                    Destroy(_previewInstance);
                    _previewInstance = null;
                }
            }
        }

        private void EnsurePreviewInstance(MonsterType type)
        {
            if (_previewInstance != null) return;

            // 반투명 마커 또는 간이 프리뷰 생성
            _previewInstance = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            _previewInstance.name = "[Preview_Marker]";
            _previewInstance.transform.localScale = new Vector3(1.2f, 0.8f, 1.2f);
            var col = _previewInstance.GetComponent<Collider>();
            if (col != null) Destroy(col);

            var rend = _previewInstance.GetComponent<Renderer>();
            if (rend != null)
            {
                var mat = rend.material;
                mat.color = new Color(0.2f, 0.9f, 0.4f, 0.5f);
            }
        }

        private void PlaceMonsterAt(Vector3 pos, MonsterType type)
        {
            GameObject prefab = null;
            if (GameDataManager.Instance != null && GameDataManager.Instance.Prefabs != null)
            {
                prefab = GameDataManager.Instance.Prefabs.GetMonsterPrefab(type);
            }

            if (prefab == null)
            {
                Debug.LogWarning($"[MonsterTestController] {type}의 프리팹을 찾을 수 없습니다.");
                return;
            }

            GameObject monsterObj = Instantiate(prefab, pos, Quaternion.identity);
            monsterObj.name = $"Test_{type}_{_spawnedMonsters.Count + 1}";

            var ai = monsterObj.GetComponent<MonsterAI>();
            if (ai != null)
            {
                // MonsterStatsTable 데이터 로드 및 초기화
                var stat = GameDataManager.Instance != null && GameDataManager.Instance.MonsterStats != null
                    ? GameDataManager.Instance.MonsterStats.GetStat(type)
                    : null;

                float hp = stat != null && stat.maxHp > 0 ? stat.maxHp : 50f;
                float spd = stat != null && stat.moveSpeed > 0 ? stat.moveSpeed : 4f;
                float dmg = stat != null && stat.attackDamage > 0 ? stat.attackDamage : 10f;

                ai.Setup(type, hp, spd, dmg, false, "TestPool", -1f, -1f, 0);
                ai.SetSpawnDelay(999999f); // 테스트 시작 전까지는 이동/공격 대기
                _spawnedMonsters.Add(ai);
            }

            Debug.Log($"<color=cyan>[MonsterTest] {type} 배치 완료! (현재 배치 수: {_spawnedMonsters.Count})</color>");
        }

        #endregion

        #region Test Start & End Flow

        /// <summary>
        /// 3번 요구사항: "테스트시작" 버튼 클릭 시 좌측 방에 플레이어 배치, 전투 개시, 카메라 플레이어 추적
        /// </summary>
        public void StartTest()
        {
            if (_isTesting) return;

            // 선택 프리뷰 해제
            _selectedMonsterType = null;
            if (_previewInstance != null)
            {
                Destroy(_previewInstance);
                _previewInstance = null;
            }

            // 1. 플레이어 스폰 (좌측 방 중심 Vector3(0, 0.5f, 0))
            Vector3 playerSpawnPos = new Vector3(LeftRoomCenter.x, 0.5f, LeftRoomCenter.z);
            if (_playerInstance == null)
            {
                GameObject playerPrefab = null;
                if (GameDataManager.Instance != null && GameDataManager.Instance.Prefabs != null)
                {
                    playerPrefab = PrefabDatabaseSO.ResolvePrefab(ref GameDataManager.Instance.Prefabs.playerPrefab, "Prefabs/Player_Guotaji");
                }
                if (playerPrefab == null)
                {
                    playerPrefab = Resources.Load<GameObject>("Prefabs/Player_Guotaji");
                }

                if (playerPrefab != null)
                {
                    var playerObj = Instantiate(playerPrefab, playerSpawnPos, Quaternion.identity);
                    playerObj.name = "Player_Guotaji";
                    _playerInstance = playerObj.GetComponent<PlayerController>();
                    if (_playerInstance != null)
                    {
                        _playerInstance.IsGodMode = _isGodMode;
                        _playerInstance.EquipBow(_selectedBowId);
                        _playerInstance.EquipDagger(_selectedDaggerId);
                    }
                }
            }
            else
            {
                _playerInstance.transform.position = playerSpawnPos;
                _playerInstance.gameObject.SetActive(true);
                _playerInstance.IsGodMode = _isGodMode;
                _playerInstance.EquipBow(_selectedBowId);
                _playerInstance.EquipDagger(_selectedDaggerId);
            }

            // 2. 카메라를 플레이어 타겟으로 전환
            if (QuarterViewCamera.Instance != null && _playerInstance != null)
            {
                QuarterViewCamera.Instance.SetTarget(_playerInstance.transform);
            }

            // 3. 배치된 모든 몬스터 전투 개시 (어그로 활성화)
            for (int i = 0; i < _spawnedMonsters.Count; i++)
            {
                var monster = _spawnedMonsters[i];
                if (monster != null && !monster.IsDead)
                {
                    monster.SetSpawnDelay(0f);
                    monster.TriggerAggro();
                }
            }

            _isTesting = true;
            Debug.Log("<color=green><b>[MonsterTest] 테스트 시작! 좌측 방에 플레이어가 배치되었습니다.</b></color>");
        }

        /// <summary>
        /// 4번 요구사항: "테스트종료" 클릭 시 모든 것을 초기화하고 0번(우측 방 뷰)으로 되돌아감
        /// </summary>
        public void EndTest()
        {
            _isTesting = false;

            // 1. 플레이어 제거
            if (_playerInstance != null)
            {
                Destroy(_playerInstance.gameObject);
                _playerInstance = null;
            }

            // 2. 배치/스폰된 모든 몬스터 제거
            for (int i = 0; i < _spawnedMonsters.Count; i++)
            {
                if (_spawnedMonsters[i] != null)
                {
                    Destroy(_spawnedMonsters[i].gameObject);
                }
            }
            _spawnedMonsters.Clear();
            MonsterAI.ActiveMonsters.Clear();

            // 3. 잔여 투사체/바닥 이펙트 정리
            CleanupRemainingCombatObjects();

            // 4. 0번 상태로 카메라 복귀 (우측 방 포커싱)
            SetupCameraForPlacement();

            Debug.Log("<color=yellow><b>[MonsterTest] 테스트 종료! 전체 초기화 완료. 우측 방 화면으로 복귀했습니다.</b></color>");
        }

        private void CleanupRemainingCombatObjects()
        {
            var projectiles = FindObjectsByType<Combat.Projectile>(FindObjectsSortMode.None);
            for (int i = 0; i < projectiles.Length; i++)
            {
                if (projectiles[i] != null) Destroy(projectiles[i].gameObject);
            }

            var zhuProjectiles = FindObjectsByType<Combat.ZhuParabolicProjectile>(FindObjectsSortMode.None);
            for (int i = 0; i < zhuProjectiles.Length; i++)
            {
                if (zhuProjectiles[i] != null) Destroy(zhuProjectiles[i].gameObject);
            }
        }

        #endregion

        #region GUI Implementation

        private void OnGUI()
        {
            // 상단 중앙: "테스트시작" / "테스트종료" 토글 버튼 (요구사항 1-2, 3)
            DrawCenterTopControls();

            // 상단 우측: 실시간 테이블 리로드 & 초기화 버튼 (요구사항 5)
            DrawRightTopControls();

            // 좌측: 검색 가능한 몬스터 리스트 (요구사항 1, 2)
            if (!_isTesting)
            {
                DrawLeftMonsterList();
            }
            else
            {
                DrawTestingOverlay();
            }
        }

        private void DrawCenterTopControls()
        {
            float btnWidth = 240f;
            float btnHeight = 50f;
            float x = (Screen.width - btnWidth) * 0.5f;
            float y = 15f;

            GUI.backgroundColor = _isTesting ? new Color(0.9f, 0.3f, 0.3f) : new Color(0.2f, 0.8f, 0.3f);
            GUIStyle btnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold
            };

            string btnText = _isTesting ? "■ 테스트 종료 (End Test)" : "▶ 테스트 시작 (Start Test)";

            if (GUI.Button(new Rect(x, y, btnWidth, btnHeight), btnText, btnStyle))
            {
                if (_isTesting)
                {
                    EndTest();
                }
                else
                {
                    StartTest();
                }
            }

            GUI.backgroundColor = Color.white;
        }

        private void DrawRightTopControls()
        {
            float x = Screen.width - 220f;
            float y = 15f;

            GUI.backgroundColor = new Color(0.3f, 0.7f, 1f);
            if (GUI.Button(new Rect(x, y, 200f, 35f), "⚡ 테이블 실시간 리로드"))
            {
                if (GameDataManager.Instance != null)
                {
                    GameDataManager.Instance.ReloadMonsterTables();
                }
            }

            if (!_isTesting && _spawnedMonsters.Count > 0)
            {
                GUI.backgroundColor = new Color(1f, 0.6f, 0.3f);
                if (GUI.Button(new Rect(x, y + 42f, 200f, 30f), $"🗑 배치 몬스터 전체 삭제 ({_spawnedMonsters.Count})"))
                {
                    for (int i = 0; i < _spawnedMonsters.Count; i++)
                    {
                        if (_spawnedMonsters[i] != null) Destroy(_spawnedMonsters[i].gameObject);
                    }
                    _spawnedMonsters.Clear();
                }
            }

            GUI.backgroundColor = Color.white;
        }

        private void DrawLeftMonsterList()
        {
            float panelWidth = 300f;
            float panelHeight = Screen.height - 40f;

            GUI.Box(new Rect(15f, 15f, panelWidth, panelHeight), "<b>[ 몬스터 소환 리스트 (15종) ]</b>");

            GUILayout.BeginArea(new Rect(25f, 45f, panelWidth - 20f, panelHeight - 40f));

            GUILayout.BeginHorizontal();
            GUILayout.Label("검색:", GUILayout.Width(40f));
            _searchFilter = GUILayout.TextField(_searchFilter);
            if (GUILayout.Button("X", GUILayout.Width(25f)))
            {
                _searchFilter = "";
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(5f);

            if (_selectedMonsterType.HasValue)
            {
                GUI.color = Color.cyan;
                GUILayout.Label($"<b>선택됨:</b> {_selectedMonsterType.Value}\n(바닥 좌클릭: 배치 / 우클릭: 취소)");
                GUI.color = Color.white;
            }
            else
            {
                GUILayout.Label("<color=gray>몬스터를 클릭 후 바닥을 클릭해 배치하세요.</color>");
            }

            GUILayout.Space(5f);

            _scrollPos = GUILayout.BeginScrollView(_scrollPos);

            for (int i = 0; i < AllMonsterInfos.Length; i++)
            {
                var info = AllMonsterInfos[i];
                string typeStr = info.type.ToString();

                // 검색어 필터링 (영어 타입명 또는 한글명)
                if (!string.IsNullOrEmpty(_searchFilter))
                {
                    if (!typeStr.ToLower().Contains(_searchFilter.ToLower()) &&
                        !info.korName.ToLower().Contains(_searchFilter.ToLower()))
                    {
                        continue;
                    }
                }

                bool isCurrentSelected = _selectedMonsterType.HasValue && _selectedMonsterType.Value == info.type;
                GUI.backgroundColor = isCurrentSelected ? Color.cyan : Color.white;

                if (GUILayout.Button($"<b>{info.type}</b>\n<size=11>{info.korName}</size>", GUILayout.Height(42f)))
                {
                    _selectedMonsterType = info.type;
                    if (_previewInstance != null)
                    {
                        Destroy(_previewInstance);
                        _previewInstance = null;
                    }
                }
            }

            GUI.backgroundColor = Color.white;
            GUILayout.EndScrollView();

            GUILayout.EndArea();
        }

        private void DrawTestingOverlay()
        {
            float panelWidth = 260f;
            GUI.Box(new Rect(15f, 15f, panelWidth, 120f), "<b>[ 테스트 진행 중 ]</b>");

            GUILayout.BeginArea(new Rect(25f, 45f, panelWidth - 20f, 85f));
            GUILayout.Label($"배치된 몬스터 수: <b>{_spawnedMonsters.Count}</b>");
            int aliveCount = 0;
            for (int i = 0; i < _spawnedMonsters.Count; i++)
            {
                if (_spawnedMonsters[i] != null && !_spawnedMonsters[i].IsDead) aliveCount++;
            }
            GUILayout.Label($"생존 몬스터 수: <b><color=yellow>{aliveCount}</color></b>");
            GUILayout.Label("<size=11><color=gray>종료하려면 상단 '테스트 종료'를 누르세요.</color></size>");
            GUILayout.EndArea();
        }

                private void DrawPlayerHpBar()
        {
            if (_playerInstance == null) return;

            float barWidth = 260f;
            float barHeight = 24f;
            float x = (Screen.width - barWidth) * 0.5f;
            float y = 70f;

            float curHp = _playerInstance.CurrentHp;
            float maxHp = _playerInstance.MaxHp;
            float ratio = Mathf.Clamp01(curHp / Mathf.Max(1f, maxHp));

            // Background
            GUI.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.85f);
            GUI.Box(new Rect(x, y, barWidth, barHeight), GUIContent.none);

            // Fill
            Color fillColor = ratio > 0.5f ? new Color(0.2f, 0.85f, 0.3f, 0.9f) : (ratio > 0.25f ? new Color(0.95f, 0.75f, 0.1f, 0.9f) : new Color(0.95f, 0.25f, 0.2f, 0.9f));
            GUI.backgroundColor = fillColor;
            GUI.Box(new Rect(x + 2f, y + 2f, (barWidth - 4f) * ratio, barHeight - 4f), GUIContent.none);

            // Text
            GUI.backgroundColor = Color.white;
            GUIStyle labelStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                fontSize = 12
            };
            labelStyle.normal.textColor = Color.white;
            string godSuffix = _isGodMode ? " <color=yellow>[무적]</color>" : "";
            GUI.Label(new Rect(x, y, barWidth, barHeight), $"HP: {curHp:F0} / {maxHp:F0} ({ratio * 100f:F0}%){godSuffix}", labelStyle);
        }

        private void DrawWeaponAndGodModeControls()
        {
            float panelWidth = 240f;
            float x = Screen.width - panelWidth - 20f;
            float y = 90f;

            var weaponStats = GameDataManager.Instance != null ? GameDataManager.Instance.WeaponStats : null;

            // 1. 무적 토글 버튼
            GUI.backgroundColor = _isGodMode ? new Color(1f, 0.85f, 0.2f) : new Color(0.35f, 0.35f, 0.4f);
            GUIStyle godStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold
            };
            string godText = _isGodMode ? "🛡️ 무적 상태 (GodMode): [ ON ]" : "🛡️ 무적 상태 (GodMode): [ OFF ]";
            if (GUI.Button(new Rect(x, y, panelWidth, 34f), godText, godStyle))
            {
                _isGodMode = !_isGodMode;
                if (_playerInstance != null)
                {
                    _playerInstance.IsGodMode = _isGodMode;
                }
            }
            GUI.backgroundColor = Color.white;

            // 2. 주무기(활) 드롭다운 토글 버튼
            y += 40f;
            string bowName = _selectedBowId;
            if (weaponStats != null)
            {
                var entry = weaponStats.GetBow(_selectedBowId);
                if (entry != null && !string.IsNullOrEmpty(entry.weaponName)) bowName = entry.weaponName;
            }

            GUI.backgroundColor = _isBowDropdownOpen ? new Color(0.4f, 0.9f, 0.5f) : new Color(0.85f, 0.95f, 1f);
            if (GUI.Button(new Rect(x, y, panelWidth, 32f), $"🏹 주무기(활): {bowName} ▼"))
            {
                _isBowDropdownOpen = !_isBowDropdownOpen;
                _isDaggerDropdownOpen = false;
            }
            GUI.backgroundColor = Color.white;

            // 주무기 드롭다운 목록
            if (_isBowDropdownOpen && weaponStats != null && weaponStats.bows != null)
            {
                float dropHeight = Mathf.Min(180f, weaponStats.bows.Count * 36f + 10f);
                GUI.Box(new Rect(x, y + 34f, panelWidth, dropHeight), GUIContent.none);
                _bowScrollPos = GUI.BeginScrollView(new Rect(x, y + 34f, panelWidth, dropHeight), _bowScrollPos, new Rect(0, 0, panelWidth - 20f, weaponStats.bows.Count * 34f));
                for (int i = 0; i < weaponStats.bows.Count; i++)
                {
                    var bow = weaponStats.bows[i];
                    bool isCur = bow.weaponId == _selectedBowId;
                    GUI.backgroundColor = isCur ? Color.cyan : Color.white;
                    if (GUI.Button(new Rect(5f, i * 34f, panelWidth - 30f, 30f), $"{bow.weaponName} ({bow.weaponId})"))
                    {
                        _selectedBowId = bow.weaponId;
                        _isBowDropdownOpen = false;
                        if (_playerInstance != null) _playerInstance.EquipBow(_selectedBowId);
                    }
                }
                GUI.backgroundColor = Color.white;
                GUI.EndScrollView();
            }

            // 3. 단검 드롭다운 토글 버튼
            y += 38f;
            string daggerName = _selectedDaggerId;
            if (weaponStats != null)
            {
                var entry = weaponStats.GetDagger(_selectedDaggerId);
                if (entry != null && !string.IsNullOrEmpty(entry.weaponName)) daggerName = entry.weaponName;
            }

            GUI.backgroundColor = _isDaggerDropdownOpen ? new Color(0.4f, 0.9f, 0.5f) : new Color(0.85f, 0.95f, 1f);
            if (GUI.Button(new Rect(x, y, panelWidth, 32f), $"🗡️ 단검: {daggerName} ▼"))
            {
                _isDaggerDropdownOpen = !_isDaggerDropdownOpen;
                _isBowDropdownOpen = false;
            }
            GUI.backgroundColor = Color.white;

            // 단검 드롭다운 목록
            if (_isDaggerDropdownOpen && weaponStats != null && weaponStats.daggers != null)
            {
                float dropHeight = Mathf.Min(180f, weaponStats.daggers.Count * 36f + 10f);
                GUI.Box(new Rect(x, y + 34f, panelWidth, dropHeight), GUIContent.none);
                _daggerScrollPos = GUI.BeginScrollView(new Rect(x, y + 34f, panelWidth, dropHeight), _daggerScrollPos, new Rect(0, 0, panelWidth - 20f, weaponStats.daggers.Count * 34f));
                for (int i = 0; i < weaponStats.daggers.Count; i++)
                {
                    var dagger = weaponStats.daggers[i];
                    bool isCur = dagger.weaponId == _selectedDaggerId;
                    GUI.backgroundColor = isCur ? Color.cyan : Color.white;
                    if (GUI.Button(new Rect(5f, i * 34f, panelWidth - 30f, 30f), $"{dagger.weaponName} ({dagger.weaponId})"))
                    {
                        _selectedDaggerId = dagger.weaponId;
                        _isDaggerDropdownOpen = false;
                        if (_playerInstance != null) _playerInstance.EquipDagger(_selectedDaggerId);
                    }
                }
                GUI.backgroundColor = Color.white;
                GUI.EndScrollView();
            }
        }
        #endregion
    }
}