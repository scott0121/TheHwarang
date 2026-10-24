using System;
using System.Collections.Generic;
using MythOfGuotaji.Progression;
using UnityEngine;

namespace MythOfGuotaji.Data
{
    /// <summary>
    /// 인게임 축복이 강화하는 스탯 유형
    /// </summary>
    public enum BoonStatType
    {
        MaxHp = 0,                  // 최대 체력
        MoveSpeed = 1,              // 이동 속도
        DashCooldown = 2,           // 대시 쿨다운 감소
        BowDamage = 3,              // 화살 기본 공격력
        BowRate = 4,                // 화살 연사 속도
        ArrowPierce = 5,            // 화살 추가 관통 수
        ArrowMulti = 6,             // 화살 다발 추가 발사 수
        ChargeRange = 7,            // 차징 관통 사거리 추가
        ChargeDamageMult = 8,       // 차징 화살 피해 배율 증가
        ChargeSpeed = 9,            // 차징 소요 시간 단축
        DaggerDamage = 10,          // 단검 근접 공격력
        DaggerRadius = 11,          // 단검 공격 반경
        DaggerReflectMult = 12,     // 투사체 반사 피해 배율
        GcdReduction = 13,          // 무기 전환 글로벌 쿨다운(GCD) 단축
        HeartDropRate = 14,         // 생명 하트 드롭 확률 증가
        HeartHealBonus = 15         // 하트 획득 시 추가 회복량
    }

    /// <summary>
    /// 인게임 업그레이드 선택지 1개 항목 (제목 - 0단계 수치 - Max 수치 - 최대 업그레이드 회수)
    /// </summary>
    [Serializable]
    public class InRunBoonEntry
    {
        [Tooltip("고유 식별 ID")]
        public string boonId;

        [Tooltip("축복 제목")]
        public string title;

        [Tooltip("카테고리 분류 (생존/기동, 활/사격, 차징화살, 단검/패링, 유틸/회복)")]
        public string category;

        [Tooltip("강화 대상 스탯 종류")]
        public BoonStatType statType;

        [Tooltip("0단계 -> 1단계 1회 업그레이드 기본 기준 수치")]
        public float baseStepValue;

        [Tooltip("Max 수치 (런 중 달성 가능한 최대 누적 증가 한도)")]
        public float maxValue;

        [Tooltip("최대 업그레이드 가능 회수 (Max Rank)")]
        public int maxUpgradeCount;

        [Tooltip("기본 희귀도")]
        public BoonRarity defaultRarity = BoonRarity.Common;

        [Tooltip("단위 표기 (HP, m/s, 초, %, 발, m, 배 등)")]
        public string unit = "";

        [Tooltip("정수 스탯 여부 (화살수, 관통수 등)")]
        public bool isIntegerStat = false;

        [Tooltip("설명 템플릿")]
        [TextArea(1, 2)]
        public string descriptionFormat = "";
    }

    /// <summary>
    /// 인게임 업그레이드(Boon) 전체 리스트 및 한도, 회수를 정의하는 마스터 테이블
    /// </summary>
    [CreateAssetMenu(fileName = "InRunBoonTable", menuName = "MythOfGuotaji/Data/InRunBoonTable")]
    public class InRunBoonTableSO : ScriptableObject
    {
        [Header("Master In-Run Boon Upgrade Database")]
        public List<InRunBoonEntry> entries = new();

        public InRunBoonEntry GetEntry(string boonId)
        {
            if (entries == null) return null;
            return entries.Find(e => e.boonId.Equals(boonId, StringComparison.OrdinalIgnoreCase));
        }

        public InRunBoonEntry GetEntryByStat(BoonStatType statType)
        {
            if (entries == null) return null;
            return entries.Find(e => e.statType == statType);
        }
    }
}
