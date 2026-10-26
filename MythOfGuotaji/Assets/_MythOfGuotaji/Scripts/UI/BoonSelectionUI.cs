using System;
using System.Collections.Generic;
using MythOfGuotaji.Progression;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using UnityEngine.Playables;


namespace MythOfGuotaji.UI
{
    /// <summary>
    /// UGUI 기반 3지선다 비술(Boon) 레벨업 선택 모달 창
    /// - New Input System 완벽 호환 (InputSystemUIInputModule 보장 및 단축키 1, 2, 3 지원)
    /// - 프리팹 위치: Assets/_MythOfGuotaji/Resources/Prefabs/GUI/BoonSelectionUI.prefab
    /// - 희귀도 4단계 컬러링 지원 (Common: 흰색, Rare: 파란색, Epic: 보라색, Legendary: 노란색/금색)
    /// - 카드 전체 영역 클릭 및 하단 선택 버튼 클릭 지원
    /// - 텍스트/장식 이미지 raycastTarget 간섭 원천 차단
    /// </summary>
    public class BoonSelectionUI : MonoBehaviour
    {
        public static BoonSelectionUI Instance { get; private set; }
        public static bool IsShowing => Instance != null && Instance._isShowing;

        [Header("UI Structure")]
        [SerializeField] private GameObject _panelRoot;          // 모달 배경 및 컨테이너
        [SerializeField] private Text _headerTitleText;          // 상단 안내 타이틀
        [SerializeField] private List<BoonCardSlotUI> _cardSlots = new(); // 3개 선택지 카드 슬롯
        [SerializeField] private PlayableDirector _cardsOnDirector = null;
        private List<InRunBoonSO> _currentChoices;
        private bool _isShowing = false;

        public static void EnsureInstance()
        {
            if (Instance != null) return;

            // 1. New Input System EventSystem 보장 (클릭 및 입력 모듈 활성화)
            EnsureEventSystem();

            var existing = FindAnyObjectByType<BoonSelectionUI>();
            if (existing != null)
            {
                Instance = existing;
                return;
            }

            var prefab = Data.GameDataManager.Instance?.Prefabs?.boonSelectionUIPrefab 
                      ?? Resources.Load<GameObject>("Prefabs/GUI/BoonSelectionUI");
            if (prefab != null)
            {
                var go = Instantiate(prefab);
                go.name = "BoonSelectionUI";
            }
            else
            {
                Debug.LogWarning("[BoonSelectionUI] 'Prefabs/GUI/BoonSelectionUI' 프리팹을 찾지 못했습니다! PrefabDatabase 또는 Resources/Prefabs/GUI/BoonSelectionUI 프리팹을 확인하세요.");
            }
        }

        public static void EnsureEventSystem()
        {
            var es = FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>();
            if (es == null)
            {
                var esGo = new GameObject("[EventSystem]");
                es = esGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esGo.AddComponent<InputSystemUIInputModule>();
            }
            else
            {
                var standalone = es.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>();
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
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (_panelRoot == null)
            {
                _panelRoot = transform.Find("ModalPanel")?.gameObject ?? gameObject;
            }

            // 혹시 직렬화된 _cardSlots 가 비어있거나 누락되었을 때 자동 자식 탐색
            if (_cardSlots == null || _cardSlots.Count == 0 || _cardSlots[0] == null)
            {
                _cardSlots = new List<BoonCardSlotUI>(GetComponentsInChildren<BoonCardSlotUI>(true));
            }

            Hide();
        }

        private void Start()
        {
            EnsureEventSystem();

            if (InRunProgressionManager.Instance != null)
            {
                InRunProgressionManager.Instance.OnBoonSelectionRequested += Show;
            }
        }

        private void Update()
        {
            if (!_isShowing || _currentChoices == null) return;

            // 키보드 숫자키 (1, 2, 3 및 키패드 1, 2, 3) 단축키 처리
            var kb = Keyboard.current;
            if (kb != null)
            {
                if ((kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame) && _currentChoices.Count > 0)
                {
                    OnCardClicked(_currentChoices[0]);
                }
                else if ((kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame) && _currentChoices.Count > 1)
                {
                    OnCardClicked(_currentChoices[1]);
                }
                else if ((kb.digit3Key.wasPressedThisFrame || kb.numpad3Key.wasPressedThisFrame) && _currentChoices.Count > 2)
                {
                    OnCardClicked(_currentChoices[2]);
                }
            }
        }

        private void OnDestroy()
        {
            if (InRunProgressionManager.Instance != null)
            {
                InRunProgressionManager.Instance.OnBoonSelectionRequested -= Show;
            }
            if (Instance == this) Instance = null;
        }

        /// <summary>
        /// 3지선다 축복 카드 UI 표시 및 게임 일시 정지, 커서 활성화
        /// </summary>
        public void Show(List<InRunBoonSO> choices)
        {
            if (choices == null || choices.Count == 0) return;

            EnsureEventSystem();

            _currentChoices = choices;
            _isShowing = true;
            Time.timeScale = 0f; // 선택 중 일시 정지

            // UI용 일반 커서 및 잠금 해제
            if (AimCursorManager.Instance != null)
            {
                AimCursorManager.Instance.SetNormalCursorForUI();
            }
            else
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }

            if (_panelRoot != null)
            {
                _panelRoot.SetActive(true);
                if (_cardsOnDirector != null)
                {
                    _cardsOnDirector.Stop();
                    _cardsOnDirector.Evaluate();
                    _cardsOnDirector.Play();
                }
            }

            if (_headerTitleText != null)
            {
                _headerTitleText.text = "✨ [동료 헬퍼] 영혼석 정제 완료! 새로운 비술을 선택하세요 (키보드 1, 2, 3 가능)";
            }

            // 슬롯 리스트 검증 및 보정
            if (_cardSlots == null || _cardSlots.Count == 0 || _cardSlots[0] == null)
            {
                _cardSlots = new List<BoonCardSlotUI>(GetComponentsInChildren<BoonCardSlotUI>(true));
            }

            for (int i = 0; i < _cardSlots.Count; i++)
            {
                if (_cardSlots[i] == null) continue;

                if (i < choices.Count)
                {
                    _cardSlots[i].gameObject.SetActive(true);
                    _cardSlots[i].Setup(choices[i], OnCardClicked, i + 1);
                }
                else
                {
                    _cardSlots[i].gameObject.SetActive(false);
                }
            }
        }

        public void Hide()
        {
            _isShowing = false;
            _currentChoices = null;

            if (_panelRoot != null)
            {
                _panelRoot.SetActive(false);
            }

            // 인게임 에임 커서 및 Confined 영역 제한 복원
            AimCursorManager.Instance?.RestoreAimCursor();
        }

        private void OnCardClicked(InRunBoonSO selectedBoon)
        {
            if (!_isShowing || selectedBoon == null) return;

            Hide();
            Time.timeScale = 1f; // 게임 재개

            InRunProgressionManager.Instance?.SelectBoon(selectedBoon);
            Debug.Log($"<color=cyan>[BoonSelectionUI] 비술 선택 완료: {selectedBoon.boonTitle}</color>");
        }

        public void BindComponents(GameObject panelRoot, Text headerText, List<BoonCardSlotUI> slots)
        {
            _panelRoot = panelRoot;
            _headerTitleText = headerText;
            _cardSlots = slots;
        }
    }
}
