using System.Collections.Generic;
using MythOfGuotaji.Actor;
using MythOfGuotaji.Core;
using MythOfGuotaji.Dungeon;
using MythOfGuotaji.Progression;
using MythOfGuotaji.UI.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace MythOfGuotaji.UI
{
    public class ProgressionDebugUI : UIScreen
    {
        private PlayerController _player;
        private bool _showDebugPanel = false;

        private RectTransform _panelRoot;
        private TextMeshProUGUI _infoLabel;

        protected override int SortingOrder => UITheme.Sorting.Debug;
        protected override bool NeedsRaycaster => true;
        protected override bool NeedsEventSystem => true;

        private void Start()
        {
            _player = FindAnyObjectByType<PlayerController>();
            EnsureBuilt();
            SetVisible(false);
        }

        protected override void Build(RectTransform root)
        {
            var panelImg = UIKit.CreatePanel(root, "DebugPanel", new Color(0.08f, 0.08f, 0.12f, 0.92f), false, true);
            _panelRoot = panelImg.rectTransform;
            UIKit.SetAnchored(_panelRoot, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f, -20f), new Vector2(360f, 480f));

            var title = UIKit.CreateLabel(_panelRoot, "Title", "🏹 거타지 디버그 콘솔 (F1)", 15f, UITheme.Gold, TextAlignmentOptions.Left, FontStyles.Bold, false, false);
            UIKit.SetAnchored(title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(16f, -12f), new Vector2(320f, 26f));

            _infoLabel = UIKit.CreateLabel(_panelRoot, "Info", "", 12f, Color.white, TextAlignmentOptions.TopLeft, FontStyles.Normal, false, false);
            UIKit.SetAnchored(_infoLabel.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(16f, -44f), new Vector2(320f, 160f));

            var btnContainer = UIKit.CreateRect("Buttons", _panelRoot);
            UIKit.SetAnchored(btnContainer, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(16f, 16f), new Vector2(328f, 260f));

            var vlg = btnContainer.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 8f;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            CreateDebugButton(btnContainer, "체력 완전 회복 (HP 100%)", () =>
            {
                if (_player != null)
                {
                    _player.Heal(_player.MaxHp);
                }
            });

            CreateDebugButton(btnContainer, "보스방 열쇠 강제 지급 (🔑)", () =>
            {
                GameManager.Instance?.AcquireBossKey();
            });

            CreateDebugButton(btnContainer, "영물 축복 (즉시 1레벨업)", () =>
            {
                if (InRunProgressionManager.Instance != null)
                {
                    InRunProgressionManager.Instance.AddExp(InRunProgressionManager.Instance.expToNextLevel);
                }
            });

            CreateDebugButton(btnContainer, "방 안의 모든 일반 요괴 처치", () =>
            {
                var monsters = new List<MonsterAI>(MonsterAI.ActiveMonsters);
                for (int i = 0; i < monsters.Count; i++)
                {
                    if (monsters[i] != null && !monsters[i].IsDead && monsters[i].Type != MonsterType.FoxBoss)
                    {
                        monsters[i].Die();
                    }
                }
            });

            CreateDebugButton(btnContainer, "창 닫기 (F1)", () =>
            {
                _showDebugPanel = false;
                SetVisible(false);
            });
        }

        private void CreateDebugButton(Transform parent, string label, UnityEngine.Events.UnityAction action)
        {
            var btn = UIKit.CreateButton(parent, "Btn_" + label, label, action, 12f, UITheme.ButtonNormal, Color.white);
            var rt = (RectTransform)btn.transform;
            rt.sizeDelta = new Vector2(0f, 34f);
            var le = btn.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 34f;
            le.minHeight = 34f;
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.f1Key.wasPressedThisFrame)
            {
                _showDebugPanel = !_showDebugPanel;
                SetVisible(_showDebugPanel);
            }

            if (_showDebugPanel && _infoLabel != null)
            {
                UpdateInfo();
            }
        }

        private void UpdateInfo()
        {
            if (_player == null) _player = FindAnyObjectByType<PlayerController>();

            float hp = _player != null ? _player.CurrentHp : 0f;
            float maxHp = _player != null ? _player.MaxHp : 0f;
            bool hasKey = GameManager.Instance != null && GameManager.Instance.HasBossKey;
            int kills = GameManager.Instance != null ? GameManager.Instance.CurrentKills : 0;
            int level = InRunProgressionManager.Instance != null ? InRunProgressionManager.Instance.currentRunLevel : 1;
            int exp = InRunProgressionManager.Instance != null ? InRunProgressionManager.Instance.currentRunExp : 0;
            int reqExp = InRunProgressionManager.Instance != null ? InRunProgressionManager.Instance.expToNextLevel : 10;

            _infoLabel.text =
                $"<b>거타지 HP:</b> <color=#55FF55>{Mathf.CeilToInt(hp)}</color> / {maxHp}\n" +
                $"<b>보스방 열쇠:</b> {(hasKey ? "<color=yellow>🔑 보유 중</color>" : "<color=gray>❌ 미보유</color>")}\n" +
                $"<b>요괴 처치 수:</b> {kills} 마리\n" +
                $"<b>인런 레벨:</b> Lv.{level} (영혼석: {exp}/{reqExp})\n" +
                $"<b>활동 요괴:</b> {MonsterAI.ActiveMonsters.Count} 마리";
        }
    }
}
