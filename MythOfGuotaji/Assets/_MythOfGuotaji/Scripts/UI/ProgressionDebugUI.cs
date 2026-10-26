using System.Collections.Generic;
using MythOfGuotaji.Actor;
using MythOfGuotaji.Core;
using MythOfGuotaji.Dungeon;
using MythOfGuotaji.Progression;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MythOfGuotaji.UI
{
    /// <summary>
    /// 프로토타입 즉시 플레이 및 시나리오/캐릭터 트리, 던전 생성, 전투 테스트를 위한 통합 HUD & 디버그 패널
    /// </summary>
    public class ProgressionDebugUI : MonoBehaviour
    {
        private PlayerController _player;
        private bool _showDebugPanel = true;
        private List<InRunBoonSO> _pendingBoonChoices = null;

        private void Start()
        {
            _player = FindAnyObjectByType<PlayerController>();

            if (InRunProgressionManager.Instance != null)
            {
                InRunProgressionManager.Instance.OnBoonSelectionRequested += boons =>
                {
                    // UGUI 기반 BoonSelectionUI가 없을 때만 OnGUI 모달 fallback 활성화
                    if (BoonSelectionUI.Instance == null)
                    {
                        _pendingBoonChoices = boons;
                        Time.timeScale = 0f;
                    }
                };
            }
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.f1Key.wasPressedThisFrame)
            {
                _showDebugPanel = !_showDebugPanel;
            }

            if (_player == null)
            {
                _player = FindAnyObjectByType<PlayerController>();
            }
        }

        private void OnGUI()
        {
            DrawHUD();

            if (_pendingBoonChoices != null && _pendingBoonChoices.Count > 0)
            {
                DrawBoonSelectionModal();
                return;
            }

            if (_showDebugPanel)
            {
                DrawDebugControls();
            }
        }

        private void DrawHUD()
        {
            GUI.Box(new Rect(15, 15, 290, 160), "<b>🏹 거타지의 전설 (Myth of Guotaji)</b>");

            GUILayout.BeginArea(new Rect(25, 40, 270, 130));

            // 플레이어 HP
            if (_player != null)
            {
                GUILayout.Label($"<b>거타지 HP:</b> {Mathf.CeilToInt(_player.CurrentHp)} / {_player.MaxHp}");
            }
            else
            {
                GUILayout.Label("플레이어 대기 중...");
            }

            // 보스방 열쇠 여부
            bool hasKey = GameManager.Instance != null && GameManager.Instance.HasBossKey;
            string keyText = hasKey ? "<color=yellow>🔑 보유 중 (보스방 개방 가능)</color>" : "<color=gray>❌ 미보유 (열쇠방 탐색 필요)</color>";
            GUILayout.Label($"<b>보스방 열쇠:</b> {keyText}");

            // 요괴 처치 수
            int kills = GameManager.Instance != null ? GameManager.Instance.CurrentKills : 0;
            GUILayout.Label($"<b>처치한 요괴 수:</b> {kills} 마리");

            // 스테이지 레벨 및 보석 수집 경험치
            if (InRunProgressionManager.Instance != null)
            {
                GUILayout.Label($"<b>런 레벨:</b> Lv.{InRunProgressionManager.Instance.currentRunLevel}  (보석 EXP: {InRunProgressionManager.Instance.currentRunExp}/{InRunProgressionManager.Instance.expToNextLevel})");
            }

            // 진행 시간 및 몬스터 레벨
            if (GameManager.Instance != null)
            {
                float t = GameManager.Instance.StageElapsedTime;
                int mins = Mathf.FloorToInt(t / 60f);
                int secs = Mathf.FloorToInt(t % 60f);
                int mLvl = GameManager.Instance.CurrentMonsterLevel;
                GUILayout.Label($"<b>진행 시간:</b> {mins:00}:{secs:00} | <b>적 요괴 레벨:</b> Lv.{mLvl} (HP +{mLvl * 20}%, ATK +{mLvl * 10}%)");
            }

            GUILayout.EndArea();
        }

        private void DrawBoonSelectionModal()
        {
            GUI.Box(new Rect(Screen.width * 0.5f - 310, Screen.height * 0.5f - 190, 620, 380), "<b>✨ [동료 헬퍼] 영혼석 정제 완료! 새로운 비술(스킬)을 선택하세요</b>");

            GUILayout.BeginArea(new Rect(Screen.width * 0.5f - 290, Screen.height * 0.5f - 140, 580, 320));
            GUILayout.Space(10);

            for (int i = 0; i < _pendingBoonChoices.Count; i++)
            {
                var boon = _pendingBoonChoices[i];
                GUILayout.BeginVertical("box");
                GUILayout.Label($"<b>{boon.boonTitle}</b> [{boon.rarity}]");
                GUILayout.Label(boon.description);

                if (GUILayout.Button("선택하기", GUILayout.Height(35)))
                {
                    InRunProgressionManager.Instance.SelectBoon(boon);
                    _pendingBoonChoices = null;
                    Time.timeScale = 1f;
                    break;
                }
                GUILayout.EndVertical();
                GUILayout.Space(10);
            }

            GUILayout.EndArea();
        }

        private void DrawDebugControls()
        {
            float w = 310;
            GUI.Box(new Rect(Screen.width - w - 15, 15, w, 485), "<b>🛠️ 개발 & 테스트 제어판 (F1 토글)</b>");

            GUILayout.BeginArea(new Rect(Screen.width - w - 5, 45, w - 20, 445));

            var save = GameManager.Instance?.SaveData;
            if (save != null)
            {
                GUILayout.Label($"<b>출격 스테이지:</b> Ch.{save.currentChapter} ({save.selectedStageCode}) | <b>기억 조각:</b> {save.memoryShards}개");
                GUILayout.Label($"<b>인연 포인트:</b> {save.affinityPoints} pt  |  <b>재도전 가호:</b> {save.retryBlessingStacks}중첩");
            }

            if (GUILayout.Button("⛩️ 곡도 사당 거점으로 귀환 (Intro 씬)", GUILayout.Height(28)))
            {
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.ReturnToIntroHub();
                }
                else
                {
                    UnityEngine.SceneManagement.SceneManager.LoadScene("Intro");
                }
            }

            GUILayout.Space(8);
            GUILayout.Label("<b>[절차적 미로 던전 제어]</b>");
            if (GUILayout.Button("🎲 미로 던전 즉시 재배치 (Re-Generate)"))
            {
                MazeGenerator.Instance?.RestartStage();
            }
            if (GUILayout.Button("🔑 보스방 열쇠 즉시 획득 (Cheat)"))
            {
                GameManager.Instance?.AcquireBossKey();
                MazeGenerator.Instance?.UnlockBossDoors();
            }

            GUILayout.Space(8);
            GUILayout.Label("<b>[산해경 요괴 스폰]</b>");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("잡몹 10마리"))
            {
                if (_player != null && HordeSpawner.Instance != null)
                {
                    HordeSpawner.Instance.SpawnHorde(_player.transform.position, 10, 5f);
                }
            }
            if (GUILayout.Button("정예 열쇠 수문장"))
            {
                if (_player != null && HordeSpawner.Instance != null)
                {
                    HordeSpawner.Instance.SpawnEliteKeyKeeper(_player.transform.position + Vector3.forward * 4f);
                }
            }
            GUILayout.EndHorizontal();

            if (GUILayout.Button("🦊 여우 요괴(최종 보스) 스폰"))
            {
                if (_player != null && HordeSpawner.Instance != null)
                {
                    HordeSpawner.Instance.SpawnFoxBoss(_player.transform.position + Vector3.forward * 6f);
                }
            }

            GUILayout.Space(8);
            GUILayout.Label("<b>[시간 기반 몬스터 레벨업 제어]</b>");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("⏱️ +5분 가속 (Lv+1)"))
            {
                GameManager.Instance?.AddStageElapsedTime(300f);
            }
            if (GUILayout.Button("⏱️ -5분 단축 (Lv-1)"))
            {
                GameManager.Instance?.AddStageElapsedTime(-300f);
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(8);
            GUILayout.Label("<b>[성장 트리 테스트]</b>");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("인연 포인트 +5"))
            {
                if (save != null) save.affinityPoints += 5;
            }
            if (GUILayout.Button("스킬 포인트 +3"))
            {
                if (save != null) save.characterSkillPoints += 3;
            }
            GUILayout.EndHorizontal();

            if (GUILayout.Button("🔄 캐릭터 스킬 트리 전체 리셋 (100% 환급)"))
            {
                CharacterTreeManager.Instance?.ResetAllSkills();
            }

            GUILayout.EndArea();
        }
    }
}
