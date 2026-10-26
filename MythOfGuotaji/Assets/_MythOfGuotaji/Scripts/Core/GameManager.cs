using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MythOfGuotaji.Core
{
    public enum GameState
    {
        Title,
        Hub,
        InStage,
        StageSuccess,
        StageFailed
    }

    /// <summary>
    /// 게임 전역 상태 및 루프, Outgame(Intro) <-> Ingame(PlayMain) 씬 전환을 관장하는 메인 싱글톤
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public const string IntroSceneName = "Intro";
        public const string PlayMainSceneName = "PlayMain";

        public static GameManager Instance { get; private set; }

        [Header("State")]
        [SerializeField] private GameState _currentState = GameState.Title;
        public GameState CurrentState => _currentState;

        [Header("Stage Runtime State")]
        public bool HasBossKey { get; private set; } = false;
        public int CurrentKills { get; private set; } = 0;
        public float StageElapsedTime { get; private set; } = 0f;
        public int CurrentStage => SaveData != null ? Mathf.Max(1, SaveData.currentStage) : 1;

        /// <summary>
        /// 게임 시작 후 5분부터 매 5분마다 소환되는 몬스터의 레벨 (5분 미만: 0Lv, 5분~: 1Lv, 10분~: 2Lv...)
        /// 레벨당 체력 +20%, 공격력 +10% 합연산 적용
        /// </summary>
        public int CurrentMonsterLevel => Mathf.Max(0, Mathf.FloorToInt(StageElapsedTime / 300f));

        public SaveData SaveData { get; private set; }

        public event Action<GameState> OnStateChanged;
        public event Action<bool> OnBossKeyStatusChanged;
        public event Action<int> OnKillCountChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            LoadGame();
        }

        private void Update()
        {
            if (_currentState == GameState.InStage)
            {
                StageElapsedTime += Time.deltaTime;
            }
        }

        public void AddStageElapsedTime(float seconds)
        {
            StageElapsedTime = Mathf.Max(0f, StageElapsedTime + seconds);
            Debug.Log($"[GameManager] 진행 시간 조정: {StageElapsedTime:F1}s (현재 몬스터 레벨: Lv.{CurrentMonsterLevel})");
        }

        public void LoadGame()
        {
            SaveData = SaveSystem.Load();
        }

        public void SaveGame()
        {
            if (SaveData != null)
            {
                SaveSystem.Save(SaveData);
            }
        }

        public void SetState(GameState newState)
        {
            if (_currentState == newState) return;
            _currentState = newState;
            Debug.Log($"[GameManager] GameState changed to: {_currentState}");
            OnStateChanged?.Invoke(_currentState);
        }

        /// <summary>
        /// 스테이지 진입 시 런타임 상태 초기화
        /// </summary>
        public void StartStage()
        {
            HasBossKey = false;
            CurrentKills = 0;
            StageElapsedTime = 0f;
            SetState(GameState.InStage);
            Progression.InRunProgressionManager.Instance?.ResetInRunProgress();
            OnBossKeyStatusChanged?.Invoke(false);
            OnKillCountChanged?.Invoke(0);
        }

        /// <summary>
        /// [던전 문 정책 - 규칙 8]: 중간보스 처치로 열쇠 획득
        /// - 열쇠 획득 즉시 원격으로 문을 열지 않으며, 해당 방의 전투가 끝난 뒤 플레이어가 중간보스방 출구 문 앞에 가야만 열림
        /// </summary>
        public void AcquireBossKey()
        {
            HasBossKey = true;
            Debug.Log("<color=gold>[GameManager] 중간보스 열쇠를 획득했습니다! 전투가 끝난 뒤 중간보스방 출구 관문 앞으로 다가가면 문을 열 수 있습니다.</color>");
            OnBossKeyStatusChanged?.Invoke(true);
        }

        /// <summary>
        /// 몬스터 처치 기록
        /// </summary>
        public void AddKill()
        {
            CurrentKills++;
            OnKillCountChanged?.Invoke(CurrentKills);
        }

        /// <summary>
        /// 스테이지 클리어 (보스 토벌 완료)
        /// </summary>
        public void CompleteStage(int affinityReward, int goldReward)
        {
            SetState(GameState.StageSuccess);
            if (SaveData != null)
            {
                SaveData.affinityPoints += affinityReward;
                SaveData.gold += goldReward;
                SaveData.memoryShards += 1;
                SaveData.totalCollectedShards = Mathf.Min(22, SaveData.totalCollectedShards + 1);
                SaveData.retryBlessingStacks = 0;
                SaveData.returnedFromDeath = false;
                SaveData.currentStage = Mathf.Min(22, SaveData.currentStage + 1);
                SaveGame();
            }
            Debug.Log($"[GameManager] 스테이지 클리어! 기억의 조각 +1, 인연 포인트 +{affinityReward}, 골드 +{goldReward}");
        }

        /// <summary>
        /// 스테이지 실패 (거타지 사망) -> 재도전 가호 중첩 및 사당 귀환 대기
        /// </summary>
        public void FailStage()
        {
            SetState(GameState.StageFailed);
            if (SaveData != null)
            {
                SaveData.retryBlessingStacks = Mathf.Min(5, SaveData.retryBlessingStacks + 1);
                SaveData.returnedFromDeath = true;
                SaveGame();
            }
            Debug.LogWarning("[GameManager] 거타지가 쓰러졌습니다. 보옥의 결계(곡도 사당 우물가)로 귀환하거나 재도전할 수 있습니다.");
        }

        /// <summary>
        /// 아웃게임(Intro) 씬에서 선택한 챕터/스테이지로 인게임(PlayMain) 씬 출격
        /// </summary>
        public void LaunchStageToPlayMain(int chapter, int stageNumber, string stageCode)
        {
            if (SaveData == null) LoadGame();
            if (SaveData != null)
            {
                SaveData.currentChapter = chapter;
                SaveData.currentStage = Mathf.Clamp(stageNumber, 1, 22);
                SaveData.selectedStageCode = stageCode;
                SaveGame();
            }

            Time.timeScale = 1f;
            Debug.Log($"<color=cyan><b>[GameManager]</b> 곡도 사당에서 미궁으로 출격합니다! (Chapter {chapter} / Stage {stageCode}, Index #{stageNumber})</color>");
            SceneManager.LoadScene(PlayMainSceneName);
        }

        /// <summary>
        /// 인게임(PlayMain)에서 아웃게임 거점(Intro - 곡도 사당)으로 귀환
        /// </summary>
        public void ReturnToIntroHub()
        {
            Time.timeScale = 1f;
            SetState(GameState.Hub);
            Debug.Log("<color=cyan><b>[GameManager]</b> 곡도 사당 거점(Intro 씬)으로 귀환합니다.</color>");
            SceneManager.LoadScene(IntroSceneName);
        }
    }
}
