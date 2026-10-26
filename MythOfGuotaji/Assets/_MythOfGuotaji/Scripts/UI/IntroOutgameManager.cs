using System;
using System.Collections.Generic;
using MythOfGuotaji.Core;
using MythOfGuotaji.Data;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace MythOfGuotaji.UI
{
    /// <summary>
    /// 아웃게임 씬(Intro.unity) 전용 매니저 및 거점(곡도 사당) UI 제어기
    /// - 기반 문서: _docs_/거타지 인연의 화살 — 시나리오·제작 기획서 v1.1b.md
    /// - 구성:
    ///   1. 타이틀 화면 (거타지: 인연의 화살 로고, 로그라인, 사당 진입 / 즉시 출격)
    ///   2. 곡도 사당 거점 (보옥의 결계 안 우물가 3D 환경 프리팹 + 사당 허브 UI)
    ///   3. 미궁 출격 (챕터 0 ~ 챕터 6, 전 22개 스테이지 선택 -> PlayMain.scene 로드)
    ///   4. 기억의 우물 (기억의 조각을 소비하여 활/단검/보옥/생존 4대 계열 영구 강화 및 100% 환급)
    ///   5. 기억 열람 (6대 핵심 기억 특성 장착 및 기억의 조각 시나리오 회상록 열람)
    ///   6. 사당 인연 대화 (보옥, 노승 부부/용왕, 해로, 호원 대사 상호작용)
    /// </summary>
    public class IntroOutgameManager : MonoBehaviour
    {
        public static IntroOutgameManager Instance { get; private set; }

        public enum OutgameTab
        {
            StageSelect = 0, // 미궁 출격 (챕터 0~6, 22스테이지)
            MemoryWell = 1,  // 기억의 우물 (4대 계열 영구 강화)
            MemoryArchive = 2 // 기억 열람 (핵심 기억 장착 & 시나리오 회상)
        }

        [Serializable]
        public class StageBriefingInfo
        {
            public int chapter;
            public int globalStageNumber; // 1 ~ 22
            public string stageCode;      // "0-1", "1-1" ... "6-3"
            public string stageTitle;
            public string bossOrKeeperName;
            public string gimmickSummary;
            public string scenarioNote;
        }

        [Serializable]
        public class CoreMemoryTraitInfo
        {
            public string id;
            public string title;
            public string unlockCondition;
            public string effectDescription;
            public string loreQuote;
        }

        [Header("Scene Environment & Camera")]
        [SerializeField] private GameObject _shrineEnvironmentRoot;
        [SerializeField] private Camera _shrineCamera;
        [SerializeField] private Transform _wellOrbitPivot;

        private bool _isTitleScreen = true;
        private OutgameTab _currentTab = OutgameTab.StageSelect;
        private int _selectedChapterFilter = 1; // 0 ~ 6
        private int _selectedStageIndex = 1;    // 0-based index into _stages list (default "1-1")
        private int _selectedArchiveIndex = 0;

        private string _activeSpeakerName = "보옥 (보석 토끼)";
        private string _activeDialogueText = "“여긴 안전해요. 우물 안쪽은 제 결계 안이니까요. 출격 준비가 되면 미궁의 문을 여세요.”";

        private Texture2D _whiteTex;
        private Vector2 _stageScrollPos;
        private Vector2 _archiveScrollPos;

        private readonly List<StageBriefingInfo> _stages = new();
        private readonly List<CoreMemoryTraitInfo> _coreMemories = new();

        public void BindSceneReferences(GameObject shrineEnvRoot, Camera shrineCam, Transform orbitPivot)
        {
            _shrineEnvironmentRoot = shrineEnvRoot;
            _shrineCamera = shrineCam;
            _wellOrbitPivot = orbitPivot;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            Time.timeScale = 1f;

            _whiteTex = new Texture2D(1, 1);
            _whiteTex.SetPixel(0, 0, Color.white);
            _whiteTex.Apply();

            BuildScenarioDataFromSpecV11b();
            EnsureManagersAndEnvironment();
        }

        private void Start()
        {
            // 아웃게임에서는 일반 마우스 커서 활성화
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            if (GameManager.Instance != null && GameManager.Instance.SaveData != null)
            {
                var save = GameManager.Instance.SaveData;

                // 직전 플레이에서 사망 후 귀환했거나 이미 진행 중인 경우 바로 사당 허브로 진입 가능
                if (save.returnedFromDeath)
                {
                    _isTitleScreen = false;
                    _activeSpeakerName = "보옥 (보석 토끼)";
                    _activeDialogueText = "“흥, 이번엔 봐줄게요. 다음엔 없어요... 아직 기억이 다 안 돌아왔나 봐요. 기억의 우물을 들여다봐요.”";
                    save.returnedFromDeath = false;
                    GameManager.Instance.SaveGame();
                }

                // 저장된 스테이지 코드와 일치하는 항목 선택
                int foundIdx = _stages.FindIndex(s => s.stageCode == save.selectedStageCode);
                if (foundIdx >= 0)
                {
                    _selectedStageIndex = foundIdx;
                    _selectedChapterFilter = _stages[foundIdx].chapter;
                }
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (_whiteTex != null) Destroy(_whiteTex);
        }

        private void Update()
        {
            // 사당 거점 카메라의 부드러운 호흡/공전 연출
            if (_shrineCamera != null)
            {
                float t = Time.unscaledTime * 0.28f;
                Vector3 center = _wellOrbitPivot != null ? _wellOrbitPivot.position : new Vector3(0f, 1.2f, 0f);
                float radius = 9.5f;
                float angle = Mathf.Sin(t) * 18f * Mathf.Deg2Rad;
                Vector3 camPos = center + new Vector3(Mathf.Sin(angle) * radius, 5.2f + Mathf.Sin(t * 1.5f) * 0.25f, -Mathf.Cos(angle) * radius);
                _shrineCamera.transform.position = Vector3.Lerp(_shrineCamera.transform.position, camPos, Time.unscaledDeltaTime * 3f);
                _shrineCamera.transform.LookAt(center + Vector3.up * 0.6f);
            }
        }

        private void EnsureManagersAndEnvironment()
        {
            if (FindAnyObjectByType<GameDataManager>() == null)
            {
                new GameObject("[GameDataManager]").AddComponent<GameDataManager>();
            }
            if (FindAnyObjectByType<GameManager>() == null)
            {
                new GameObject("[GameManager]").AddComponent<GameManager>();
            }

            var es = FindAnyObjectByType<EventSystem>();
            if (es == null)
            {
                var esGo = new GameObject("EventSystem");
                es = esGo.AddComponent<EventSystem>();
                esGo.AddComponent<InputSystemUIInputModule>();
            }
            else
            {
                var standalone = es.GetComponent<StandaloneInputModule>();
                if (standalone != null)
                {
                    standalone.enabled = false;
                    DestroyImmediate(standalone);
                }
                if (es.GetComponent<InputSystemUIInputModule>() == null)
                {
                    es.gameObject.AddComponent<InputSystemUIInputModule>();
                }
            }

            // 3D 곡도 사당 배경 프리팹 확인 및 인스턴스화
            if (_shrineEnvironmentRoot == null)
            {
                var existingEnv = GameObject.Find("Outgame_GokdoShrineEnv");
                if (existingEnv != null)
                {
                    _shrineEnvironmentRoot = existingEnv;
                }
                else
                {
                    GameObject envPrefab = null;
                    if (GameDataManager.Instance != null && GameDataManager.Instance.Prefabs != null)
                    {
                        envPrefab = PrefabDatabaseSO.ResolvePrefab(ref GameDataManager.Instance.Prefabs.outgameShrineEnvPrefab, "Prefabs/Outgame_GokdoShrineEnv");
                    }
                    else
                    {
                        envPrefab = Resources.Load<GameObject>("Prefabs/Outgame_GokdoShrineEnv");
                    }

                    if (envPrefab != null)
                    {
                        _shrineEnvironmentRoot = Instantiate(envPrefab, Vector3.zero, Quaternion.identity);
                        _shrineEnvironmentRoot.name = "Outgame_GokdoShrineEnv";
                    }
                }
            }

            if (_shrineEnvironmentRoot != null && _wellOrbitPivot == null)
            {
                var well = _shrineEnvironmentRoot.transform.Find("WellOfMemory");
                _wellOrbitPivot = well != null ? well : _shrineEnvironmentRoot.transform;
            }

            if (_shrineCamera == null)
            {
                _shrineCamera = Camera.main;
                if (_shrineCamera == null)
                {
                    var camGo = new GameObject("Main Camera");
                    camGo.tag = "MainCamera";
                    _shrineCamera = camGo.AddComponent<Camera>();
                    camGo.AddComponent<AudioListener>();
                    _shrineCamera.clearFlags = CameraClearFlags.SolidColor;
                    _shrineCamera.backgroundColor = new Color(0.04f, 0.06f, 0.11f, 1f);
                }
            }
        }

        private void BuildScenarioDataFromSpecV11b()
        {
            _stages.Clear();

            // 챕터 0: 폭풍우와 괴신선 (튜토리얼, 1스테이지)
            _stages.Add(new StageBriefingInfo
            {
                chapter = 0, globalStageNumber = 1, stageCode = "0-1",
                stageTitle = "폭풍우와 괴신선 (튜토리얼)",
                bossOrKeeperName = "괴신선 갑판 요괴 무리",
                gimmickSummary = "선형 4개 방 구성 — 기본 이동·회피(슬라이딩), 활 5연사/차징 사격, 단검 패링, 보옥의 영혼석 수집 튜토리얼",
                scenarioNote = "서기 890년, 당나라로 향하던 사절단 선박이 진성여왕의 기억을 노린 검은 구미호의 폭풍에 난파되고, 거타지는 곡도(鵠島) 해안에서 눈을 뜬다."
            });

            // 챕터 1: 안개 낀 해안과 부러진 목궁 (3스테이지)
            _stages.Add(new StageBriefingInfo
            {
                chapter = 1, globalStageNumber = 2, stageCode = "1-1",
                stageTitle = "안개 낀 곡도 해안",
                bossOrKeeperName = "수문장: 갑각 해안 수호병",
                gimmickSummary = "해안 안개 지형 / 기본 돌진 요괴 & 맹독충 출현 — 단검으로 독침을 흘려보내고 열쇠를 확보하라",
                scenarioNote = "바위 틈에 갇혀 있던 보석 토끼 '보옥'을 구출한 거타지. 보옥은 자신이 여왕의 여의보주임을 숨긴 채 동행을 제안한다."
            });
            _stages.Add(new StageBriefingInfo
            {
                chapter = 1, globalStageNumber = 3, stageCode = "1-2",
                stageTitle = "부러진 목궁의 기억",
                bossOrKeeperName = "수문장: 정예 암석 괴수",
                gimmickSummary = "정예 암석 굴리기 기믹 / 클리어 시 핵심 기억 [초심의 시위] 해금",
                scenarioNote = "어린 시절 처음 활시위를 당기던 거타지의 기억과, 그를 호위무사로 발탁했던 진성여왕의 첫인상이 겹쳐진다."
            });
            _stages.Add(new StageBriefingInfo
            {
                chapter = 1, globalStageNumber = 4, stageCode = "1-3",
                stageTitle = "망루의 눈먼 파수꾼 (챕터 1 보스전)",
                bossOrKeeperName = "보스: 눈먼 파수꾼 석진(石鎭)",
                gimmickSummary = "둔중한 철퇴 파동과 투석 탄막 — 푸른 투사체를 단검 패링으로 되돌려 석진의 흉갑(기억의 핵)을 노출시켜라!",
                scenarioNote = "과거 곡도 사당을 지키다 여우의 요기에 눈이 먼 거인 무사 석진을 정화하고 첫 번째 진실의 파편을 회수한다."
            });

            // 챕터 2: 불타는 서라벌의 환영 (4스테이지)
            _stages.Add(new StageBriefingInfo
            {
                chapter = 2, globalStageNumber = 5, stageCode = "2-1",
                stageTitle = "잿빛 회랑의 화염",
                bossOrKeeperName = "수문장: 도깨비 화염술사 우두머리",
                gimmickSummary = "도깨비불 패링 핑퐁 기믹 — 느리게 날아오는 도깨비 화염구를 단검으로 쳐내어 보호막을 분쇄하라",
                scenarioNote = "미궁 내부가 불타는 서라벌 왕경의 환영으로 일렁인다. 거타지는 자신이 떠나온 날 밤의 불길을 기억해낸다."
            });
            _stages.Add(new StageBriefingInfo
            {
                chapter = 2, globalStageNumber = 6, stageCode = "2-2",
                stageTitle = "무너진 전각과 화랑의 맹세",
                bossOrKeeperName = "수문장: 저돌맹진 중장 멧돼지",
                gimmickSummary = "돌진 유도 후 벽 격돌 기절 기믹 — 화랑 후배 '호원' 구출 이벤트 발생",
                scenarioNote = "환영 속에서 헤매던 화랑 후배 호원과 재회한다. 호원은 거타지가 반역자로 몰렸다는 충격적인 소식을 전한다."
            });
            _stages.Add(new StageBriefingInfo
            {
                chapter = 2, globalStageNumber = 7, stageCode = "2-3",
                stageTitle = "붉은 화살의 누명",
                bossOrKeeperName = "수문장: 화염 주(鴸) 군단장",
                gimmickSummary = "2초 곡사 폭격 장판 / 클리어 시 핵심 기억 [흔들리지 않는 눈] 해금",
                scenarioNote = "거타지의 기억 속 '여왕을 겨눈 붉은 화살'은 암살 시도가 아니라, 여왕의 그림자에 숨어든 요괴를 쏘아 맞힌 것이었음이 드러난다."
            });
            _stages.Add(new StageBriefingInfo
            {
                chapter = 2, globalStageNumber = 8, stageCode = "2-4",
                stageTitle = "단죄의 화염 (챕터 2 보스전)",
                bossOrKeeperName = "보스: 붉은 집행관(赤執行官)",
                gimmickSummary = "화염 검기 연속 발사 & 십자 화염 기둥 — 강화(황금) 검기를 패링 반사하여 그로기를 유도하고 완전 충전 화살로 격파!",
                scenarioNote = "왜곡된 죄책감이 구현화한 붉은 집행관을 쓰러뜨리고, 거타지는 스스로의 활에 대한 의심을 거둔다."
            });

            // 챕터 3: 통곡의 나루터 (4스테이지)
            _stages.Add(new StageBriefingInfo
            {
                chapter = 3, globalStageNumber = 9, stageCode = "3-1",
                stageTitle = "안개 잠긴 나루터",
                bossOrKeeperName = "수문장: 진흙 이괴 수호자",
                gimmickSummary = "늪지 감속 장판 & 요선 거미 견인 사슬 — 슬라이딩(무적 회피)으로 거미줄을 즉시 절단하라",
                scenarioNote = "침몰한 사절단원들의 원혼이 떠도는 통곡의 나루터. 늙은 바다거북 '해로'가 물길을 안내한다."
            });
            _stages.Add(new StageBriefingInfo
            {
                chapter = 3, globalStageNumber = 10, stageCode = "3-2",
                stageTitle = "가라앉은 갑판의 흔적",
                bossOrKeeperName = "수문장: 흑철 방패수문장",
                gimmickSummary = "전면 방패 방어 — 슬라이딩으로 배후를 잡거나 패링 반사탄으로 방패를 무력화하라",
                scenarioNote = "난파 당시 동료들을 구하지 못했다는 거타지의 회한이 시험대에 오른다."
            });
            _stages.Add(new StageBriefingInfo
            {
                chapter = 3, globalStageNumber = 11, stageCode = "3-3",
                stageTitle = "노승 부부의 탄식",
                bossOrKeeperName = "수문장: 심해 독충 여왕",
                gimmickSummary = "다중 산탄 독침 탄막 / 클리어 시 핵심 기억 [회한의 무게] 해금",
                scenarioNote = "곡도 사당의 노승 부부가 사실 서해 용왕과 용비의 화신이었으며, 여우가 그들의 간을 노리고 있음이 밝혀진다."
            });
            _stages.Add(new StageBriefingInfo
            {
                chapter = 3, globalStageNumber = 12, stageCode = "3-4",
                stageTitle = "원한의 물길 (챕터 3 보스전)",
                bossOrKeeperName = "보스: 원한의 뱃사공 양패(兩貝)",
                gimmickSummary = "파도 밀치기 & 원혼 구체 소환 — 원혼 구체를 단검으로 쳐내어 양패의 등불(기억의 핵)을 정화하라!",
                scenarioNote = "바다에 수장된 사절단의 한을 품은 양패를 진혼하고, 보옥은 거타지에게 조금씩 마음을 열기 시작한다."
            });

            // 챕터 4: 천년의 여우굴 (4스테이지)
            _stages.Add(new StageBriefingInfo
            {
                chapter = 4, globalStageNumber = 13, stageCode = "4-1",
                stageTitle = "환영이 깃든 여우숲",
                bossOrKeeperName = "수문장: 환영 여우귀 수석",
                gimmickSummary = "피격 시 2체 분신 생성 기믹 — 보옥의 빛(또는 차징 관통 화살)으로 본체와 분신을 동시에 꿰뚫어라",
                scenarioNote = "여우굴 초입, 보옥의 목소리와 진성여왕의 목소리가 완벽히 겹쳐 들리기 시작한다."
            });
            _stages.Add(new StageBriefingInfo
            {
                chapter = 4, globalStageNumber = 14, stageCode = "4-2",
                stageTitle = "거짓된 밀실",
                bossOrKeeperName = "수문장: 영력 흡수 괴물",
                gimmickSummary = "원거리 사격 흡수 배리어 — 근접 단검 베기로 배리어를 파쇄한 뒤 사격하라",
                scenarioNote = "천년 묵은 여우가 왜 진성여왕의 기억과 용왕의 일족을 동시에 노렸는지 단서가 드러난다."
            });
            _stages.Add(new StageBriefingInfo
            {
                chapter = 4, globalStageNumber = 15, stageCode = "4-3",
                stageTitle = "여의보주의 진실",
                bossOrKeeperName = "수문장: 사방신 수호 토템",
                gimmickSummary = "일반 공격 면역 토템 — 토템이 발사하는 황금 탄환을 단검 패링(2배 피해)하여 파괴! 핵심 기억 [의연함] 해금",
                scenarioNote = "보옥은 단순한 보물이 아니라, 진성여왕이 자신의 생명력과 기억을 쪼개어 거타지를 지키기 위해 만든 분신이었다."
            });
            _stages.Add(new StageBriefingInfo
            {
                chapter = 4, globalStageNumber = 16, stageCode = "4-4",
                stageTitle = "천년 여우의 그림자 (챕터 4 보스전)",
                bossOrKeeperName = "보스: 천년 여우의 그림자 연호(煙狐)",
                gimmickSummary = "3방향 환영 분신 탄막 & 부채꼴 진공 흡입 — 진짜 연호의 꼬리 핵을 간파하여 완전 충전 화살을 꽂아라!",
                scenarioNote = "여우의 분신 연호를 격파하지만, 본체인 검은 구미호는 이미 심연의 제단에서 용왕의 영역을 침식하고 있었다."
            });

            // 챕터 5: 심연의 제단 (4스테이지)
            _stages.Add(new StageBriefingInfo
            {
                chapter = 5, globalStageNumber = 17, stageCode = "5-1",
                stageTitle = "침식된 해저 신전",
                bossOrKeeperName = "수문장: 심연 철갑 거북",
                gimmickSummary = "20m 가속 돌진 & 3m 반경 충격파 — 지형 기둥 뒤로 회피하여 돌진을 멈추게 한 뒤 반격하라",
                scenarioNote = "서해 용궁으로 이어지는 심연의 제단. 흑기(黑氣)에 오염된 신수들이 거타지의 앞길을 막는다."
            });
            _stages.Add(new StageBriefingInfo
            {
                chapter = 5, globalStageNumber = 18, stageCode = "5-2",
                stageTitle = "용비의 결계",
                bossOrKeeperName = "수문장: 자폭 화염조 군단",
                gimmickSummary = "고속 자폭 참새 떼 & 곡사 폭격 복합 웨이브",
                scenarioNote = "용왕 부부가 마지막 힘으로 제단의 봉인을 지탱하고 있다. 거타지는 신궁의 경지에 다다른다."
            });
            _stages.Add(new StageBriefingInfo
            {
                chapter = 5, globalStageNumber = 19, stageCode = "5-3",
                stageTitle = "신궁(神弓)의 각성",
                bossOrKeeperName = "수문장: 심연 수문장 쌍귀",
                gimmickSummary = "복합 정예 수문장 2기 동시 교전 / 클리어 시 핵심 기억 [신궁(神弓)] 해금",
                scenarioNote = "“내 화살은 죽이기 위한 것이 아니라, 이어진 인연을 되돌리기 위한 것이다.”"
            });
            _stages.Add(new StageBriefingInfo
            {
                chapter = 5, globalStageNumber = 20, stageCode = "5-4",
                stageTitle = "타락한 흑룡 (챕터 5 보스전)",
                bossOrKeeperName = "보스: 타락한 흑룡 청잠(靑潛)",
                gimmickSummary = "거대 브레스 장판 & 뇌전 탄막 — 강화 뇌전구를 연속 패링하여 흑룡의 역린(기억의 핵)을 꿰뚫어라!",
                scenarioNote = "여우의 주술에 잠식당했던 용왕의 아들 청잠을 정화하고, 용왕으로부터 최후의 비술과 축복을 받는다."
            });

            // 챕터 6: 새벽을 여는 시위 (3스테이지)
            _stages.Add(new StageBriefingInfo
            {
                chapter = 6, globalStageNumber = 21, stageCode = "6-1",
                stageTitle = "진홍의 영역",
                bossOrKeeperName = "수문장: 구미호의 친위 요괴장",
                gimmickSummary = "전 기믹 총집결 웨이브 — 도깨비불, 사방신 토템, 거미 사슬, 암석 돌진 복합 돌파",
                scenarioNote = "스물한 개의 기억 조각이 하나로 모이며, 거타지와 보옥은 모든 진실을 마주한 채 최후의 결전지로 향한다."
            });
            _stages.Add(new StageBriefingInfo
            {
                chapter = 6, globalStageNumber = 22, stageCode = "6-2",
                stageTitle = "인연의 끝과 시작 (최종장 전초 & 진엔딩 결전)",
                bossOrKeeperName = "최종 보스: 진홍의 주인 검은 구미호 (챕터 6-2 / 6-3)",
                gimmickSummary = "9꼬리 페이즈 전환 (부채꼴 진공 흡입 + 180도 휩쓸기 + 여우불 탄막 패링) / 22개 기억 해금 시 [새벽의 시위] 진엔딩!",
                scenarioNote = "보옥이 자신을 희생해 여우를 봉인하려는 순간, 모든 기억을 되찾은 거타지가 '새벽의 시위'를 당겨 여우의 요기만을 꿰뚫고 보옥과 여왕의 혼을 함께 구원한다."
            });

            // 6대 핵심 기억 (기획서 5.2장)
            _coreMemories.Clear();
            _coreMemories.Add(new CoreMemoryTraitInfo
            {
                id = "MEM_CH1_FIRST_STRING",
                title = "1. 초심의 시위 (챕터 1 핵심 기억)",
                unlockCondition = "챕터 1-2 해금 (기본 해금)",
                effectDescription = "전투 진입 후 첫 화살 및 완전 충전 사격의 치명타 확률 +20% 증가",
                loreQuote = "“시위를 당길 때 손끝이 떨리는 것은 두려움 때문이 아니다. 지켜야 할 이의 숨결이 시위에 실려 있기 때문이다.”"
            });
            _coreMemories.Add(new CoreMemoryTraitInfo
            {
                id = "MEM_CH2_STEADY_EYE",
                title = "2. 흔들리지 않는 눈 (챕터 2 핵심 기억)",
                unlockCondition = "챕터 2-3 해금",
                effectDescription = "완전 충전 화살 적중 시 보스/수문장의 약점(기억의 핵) 노출 시간 +1.5초 연장",
                loreQuote = "“불타는 왕경 한복판에서도 내가 겨눈 것은 여왕의 심장이 아니라, 그 뒤편에 도사린 검은 그림자였다.”"
            });
            _coreMemories.Add(new CoreMemoryTraitInfo
            {
                id = "MEM_CH3_REGRET_WEIGHT",
                title = "3. 회한의 무게 (챕터 3 핵심 기억)",
                unlockCondition = "챕터 3-3 해금",
                effectDescription = "단검 베기 및 투사체 패링 성공 시 보옥 보호막 충전량 및 이동속도 보너스 증가",
                loreQuote = "“가라앉은 전우들의 이름을 잊지 않겠다. 부러진 화살촉을 벼려 만든 이 단검이 내 두 번째 맹세다.”"
            });
            _coreMemories.Add(new CoreMemoryTraitInfo
            {
                id = "MEM_CH4_RESOLUTE",
                title = "4. 의연함 (챕터 4 핵심 기억)",
                unlockCondition = "챕터 4-3 해금",
                effectDescription = "환영·분신 요괴 간파 및 본체에 대한 최종 피해량 +25% 증가",
                loreQuote = "“천 개의 거짓 목소리가 속삭여도, 내 뒤를 따르는 작은 토끼의 발소리 하나만은 속일 수 없다.”"
            });
            _coreMemories.Add(new CoreMemoryTraitInfo
            {
                id = "MEM_CH5_DIVINE_BOW",
                title = "5. 신궁(神弓) (챕터 5 핵심 기억)",
                unlockCondition = "챕터 5-3 해금",
                effectDescription = "완전 충전(25m) 화살이 적의 방패·배리어를 관통하며 넉백 위력 +35% 증가",
                loreQuote = "“바다를 가르고 먹구름을 꿰뚫는 한 발. 삼국유사에 기록된 명궁 거타지의 진정한 각성.”"
            });
            _coreMemories.Add(new CoreMemoryTraitInfo
            {
                id = "MEM_CH6_DAWN_STRING",
                title = "6. 새벽의 시위 (챕터 6 진엔딩 핵심 기억)",
                unlockCondition = "기억의 조각 22개 전체 회수",
                effectDescription = "모든 무기와 비술의 재사용 대기시간 감소 및 진엔딩 전용 황금 인연 화살 발동",
                loreQuote = "“보옥아, 너를 부수고 얻는 평화는 내 답이 아니다. 이 화살로 악연만 끊고, 인연은 남긴다!”"
            });
        }

        private void OnGUI()
        {
            float sw = Screen.width;
            float sh = Screen.height;
            float scale = Mathf.Clamp(sh / 1080f, 0.65f, 1.5f);

            if (_isTitleScreen)
            {
                DrawTitleScreen(sw, sh, scale);
            }
            else
            {
                DrawShrineHubScreen(sw, sh, scale);
            }
        }

        private void DrawTitleScreen(float sw, float sh, float scale)
        {
            Color prev = GUI.color;

            // 반투명 상하단 시네마틱 레터박스 & 중앙 다크 패널
            GUI.color = new Color(0.02f, 0.03f, 0.06f, 0.55f);
            GUI.DrawTexture(new Rect(0, 0, sw, sh), _whiteTex);

            float cardW = Mathf.Clamp(sw * 0.62f, 640f, 1080f);
            float cardH = 540f * scale;
            float cardX = (sw - cardW) * 0.5f;
            float cardY = (sh - cardH) * 0.5f;

            GUI.color = new Color(0.05f, 0.07f, 0.12f, 0.92f);
            GUI.DrawTexture(new Rect(cardX, cardY, cardW, cardH), _whiteTex);

            GUI.color = new Color(0.85f, 0.68f, 0.28f, 0.9f);
            DrawRectBorder(new Rect(cardX - 2, cardY - 2, cardW + 4, cardH + 4), 2f);

            // 상단 장르 태그
            GUIStyle tagStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.RoundToInt(16f * scale),
                fontStyle = FontStyle.Bold
            };
            tagStyle.normal.textColor = new Color(0.35f, 0.9f, 0.95f);
            GUI.Label(new Rect(cardX, cardY + 28f * scale, cardW, 28f * scale), "《 삼국유사 『거타지(居陀知)』 설화 재해석 — 쿼터뷰 로그라이크 아처 액션 》", tagStyle);

            // 메인 타이틀
            GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.RoundToInt(48f * scale),
                fontStyle = FontStyle.Bold
            };
            titleStyle.normal.textColor = new Color(1f, 0.90f, 0.42f);
            GUI.Label(new Rect(cardX, cardY + 65f * scale, cardW, 65f * scale), "🏹 거타지 : 인연의 화살", titleStyle);

            // 영문 서브타이틀 & 로그라인
            GUIStyle loglineStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.RoundToInt(19f * scale),
                fontStyle = FontStyle.Italic
            };
            loglineStyle.normal.textColor = new Color(0.92f, 0.95f, 1f);
            GUI.Label(new Rect(cardX, cardY + 135f * scale, cardW, 32f * scale), "“쏘고, 흘려보내고, 되돌려라. 죽을 때마다 기억이 바뀌는 한국 신화 로그라이크 아처 액션”", loglineStyle);

            // 시놉시스 박스
            GUIStyle loreStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.RoundToInt(15f * scale),
                wordWrap = true
            };
            loreStyle.normal.textColor = new Color(0.78f, 0.84f, 0.92f);
            string synopsis =
                "서기 890년(통일신라 진성여왕 4년), 폭풍우에 난파되어 안개 낀 외딴섬 '곡도(鵠島)'에 표류한 명궁 거타지.\n" +
                "기억을 잃은 그의 곁에는 자신을 영물이라 부르는 까칠한 보석 토끼 '보옥(寶玉)'이 함께하고 있다.\n" +
                "보옥의 결계가 펼쳐진 곡도 사당의 우물가를 거점으로 삼아, 천년 묵은 검은 구미호의 미궁을 돌파하고 22개의 기억을 되찾아라.";
            GUI.Label(new Rect(cardX + 50f * scale, cardY + 185f * scale, cardW - 100f * scale, 95f * scale), synopsis, loreStyle);

            // 버튼 3종
            float btnW = 360f * scale;
            float btnH = 52f * scale;
            float btnX = cardX + (cardW - btnW) * 0.5f;

            GUIStyle btnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = Mathf.RoundToInt(19f * scale),
                fontStyle = FontStyle.Bold
            };

            GUI.color = new Color(0.2f, 0.85f, 0.95f, 1f);
            if (GUI.Button(new Rect(btnX, cardY + 300f * scale, btnW, btnH), "⛩️ 곡도 사당 거점으로 진입 (Outgame Hub)", btnStyle))
            {
                _isTitleScreen = false;
                if (GameManager.Instance != null) GameManager.Instance.SetState(GameState.Hub);
            }

            var save = GameManager.Instance?.SaveData;
            string currentCode = save != null && !string.IsNullOrEmpty(save.selectedStageCode) ? save.selectedStageCode : "1-1";
            GUI.color = new Color(0.25f, 0.92f, 0.55f, 1f);
            if (GUI.Button(new Rect(btnX, cardY + 368f * scale, btnW, btnH), $"🏹 즉시 미궁 출격 (Stage {currentCode} → PlayMain)", btnStyle))
            {
                LaunchSelectedStage();
            }

            GUI.color = new Color(0.85f, 0.45f, 0.45f, 0.9f);
            btnStyle.fontSize = Mathf.RoundToInt(15f * scale);
            if (GUI.Button(new Rect(btnX, cardY + 436f * scale, btnW, 38f * scale), "🔄 기억 및 세이브 데이터 초기화", btnStyle))
            {
                SaveSystem.DeleteSave();
                GameManager.Instance?.LoadGame();
                _selectedStageIndex = 1;
                _selectedChapterFilter = 1;
            }

            GUI.color = prev;
        }

        private void DrawShrineHubScreen(float sw, float sh, float scale)
        {
            Color prev = GUI.color;
            var save = GameManager.Instance != null ? GameManager.Instance.SaveData : null;

            // 1. 상단 사당 거점 헤더 바
            float topH = 68f * scale;
            GUI.color = new Color(0.04f, 0.06f, 0.11f, 0.92f);
            GUI.DrawTexture(new Rect(0, 0, sw, topH), _whiteTex);
            GUI.color = new Color(0.3f, 0.75f, 0.9f, 0.7f);
            GUI.DrawTexture(new Rect(0, topH - 2f, sw, 2f), _whiteTex);

            GUIStyle headerTitleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = Mathf.RoundToInt(22f * scale),
                fontStyle = FontStyle.Bold
            };
            headerTitleStyle.normal.textColor = new Color(1f, 0.9f, 0.45f);
            GUI.Label(new Rect(24f * scale, 12f * scale, 480f * scale, 44f * scale), "⛩️ 곡도(鵠島) 사당 거점 — 보옥의 결계 우물가", headerTitleStyle);

            // 상단 재화/상태 표시 (기억의 조각, 인연 포인트, 재도전 가호)
            int shards = save != null ? save.memoryShards : 4;
            int totalShards = save != null ? save.totalCollectedShards : 4;
            int blessing = save != null ? save.retryBlessingStacks : 0;

            GUIStyle statStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.RoundToInt(16f * scale),
                fontStyle = FontStyle.Bold
            };
            statStyle.normal.textColor = new Color(0.4f, 0.95f, 1f);
            string statSummary = $"💎 기억의 조각: {shards}개 (누적 {totalShards}/22)    |    ✨ 재도전 가호: {blessing}단계 (피해감면 +{blessing * 3}%, 보호막 +{blessing * 5}%)";
            GUI.Label(new Rect(sw * 0.27f, 12f * scale, sw * 0.44f, 44f * scale), statSummary, statStyle);

            // 테스트용 기억의 조각 +5 버튼 & 타이틀 복귀 버튼 & 즉시 출격 버튼
            GUIStyle topBtnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = Mathf.RoundToInt(14f * scale),
                fontStyle = FontStyle.Bold
            };

            GUI.color = new Color(0.3f, 0.85f, 0.95f);
            if (GUI.Button(new Rect(sw - 470f * scale, 14f * scale, 135f * scale, 40f * scale), "💎 조각 +5 (테스트)", topBtnStyle))
            {
                if (save != null)
                {
                    save.memoryShards += 5;
                    save.totalCollectedShards = Mathf.Min(22, save.totalCollectedShards + 5);
                    GameManager.Instance.SaveGame();
                }
            }

            GUI.color = new Color(0.75f, 0.78f, 0.85f);
            if (GUI.Button(new Rect(sw - 325f * scale, 14f * scale, 110f * scale, 40f * scale), "🏠 타이틀 화면", topBtnStyle))
            {
                _isTitleScreen = true;
            }

            GUI.color = new Color(0.25f, 0.95f, 0.55f);
            var curStage = _stages[Mathf.Clamp(_selectedStageIndex, 0, _stages.Count - 1)];
            if (GUI.Button(new Rect(sw - 205f * scale, 12f * scale, 185f * scale, 44f * scale), $"🏹 미궁 출격 [{curStage.stageCode}]", topBtnStyle))
            {
                LaunchSelectedStage();
            }

            // 2. 좌측/상단 3대 탭 전환 버튼 (미궁 출격 / 기억의 우물 / 기억 열람)
            float tabY = topH + 14f * scale;
            float tabW = 280f * scale;
            float tabH = 44f * scale;
            float tabGap = 14f * scale;
            float tabStartX = 24f * scale;

            DrawTabButton(new Rect(tabStartX, tabY, tabW, tabH), OutgameTab.StageSelect, "🗺️ 1. 미궁 출격 (챕터 0~6 선택)", scale);
            DrawTabButton(new Rect(tabStartX + (tabW + tabGap), tabY, tabW, tabH), OutgameTab.MemoryWell, "💧 2. 기억의 우물 (영구 스탯 강화)", scale);
            DrawTabButton(new Rect(tabStartX + (tabW + tabGap) * 2f, tabY, tabW, tabH), OutgameTab.MemoryArchive, "📜 3. 기억 열람 (핵심 기억 & 서사)", scale);

            // 3. 메인 콘텐츠 영역
            float contentY = tabY + tabH + 10f * scale;
            float bottomDialogueH = 155f * scale;
            float contentH = sh - contentY - bottomDialogueH - 20f * scale;
            Rect contentRect = new Rect(24f * scale, contentY, sw - 48f * scale, contentH);

            GUI.color = new Color(0.05f, 0.07f, 0.12f, 0.84f);
            GUI.DrawTexture(contentRect, _whiteTex);
            GUI.color = new Color(0.25f, 0.55f, 0.75f, 0.75f);
            DrawRectBorder(contentRect, 1.5f);

            switch (_currentTab)
            {
                case OutgameTab.StageSelect:
                    DrawStageSelectTab(contentRect, scale);
                    break;
                case OutgameTab.MemoryWell:
                    DrawMemoryWellTab(contentRect, scale);
                    break;
                case OutgameTab.MemoryArchive:
                    DrawMemoryArchiveTab(contentRect, scale);
                    break;
            }

            // 4. 하단 곡도 사당 인연 대화창 (보옥 / 노승 부부 / 해로 / 호원)
            Rect dlgRect = new Rect(24f * scale, sh - bottomDialogueH - 10f * scale, sw - 48f * scale, bottomDialogueH);
            DrawShrineDialogueBox(dlgRect, scale);

            GUI.color = prev;
        }

        private void DrawTabButton(Rect rect, OutgameTab tab, string label, float scale)
        {
            bool active = _currentTab == tab;
            GUI.color = active ? new Color(0.2f, 0.85f, 0.98f, 1f) : new Color(0.18f, 0.22f, 0.30f, 0.92f);
            GUIStyle style = new GUIStyle(GUI.skin.button)
            {
                fontSize = Mathf.RoundToInt(16f * scale),
                fontStyle = active ? FontStyle.Bold : FontStyle.Normal
            };
            if (GUI.Button(rect, label, style))
            {
                _currentTab = tab;
            }
        }

        private void DrawStageSelectTab(Rect area, float scale)
        {
            float pad = 16f * scale;
            // 좌측: 챕터 필터 (챕터 0 ~ 챕터 6) 및 스테이지 리스트
            float leftW = area.width * 0.48f;
            Rect leftRect = new Rect(area.x + pad, area.y + pad, leftW - pad, area.height - pad * 2f);

            // 우측: 선택된 스테이지 상세 브리핑 & 출격 버튼
            Rect rightRect = new Rect(area.x + leftW + pad, area.y + pad, area.width - leftW - pad * 2f, area.height - pad * 2f);

            // 챕터 선택 버튼 행 (Ch.0 ~ Ch.6)
            float chBtnW = (leftRect.width - 6f * 6f) / 7f;
            float chBtnH = 36f * scale;
            GUIStyle chStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = Mathf.RoundToInt(13f * scale),
                fontStyle = FontStyle.Bold
            };

            for (int ch = 0; ch <= 6; ch++)
            {
                bool isChSelected = _selectedChapterFilter == ch;
                GUI.color = isChSelected ? new Color(1f, 0.85f, 0.3f, 1f) : new Color(0.22f, 0.26f, 0.35f, 0.95f);
                Rect r = new Rect(leftRect.x + ch * (chBtnW + 6f), leftRect.y, chBtnW, chBtnH);
                string chName = ch == 0 ? "Ch.0 서장" : $"Ch.{ch}";
                if (GUI.Button(r, chName, chStyle))
                {
                    _selectedChapterFilter = ch;
                    int firstIdx = _stages.FindIndex(s => s.chapter == ch);
                    if (firstIdx >= 0) _selectedStageIndex = firstIdx;
                }
            }

            // 해당 챕터의 스테이지 목록 표시
            float listY = leftRect.y + chBtnH + 12f * scale;
            float listH = leftRect.height - chBtnH - 12f * scale;
            Rect listRect = new Rect(leftRect.x, listY, leftRect.width, listH);

            List<int> matchingIndices = new();
            for (int i = 0; i < _stages.Count; i++)
            {
                if (_stages[i].chapter == _selectedChapterFilter)
                {
                    matchingIndices.Add(i);
                }
            }

            float itemH = 68f * scale;
            Rect viewRect = new Rect(0, 0, listRect.width - 20f, Mathf.Max(listH, matchingIndices.Count * (itemH + 8f)));
            _stageScrollPos = GUI.BeginScrollView(listRect, _stageScrollPos, viewRect);

            GUIStyle stageBtnStyle = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = Mathf.RoundToInt(15f * scale),
                fontStyle = FontStyle.Bold,
                padding = new RectOffset(14, 14, 8, 8)
            };

            for (int k = 0; k < matchingIndices.Count; k++)
            {
                int idx = matchingIndices[k];
                var st = _stages[idx];
                bool selected = idx == _selectedStageIndex;
                GUI.color = selected ? new Color(0.2f, 0.88f, 0.95f, 1f) : new Color(0.14f, 0.18f, 0.25f, 0.95f);

                string label = $"  [{st.stageCode}]  {st.stageTitle}\n  └ 주요 표적: {st.bossOrKeeperName}";
                if (GUI.Button(new Rect(0, k * (itemH + 8f), viewRect.width, itemH), label, stageBtnStyle))
                {
                    _selectedStageIndex = idx;
                    if (GameManager.Instance != null && GameManager.Instance.SaveData != null)
                    {
                        GameManager.Instance.SaveData.currentChapter = st.chapter;
                        GameManager.Instance.SaveData.currentStage = st.globalStageNumber;
                        GameManager.Instance.SaveData.selectedStageCode = st.stageCode;
                    }
                }
            }

            GUI.EndScrollView();

            // 우측 브리핑 카드
            GUI.color = new Color(0.08f, 0.11f, 0.18f, 0.95f);
            GUI.DrawTexture(rightRect, _whiteTex);
            GUI.color = new Color(0.85f, 0.7f, 0.28f, 0.8f);
            DrawRectBorder(rightRect, 1.5f);

            var current = _stages[Mathf.Clamp(_selectedStageIndex, 0, _stages.Count - 1)];
            float rx = rightRect.x + 20f * scale;
            float ry = rightRect.y + 18f * scale;
            float rw = rightRect.width - 40f * scale;

            GUIStyle hStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(22f * scale),
                fontStyle = FontStyle.Bold
            };
            hStyle.normal.textColor = new Color(1f, 0.9f, 0.4f);
            GUI.Label(new Rect(rx, ry, rw, 34f * scale), $"📍 Stage {current.stageCode} : {current.stageTitle}", hStyle);

            GUIStyle bodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(15f * scale),
                wordWrap = true
            };
            bodyStyle.normal.textColor = new Color(0.9f, 0.95f, 1f);

            GUI.Label(new Rect(rx, ry + 42f * scale, rw, 28f * scale), $"<b>• 수문장 / 보스:</b> <color=#FF9966>{current.bossOrKeeperName}</color>", bodyStyle);
            GUI.Label(new Rect(rx, ry + 76f * scale, rw, 56f * scale), $"<b>• 핵심 전투 기믹:</b>\n{current.gimmickSummary}", bodyStyle);
            GUI.Label(new Rect(rx, ry + 142f * scale, rw, 70f * scale), $"<b>• 시나리오 기록 (기획서 v1.1b):</b>\n<i>“{current.scenarioNote}”</i>", bodyStyle);

            var save = GameManager.Instance?.SaveData;
            string eqMemId = save != null ? save.equippedCoreMemoryId : "MEM_CH1_FIRST_STRING";
            var eqMem = _coreMemories.Find(m => m.id == eqMemId) ?? _coreMemories[0];
            GUI.Label(new Rect(rx, ry + 220f * scale, rw, 30f * scale), $"<b>• 장착 중인 핵심 기억:</b> <color=#66EEFF>{eqMem.title}</color>", bodyStyle);

            // 대형 출격 버튼
            GUIStyle launchStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = Mathf.RoundToInt(20f * scale),
                fontStyle = FontStyle.Bold
            };
            GUI.color = new Color(0.22f, 0.95f, 0.55f, 1f);
            if (GUI.Button(new Rect(rx, rightRect.yMax - 68f * scale, rw, 52f * scale), $"🏹 Stage {current.stageCode} 미궁으로 출격 (PlayMain.scene 진입)", launchStyle))
            {
                LaunchSelectedStage();
            }
        }

        private void DrawMemoryWellTab(Rect area, float scale)
        {
            var save = GameManager.Instance != null ? GameManager.Instance.SaveData : null;
            if (save == null) return;

            float pad = 20f * scale;
            float x = area.x + pad;
            float y = area.y + pad;
            float w = area.width - pad * 2f;

            GUIStyle headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(20f * scale),
                fontStyle = FontStyle.Bold
            };
            headerStyle.normal.textColor = new Color(0.4f, 0.95f, 1f);
            GUI.Label(new Rect(x, y, w * 0.7f, 32f * scale), $"💧 기억의 우물 — 거타지의 잃어버린 무예와 보옥의 공명 복원 (보유 기억의 조각: {save.memoryShards}개)", headerStyle);

            // 100% 환급 초기화 버튼
            GUI.color = new Color(0.95f, 0.65f, 0.25f, 1f);
            GUIStyle resetBtnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = Mathf.RoundToInt(14f * scale),
                fontStyle = FontStyle.Bold
            };
            if (GUI.Button(new Rect(area.xMax - 290f * scale, y, 270f * scale, 34f * scale), "🔄 우물 강화 전체 초기화 (100% 환급)", resetBtnStyle))
            {
                int refunded = save.wellBowLevel + save.wellDaggerLevel + save.wellBookLevel + save.wellSurvivalLevel;
                save.wellBowLevel = 0;
                save.wellDaggerLevel = 0;
                save.wellBookLevel = 0;
                save.wellSurvivalLevel = 0;
                save.memoryShards += refunded;
                GameManager.Instance.SaveGame();
                _activeSpeakerName = "보옥 (보석 토끼)";
                _activeDialogueText = $"“우물에 깃든 기억의 흐름을 정화했어요. 돌려받은 {refunded}개의 기억 조각으로 다시 조합해 봐요.”";
            }

            float cardW = (w - 30f * scale) * 0.5f;
            float cardH = (area.height - 90f * scale) * 0.47f;
            float startY = y + 46f * scale;

            DrawWellCategoryCard(
                new Rect(x, startY, cardW, cardH),
                "🏹 1. 활(弓) 계열 — 사격술의 기억",
                "화살 기본 위력 +4/Lv, 완전 충전 속도 및 약점(기억의 핵) 관통력 강화",
                ref save.wellBowLevel,
                new Color(0.2f, 0.85f, 1f),
                scale);

            DrawWellCategoryCard(
                new Rect(x + cardW + 30f * scale, startY, cardW, cardH),
                "🗡️ 2. 단검(短劍) 계열 — 되돌리기의 기억",
                "단검 베기 위력 +6/Lv, 패링 판정 유예 증가 및 반사 투사체 피해 +15%/Lv",
                ref save.wellDaggerLevel,
                new Color(0.3f, 0.95f, 0.65f),
                scale);

            DrawWellCategoryCard(
                new Rect(x, startY + cardH + 16f * scale, cardW, cardH),
                "🐰 3. 보옥(寶玉) 계열 — 여의보주의 공명",
                "영혼석(보석) 정제 효율 증가, 보옥 보호막 강화, 생명 하트 추가 회복 +4/Lv",
                ref save.wellBookLevel,
                new Color(0.95f, 0.78f, 0.25f),
                scale);

            DrawWellCategoryCard(
                new Rect(x + cardW + 30f * scale, startY + cardH + 16f * scale, cardW, cardH),
                "🛡️ 4. 생존(生存) 계열 — 화랑의 기개",
                "거타지 최대 체력 +15/Lv, 슬라이딩(무적 회피) 재사용 대기시간 -0.08초/Lv",
                ref save.wellSurvivalLevel,
                new Color(0.95f, 0.45f, 0.45f),
                scale);
        }

        private void DrawWellCategoryCard(Rect rect, string title, string desc, ref int level, Color accentColor, float scale)
        {
            var save = GameManager.Instance.SaveData;
            GUI.color = new Color(0.09f, 0.12f, 0.20f, 0.96f);
            GUI.DrawTexture(rect, _whiteTex);
            GUI.color = accentColor;
            DrawRectBorder(rect, 1.5f);

            float px = rect.x + 16f * scale;
            float py = rect.y + 12f * scale;
            float pw = rect.width - 32f * scale;

            GUIStyle tStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(17f * scale),
                fontStyle = FontStyle.Bold
            };
            tStyle.normal.textColor = accentColor;
            GUI.Label(new Rect(px, py, pw, 26f * scale), $"{title}   [ Lv.{level} / 5 ]", tStyle);

            GUIStyle dStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(14f * scale),
                wordWrap = true
            };
            dStyle.normal.textColor = new Color(0.88f, 0.92f, 0.98f);
            GUI.Label(new Rect(px, py + 30f * scale, pw, 42f * scale), desc, dStyle);

            bool canUpgrade = level < 5 && save.memoryShards >= 1;
            GUI.color = canUpgrade ? accentColor : new Color(0.35f, 0.38f, 0.42f, 0.8f);
            GUIStyle btnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = Mathf.RoundToInt(15f * scale),
                fontStyle = FontStyle.Bold
            };

            string btnText = level >= 5 ? "★ 최고 단계 달성 (MAX)" : $"💧 기억의 조각 1개로 강화하기 (Lv.{level} → Lv.{level + 1})";
            if (GUI.Button(new Rect(px, rect.yMax - 44f * scale, pw, 34f * scale), btnText, btnStyle) && canUpgrade)
            {
                save.memoryShards -= 1;
                level += 1;
                GameManager.Instance.SaveGame();
                _activeSpeakerName = "보옥 (보석 토끼)";
                _activeDialogueText = $"“기억의 우물이 공명하며 거타지 님의 몸에 옛 감각이 되살아났어요! ({title} Lv.{level})”";
            }
        }

        private void DrawMemoryArchiveTab(Rect area, float scale)
        {
            var save = GameManager.Instance?.SaveData;
            float pad = 16f * scale;
            float leftW = area.width * 0.45f;
            Rect leftRect = new Rect(area.x + pad, area.y + pad, leftW - pad, area.height - pad * 2f);
            Rect rightRect = new Rect(area.x + leftW + pad, area.y + pad, area.width - leftW - pad * 2f, area.height - pad * 2f);

            GUIStyle subHeader = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(17f * scale),
                fontStyle = FontStyle.Bold
            };
            subHeader.normal.textColor = new Color(1f, 0.88f, 0.38f);
            GUI.Label(new Rect(leftRect.x, leftRect.y, leftRect.width, 28f * scale), "📜 6대 핵심 기억 (Core Memory Traits — 1종 선택 장착)", subHeader);

            float btnH = 46f * scale;
            float startY = leftRect.y + 34f * scale;

            GUIStyle itemStyle = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = Mathf.RoundToInt(14f * scale),
                fontStyle = FontStyle.Bold,
                padding = new RectOffset(12, 12, 6, 6)
            };

            for (int i = 0; i < _coreMemories.Count; i++)
            {
                var mem = _coreMemories[i];
                bool isEquipped = save != null && save.equippedCoreMemoryId == mem.id;
                bool isSelected = i == _selectedArchiveIndex;

                GUI.color = isEquipped
                    ? new Color(0.25f, 0.95f, 0.65f, 1f)
                    : (isSelected ? new Color(0.25f, 0.82f, 0.98f, 1f) : new Color(0.16f, 0.20f, 0.28f, 0.95f));

                string prefix = isEquipped ? "★ [장착 중] " : "• ";
                if (GUI.Button(new Rect(leftRect.x, startY + i * (btnH + 6f * scale), leftRect.width, btnH), prefix + mem.title, itemStyle))
                {
                    _selectedArchiveIndex = i;
                }
            }

            // 우측: 선택한 핵심 기억 상세 효과 & 장착 버튼 + 시나리오 진실 기록
            GUI.color = new Color(0.08f, 0.11f, 0.18f, 0.95f);
            GUI.DrawTexture(rightRect, _whiteTex);
            GUI.color = new Color(0.35f, 0.8f, 0.95f, 0.8f);
            DrawRectBorder(rightRect, 1.5f);

            var curMem = _coreMemories[Mathf.Clamp(_selectedArchiveIndex, 0, _coreMemories.Count - 1)];
            float rx = rightRect.x + 20f * scale;
            float ry = rightRect.y + 18f * scale;
            float rw = rightRect.width - 40f * scale;

            GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(21f * scale),
                fontStyle = FontStyle.Bold
            };
            titleStyle.normal.textColor = new Color(0.35f, 0.95f, 1f);
            GUI.Label(new Rect(rx, ry, rw, 32f * scale), curMem.title, titleStyle);

            GUIStyle textStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(15f * scale),
                wordWrap = true
            };
            textStyle.normal.textColor = new Color(0.92f, 0.95f, 1f);

            GUI.Label(new Rect(rx, ry + 38f * scale, rw, 26f * scale), $"<b>• 해금 조건:</b> {curMem.unlockCondition}", textStyle);
            GUI.Label(new Rect(rx, ry + 70f * scale, rw, 48f * scale), $"<b>• 핵심 기억 고유 효과:</b>\n<color=#80FFAA>{curMem.effectDescription}</color>", textStyle);
            GUI.Label(new Rect(rx, ry + 126f * scale, rw, 60f * scale), $"<b>• 회상 독백:</b>\n<i>{curMem.loreQuote}</i>", textStyle);

            // 진엔딩 서사 요약 안내
            GUI.Label(new Rect(rx, ry + 195f * scale, rw, 65f * scale),
                "<b>• [기억의 재구성 서사 구조]:</b>\n" +
                "거타지가 기억의 조각을 모을수록 '반역자 거타지'라는 왜곡된 기억이 벗겨지고, 여왕의 은밀한 어명과 여의보주 보옥의 진실이 복원됩니다.",
                textStyle);

            bool alreadyEquipped = save != null && save.equippedCoreMemoryId == curMem.id;
            GUI.color = alreadyEquipped ? new Color(0.3f, 0.6f, 0.45f, 0.9f) : new Color(1f, 0.85f, 0.3f, 1f);
            GUIStyle eqBtnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = Mathf.RoundToInt(17f * scale),
                fontStyle = FontStyle.Bold
            };
            string eqLabel = alreadyEquipped ? "✔ 현재 대표 핵심 기억으로 장착 중" : "✨ 이 핵심 기억을 대표 특성으로 장착하기";
            if (GUI.Button(new Rect(rx, rightRect.yMax - 56f * scale, rw, 42f * scale), eqLabel, eqBtnStyle) && save != null)
            {
                save.equippedCoreMemoryId = curMem.id;
                if (!save.unlockedCoreMemoryIds.Contains(curMem.id))
                {
                    save.unlockedCoreMemoryIds.Add(curMem.id);
                }
                GameManager.Instance.SaveGame();
                _activeSpeakerName = "거타지 (명궁)";
                _activeDialogueText = curMem.loreQuote;
            }
        }

        private void DrawShrineDialogueBox(Rect rect, float scale)
        {
            GUI.color = new Color(0.03f, 0.05f, 0.09f, 0.94f);
            GUI.DrawTexture(rect, _whiteTex);
            GUI.color = new Color(0.85f, 0.72f, 0.32f, 0.85f);
            DrawRectBorder(rect, 1.5f);

            float px = rect.x + 18f * scale;
            float py = rect.y + 12f * scale;

            // 좌측: 현재 화자 & 대사
            GUIStyle speakerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(17f * scale),
                fontStyle = FontStyle.Bold
            };
            speakerStyle.normal.textColor = new Color(1f, 0.88f, 0.35f);
            GUI.Label(new Rect(px, py, 380f * scale, 26f * scale), $"💬 [{_activeSpeakerName}]", speakerStyle);

            GUIStyle lineStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(16f * scale),
                wordWrap = true
            };
            lineStyle.normal.textColor = new Color(0.94f, 0.97f, 1f);
            GUI.Label(new Rect(px, py + 30f * scale, rect.width * 0.62f, rect.height - 44f * scale), _activeDialogueText, lineStyle);

            // 우측: 곡도 사당 거점 NPC 대화 버튼 4종
            float npcAreaX = rect.x + rect.width * 0.64f;
            float npcAreaW = rect.width * 0.34f;
            float btnW = (npcAreaW - 10f * scale) * 0.5f;
            float btnH = 42f * scale;

            GUIStyle npcBtnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = Mathf.RoundToInt(13f * scale),
                fontStyle = FontStyle.Bold
            };

            GUI.color = new Color(0.25f, 0.88f, 0.95f);
            if (GUI.Button(new Rect(npcAreaX, py + 12f * scale, btnW, btnH), "🐰 보옥 (보석 토끼) 대화", npcBtnStyle))
            {
                _activeSpeakerName = "보옥 (보석 토끼 / 여의보주)";
                string[] lines =
                {
                    "“흥, 이번엔 봐줄게요. 다음엔 없어요. 다치기 전에 단검으로 제때 흘려보내라고 했잖아요!”",
                    "“제가 떨어뜨린 영혼석을 주우러 갈 땐, 주변의 요괴부터 정리해 주세요. 저는 전투용 토끼가 아니란 말이에요!”",
                    "“우물 속을 들여다보면 당신이 잃어버린 활의 기억이 보일 거예요... 물론 제 덕분이겠지만요.”"
                };
                _activeDialogueText = lines[UnityEngine.Random.Range(0, lines.Length)];
            }

            GUI.color = new Color(0.45f, 0.85f, 0.65f);
            if (GUI.Button(new Rect(npcAreaX + btnW + 10f * scale, py + 12f * scale, btnW, btnH), "🐉 노승 부부 (용왕) 대화", npcBtnStyle))
            {
                _activeSpeakerName = "노승 부부 (서해 용왕의 화신)";
                _activeDialogueText = "“젊은 활잡이여, 천년 묵은 여우가 바다와 사당의 경계를 허물고 있소. 그대의 활시위만이 우리 일족과 저 아이를 구할 수 있소.”";
            }

            GUI.color = new Color(0.65f, 0.78f, 0.95f);
            if (GUI.Button(new Rect(npcAreaX, py + 18f * scale + btnH, btnW, btnH), "🐢 해로 (바다거북) 대화", npcBtnStyle))
            {
                _activeSpeakerName = "해로 (늙은 바다거북)";
                _activeDialogueText = "“허허, 폭풍우가 몰아치던 밤에도 그대의 손은 활을 놓지 않았더군. 미궁의 물길은 매번 바뀌니 보옥의 빛을 길잡이 삼으시게.”";
            }

            GUI.color = new Color(0.95f, 0.78f, 0.45f);
            if (GUI.Button(new Rect(npcAreaX + btnW + 10f * scale, py + 18f * scale + btnH, btnW, btnH), "⚔️ 호원 (화랑 후배) 대화", npcBtnStyle))
            {
                _activeSpeakerName = "호원 (화랑도 후배)";
                _activeDialogueText = "“형님! 서라벌에는 형님이 여왕 폐하를 시해하려 했다는 거짓 소문이 퍼졌습니다. 반드시 진실을 되찾아 돌아가야 합니다!”";
            }
        }

        public void LaunchSelectedStage()
        {
            var st = _stages[Mathf.Clamp(_selectedStageIndex, 0, _stages.Count - 1)];
            if (GameManager.Instance != null)
            {
                GameManager.Instance.LaunchStageToPlayMain(st.chapter, st.globalStageNumber, st.stageCode);
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(GameManager.PlayMainSceneName);
            }
        }

        private void DrawRectBorder(Rect r, float thickness)
        {
            GUI.DrawTexture(new Rect(r.x, r.y, r.width, thickness), _whiteTex);
            GUI.DrawTexture(new Rect(r.x, r.yMax - thickness, r.width, thickness), _whiteTex);
            GUI.DrawTexture(new Rect(r.x, r.y, thickness, r.height), _whiteTex);
            GUI.DrawTexture(new Rect(r.xMax - thickness, r.y, thickness, r.height), _whiteTex);
        }
    }
}
