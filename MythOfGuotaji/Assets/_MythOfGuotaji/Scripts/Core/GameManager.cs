using System;
using UnityEngine;

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
    /// 게임 전역 상태 및 루프를 관장하는 메인 싱글톤
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("State")]
        [SerializeField] private GameState _currentState = GameState.InStage;
        public GameState CurrentState => _currentState;

        [Header("Stage Runtime State")]
        public bool HasBossKey { get; private set; } = false;
        public int CurrentKills { get; private set; } = 0;
        public float StageElapsedTime { get; private set; } = 0f;

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
            OnBossKeyStatusChanged?.Invoke(false);
            OnKillCountChanged?.Invoke(0);
        }

        /// <summary>
        /// 보스방 진입을 위한 열쇠 획득
        /// </summary>
        public void AcquireBossKey()
        {
            HasBossKey = true;
            Debug.Log("<color=gold>[GameManager] 보스방 열쇠를 획득했습니다! 보스방 문이 열립니다.</color>");
            OnBossKeyStatusChanged?.Invoke(true);

            if (Dungeon.MazeGenerator.Instance != null)
            {
                Dungeon.MazeGenerator.Instance.UnlockBossDoors();
            }
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
        /// 스테이지 클리어 (보스 여우 처치)
        /// </summary>
        public void CompleteStage(int affinityReward, int goldReward)
        {
            SetState(GameState.StageSuccess);
            if (SaveData != null)
            {
                SaveData.affinityPoints += affinityReward;
                SaveData.gold += goldReward;
                SaveData.currentStage++;
                SaveGame();
            }
            Debug.Log($"[GameManager] 스테이지 클리어! 인연 포인트 +{affinityReward}, 골드 +{goldReward}");
        }

        /// <summary>
        /// 스테이지 실패 (거타지 사망) -> 로그라이크 리셋
        /// </summary>
        public void FailStage()
        {
            SetState(GameState.StageFailed);
            Debug.LogWarning("[GameManager] 거타지가 쓰러졌습니다. 해당 스테이지가 초기화됩니다.");
        }
    }
}
