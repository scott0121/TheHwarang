using System.Collections.Generic;
using MythOfGuotaji.Actor;
using MythOfGuotaji.UI.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MythOfGuotaji.UI
{
    /// <summary>
    /// 모든 몬스터 머리 위에 1920x1080 기준 40px 규격의 미니 체력바를 풀링하여 일괄 렌더링하는 UGUI 매니저
    /// </summary>
    public class MonsterHealthBarManager : MonoBehaviour
    {
        public static MonsterHealthBarManager Instance { get; private set; }

        private readonly HashSet<MonsterAI> _activeMonsters = new();
        private Camera _cam;
        private Transform _playerTransform;

        [Header("Health Bar Dimensions (1080p standard: 40px)")]
        [SerializeField] private float _baseWidth = 40f;
        [SerializeField] private float _baseHeight = 5.5f;
        [SerializeField] private float _maxVisibleDistance = 32f;

        // UGUI Pooling
        private Canvas _canvas;
        private RectTransform _canvasRoot;
        private readonly List<BarItem> _pool = new();

        private class BarItem
        {
            public GameObject gameObject;
            public RectTransform rootRect;
            public Image fillImage;
            public Image bgImage;
            public Image borderImage;
            public TextMeshProUGUI levelLabel;
            public bool inUse;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            _cam = Camera.main;
            BuildCanvas();
        }

        private void BuildCanvas()
        {
            if (_canvas != null) return;
            _canvas = UIKit.CreateCanvas("[MonsterHealthBars_Canvas]", UITheme.Sorting.HUD - 1, transform, false);
            _canvasRoot = (RectTransform)_canvas.transform;
        }

        private BarItem CreateBarItem()
        {
            var item = new BarItem();
            var go = new GameObject("MonBarItem", typeof(RectTransform));
            go.transform.SetParent(_canvasRoot, false);

            item.gameObject = go;
            item.rootRect = (RectTransform)go.transform;
            item.rootRect.sizeDelta = new Vector2(_baseWidth, _baseHeight);
            item.rootRect.pivot = new Vector2(0.5f, 0.5f);

            // 외곽선
            item.borderImage = UIKit.CreatePanel(item.rootRect, "Border", new Color(0.04f, 0.04f, 0.07f, 0.88f), true);
            UIKit.Stretch(item.borderImage.rectTransform, -1f, -1f, -1f, -1f);

            // 소실 체력 배경 (다크 레드)
            item.bgImage = UIKit.CreatePanel(item.rootRect, "Bg", new Color(0.25f, 0.05f, 0.05f, 0.82f), true);

            // 채움 바
            var fillObj = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            fillObj.transform.SetParent(item.rootRect, false);
            item.fillImage = fillObj.GetComponent<Image>();
            item.fillImage.sprite = UIKit.WhiteSprite;
            item.fillImage.type = Image.Type.Filled;
            item.fillImage.fillMethod = Image.FillMethod.Horizontal;
            item.fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            item.fillImage.fillAmount = 1f;
            item.fillImage.raycastTarget = false;
            UIKit.Stretch((RectTransform)fillObj.transform);

            // 레벨 라벨
            item.levelLabel = UIKit.CreateLabel(item.rootRect, "LevelLabel", "", 10f, new Color(1f, 0.85f, 0.2f, 0.95f), TextAlignmentOptions.Right, FontStyles.Bold, false, false);
            UIKit.SetAnchored(item.levelLabel.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-4f, 0f), new Vector2(40f, 16f));

            go.SetActive(false);
            _pool.Add(item);
            return item;
        }

        public static void Register(MonsterAI monster)
        {
            if (monster == null) return;
            if (Instance == null)
            {
                var go = new GameObject("[MonsterHealthBarManager]");
                Instance = go.AddComponent<MonsterHealthBarManager>();
            }
            Instance._activeMonsters.Add(monster);
        }

        public static void Unregister(MonsterAI monster)
        {
            if (monster == null || Instance == null) return;
            Instance._activeMonsters.Remove(monster);
        }

        public void ClearAll()
        {
            _activeMonsters.Clear();
            for (int i = 0; i < _pool.Count; i++)
            {
                _pool[i].gameObject.SetActive(false);
                _pool[i].inUse = false;
            }
        }

        private void Update()
        {
            if (_cam == null) _cam = Camera.main;
            if (_playerTransform == null)
            {
                var p = FindAnyObjectByType<PlayerController>();
                if (p != null) _playerTransform = p.transform;
            }
        }

        private void LateUpdate()
        {
            if (_cam == null || _activeMonsters.Count == 0 || _canvasRoot == null)
            {
                HideAllPoolItems();
                return;
            }

            Vector3 playerPos = _playerTransform != null ? _playerTransform.position : Vector3.zero;
            float maxDistSq = _maxVisibleDistance * _maxVisibleDistance;

            int poolIndex = 0;

            foreach (var monster in _activeMonsters)
            {
                if (monster == null || monster.IsDead || !monster.gameObject.activeInHierarchy || monster.IsDormant) continue;

                Vector3 mPos = monster.transform.position;
                if (_playerTransform != null && (playerPos - mPos).sqrMagnitude > maxDistSq) continue;

                float headOffset = monster.transform.localScale.y * 1.8f + 0.35f;
                Vector3 worldHead = mPos + Vector3.up * headOffset;
                Vector3 screenPos = _cam.WorldToScreenPoint(worldHead);

                if (screenPos.z <= 0f) continue;
                if (screenPos.x < -60f || screenPos.x > Screen.width + 60f || screenPos.y < -60f || screenPos.y > Screen.height + 60f) continue;

                BarItem item;
                if (poolIndex < _pool.Count)
                {
                    item = _pool[poolIndex];
                }
                else
                {
                    item = CreateBarItem();
                }
                poolIndex++;

                if (!item.gameObject.activeSelf) item.gameObject.SetActive(true);
                item.inUse = true;

                // 캔버스 로컬 좌표로 변환
                RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRoot, screenPos, null, out Vector2 localPos);
                item.rootRect.anchoredPosition = localPos;

                float hpRatio = Mathf.Clamp01(monster.CurrentHp / monster.MaxHp);
                item.fillImage.fillAmount = hpRatio;

                Color fillColor = monster.Type switch
                {
                    MonsterType.Elite => new Color(1f, 0.65f, 0.15f, 0.95f),
                    MonsterType.FoxBoss => new Color(0.95f, 0.2f, 0.15f, 0.95f),
                    MonsterType.Minion_Archer => new Color(0.35f, 0.95f, 0.35f, 0.95f),
                    MonsterType.Minion_BigMon => new Color(0.2f, 0.8f, 0.7f, 0.95f),
                    _ => new Color(0.9f, 0.25f, 0.25f, 0.92f)
                };
                item.fillImage.color = fillColor;

                if (monster.Level > 0)
                {
                    item.levelLabel.text = $"Lv.{monster.Level}";
                    item.levelLabel.enabled = true;
                }
                else
                {
                    item.levelLabel.enabled = false;
                }
            }

            // 나머지 미사용 풀 아이템 비활성화
            for (int i = poolIndex; i < _pool.Count; i++)
            {
                if (_pool[i].gameObject.activeSelf)
                {
                    _pool[i].gameObject.SetActive(false);
                    _pool[i].inUse = false;
                }
            }
        }

        private void HideAllPoolItems()
        {
            for (int i = 0; i < _pool.Count; i++)
            {
                if (_pool[i].gameObject.activeSelf)
                {
                    _pool[i].gameObject.SetActive(false);
                    _pool[i].inUse = false;
                }
            }
        }
    }
}
