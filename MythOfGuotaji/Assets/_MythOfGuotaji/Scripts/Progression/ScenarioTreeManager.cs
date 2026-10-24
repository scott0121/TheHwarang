using System;
using System.Collections.Generic;
using MythOfGuotaji.Core;
using UnityEngine;

namespace MythOfGuotaji.Progression
{
    /// <summary>
    /// 메인 시나리오 트리 관리자
    /// - 인연 포인트(Affinity Points) 소모로 노드 해금
    /// - 불가역(초기화 불가): 새 게임 시에만 초기화 가능
    /// - 던전 해금, 동료 합류, 직업 방향성 분기 결정
    /// </summary>
    public class ScenarioTreeManager : MonoBehaviour
    {
        public static ScenarioTreeManager Instance { get; private set; }

        [SerializeField] private ScenarioTreeSO _treeData;

        public event Action<string> OnNodeUnlocked;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public bool IsNodeUnlocked(string nodeId)
        {
            if (GameManager.Instance == null || GameManager.Instance.SaveData == null) return false;
            return GameManager.Instance.SaveData.unlockedScenarioNodeIds.Contains(nodeId);
        }

        /// <summary>
        /// 노드 해금 가능 여부 검증
        /// </summary>
        public bool CanUnlockNode(ScenarioNodeSO node, out string reason)
        {
            reason = string.Empty;
            if (node == null)
            {
                reason = "유효하지 않은 노드입니다.";
                return false;
            }

            var save = GameManager.Instance.SaveData;
            if (save == null)
            {
                reason = "세이브 데이터를 찾을 수 없습니다.";
                return false;
            }

            if (IsNodeUnlocked(node.nodeId))
            {
                reason = "이미 해금된 설화 노드입니다.";
                return false;
            }

            // 인연 포인트 확인
            if (save.affinityPoints < node.requiredAffinityPoints)
            {
                reason = $"인연 포인트가 부족합니다. (필요: {node.requiredAffinityPoints}, 보유: {save.affinityPoints})";
                return false;
            }

            // 상호 배타 노드 해금 여부 확인
            foreach (var exId in node.exclusiveNodeIds)
            {
                if (IsNodeUnlocked(exId))
                {
                    reason = "선택 분기에서 다른 경로를 이미 선택하여 이 노드는 잠겼습니다.";
                    return false;
                }
            }

            // 선행 노드 충족 여부 확인
            foreach (var preId in node.prerequisiteNodeIds)
            {
                if (!IsNodeUnlocked(preId))
                {
                    reason = $"선행 노드 [{preId}]를 먼저 완료해야 합니다.";
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 시나리오 트리 노드 영구 해금 (초기화 불가)
        /// </summary>
        public bool TryUnlockNode(ScenarioNodeSO node)
        {
            if (!CanUnlockNode(node, out string reason))
            {
                Debug.LogWarning($"[ScenarioTree] 해금 불가: {reason}");
                return false;
            }

            var save = GameManager.Instance.SaveData;
            save.affinityPoints -= node.requiredAffinityPoints;
            save.unlockedScenarioNodeIds.Add(node.nodeId);

            // 해금 효과 즉시 반영
            ApplyUnlockEffect(node);

            GameManager.Instance.SaveGame();
            Debug.Log($"<color=cyan>[ScenarioTree] 노드 해금 성공: {node.title} (인연 포인트 -{node.requiredAffinityPoints})</color>");
            OnNodeUnlocked?.Invoke(node.nodeId);
            return true;
        }

        private void ApplyUnlockEffect(ScenarioNodeSO node)
        {
            var save = GameManager.Instance.SaveData;

            switch (node.unlockType)
            {
                case ScenarioUnlockType.RecruitCompanion:
                    if (!save.unlockedCompanionIds.Contains(node.targetIdentifier))
                    {
                        save.unlockedCompanionIds.Add(node.targetIdentifier);
                        Debug.Log($"<color=lime>[동료 합류] '{node.targetIdentifier}'가 거타지의 여정에 합류했습니다!</color>");
                    }
                    break;

                case ScenarioUnlockType.UnlockDungeon:
                    Debug.Log($"<color=gold>[던전 해금] 신규 지역 '{node.targetIdentifier}'가 개방되었습니다!</color>");
                    break;

                case ScenarioUnlockType.UnlockProfession:
                    save.currentProfession = node.targetIdentifier;
                    Debug.Log($"<color=magenta>[직업 분기] 새로운 직업 갈래 '{node.targetIdentifier}'가 선택되었습니다!</color>");
                    break;
            }
        }
    }
}
