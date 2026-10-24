using System.Collections.Generic;
using MythOfGuotaji.Actor;
using UnityEngine;

namespace MythOfGuotaji.UI
{
    /// <summary>
    /// 모든 몬스터 머리 위에 1920x1080 기준 40px 규격의 미니 체력바를 일괄 렌더링하는 UI 매니저
    /// </summary>
    public class MonsterHealthBarManager : MonoBehaviour
    {
        public static MonsterHealthBarManager Instance { get; private set; }

        private readonly HashSet<MonsterAI> _activeMonsters = new();
        private Camera _cam;
        private Transform _playerTransform;
        private Texture2D _whiteTex;

        [Header("Health Bar Dimensions (1080p standard: 40px)")]
        [SerializeField] private float _baseWidth = 40f;
        [SerializeField] private float _baseHeight = 5.5f;
        [SerializeField] private float _maxVisibleDistance = 32f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            _cam = Camera.main;
            _whiteTex = new Texture2D(1, 1);
            _whiteTex.SetPixel(0, 0, Color.white);
            _whiteTex.Apply();
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

        private void OnGUI()
        {
            if (_cam == null || _activeMonsters.Count == 0) return;

            Vector3 playerPos = _playerTransform != null ? _playerTransform.position : Vector3.zero;

            // 1920x1080 기준 해상도 스케일링 (1080p에서 정확히 40px)
            float scale = Screen.height / 1080f;
            float barW = _baseWidth * scale;
            float barH = _baseHeight * scale;

            Color prevColor = GUI.color;

            foreach (var monster in _activeMonsters)
            {
                if (monster == null || monster.IsDead || !monster.gameObject.activeInHierarchy) continue;

                Vector3 mPos = monster.transform.position;
                if (_playerTransform != null && Vector3.Distance(playerPos, mPos) > _maxVisibleDistance) continue;

                // 머리 위 오프셋 (스케일 반영)
                float headOffset = monster.transform.localScale.y * 1.8f + 0.35f;
                Vector3 worldHead = mPos + Vector3.up * headOffset;
                Vector3 screenPos = _cam.WorldToScreenPoint(worldHead);

                // 카메라 후방 또는 화면 외각 체크
                if (screenPos.z <= 0f) continue;
                if (screenPos.x < -60f || screenPos.x > Screen.width + 60f || screenPos.y < -60f || screenPos.y > Screen.height + 60f) continue;

                float x = screenPos.x - (barW * 0.5f);
                float y = (Screen.height - screenPos.y) - (barH * 0.5f);

                float hpRatio = Mathf.Clamp01(monster.CurrentHp / monster.MaxHp);

                // 1. 외곽선 및 다크 배경
                Rect bgRect = new Rect(x - 1, y - 1, barW + 2, barH + 2);
                GUI.color = new Color(0.04f, 0.04f, 0.07f, 0.88f);
                GUI.DrawTexture(bgRect, _whiteTex);

                // 2. 소실된 체력 배경 (다크 레드)
                Rect emptyRect = new Rect(x, y, barW, barH);
                GUI.color = new Color(0.25f, 0.05f, 0.05f, 0.82f);
                GUI.DrawTexture(emptyRect, _whiteTex);

                // 3. 체력 채움 바
                if (hpRatio > 0.001f)
                {
                    Color fillColor = monster.Type switch
                    {
                        MonsterType.Elite => new Color(1f, 0.65f, 0.15f, 0.95f), // 정예: 황금 주황
                        MonsterType.FoxBoss => new Color(0.95f, 0.2f, 0.15f, 0.95f), // 보스: 심홍
                        MonsterType.Minion_Archer => new Color(0.35f, 0.95f, 0.35f, 0.95f), // 맹독충: 맹독 연두
                        MonsterType.Minion_BigMon => new Color(0.2f, 0.8f, 0.7f, 0.95f), // 거북이: 청록 껍질
                        _ => new Color(0.9f, 0.25f, 0.25f, 0.92f) // 일반: 붉은색
                    };

                    GUI.color = fillColor;
                    Rect fillRect = new Rect(x, y, barW * hpRatio, barH);
                    GUI.DrawTexture(fillRect, _whiteTex);
                }

                // 4. 레벨업 몬스터 레벨 표시 (1레벨 이상 시 표기)
                if (monster.Level > 0)
                {
                    GUIStyle levelStyle = new GUIStyle(GUI.skin.label)
                    {
                        alignment = TextAnchor.MiddleRight,
                        fontSize = Mathf.Clamp(Mathf.RoundToInt(10f * scale), 9, 14),
                        fontStyle = FontStyle.Bold
                    };
                    levelStyle.normal.textColor = new Color(1f, 0.85f, 0.2f, 0.95f);
                    Rect lvlRect = new Rect(x - 38f * scale, y - 3f * scale, 35f * scale, barH + 6f * scale);
                    GUI.Label(lvlRect, $"Lv.{monster.Level}", levelStyle);
                }
            }

            GUI.color = prevColor;
        }

        private void OnDestroy()
        {
            if (_whiteTex != null)
            {
                Destroy(_whiteTex);
            }
        }
    }
}
