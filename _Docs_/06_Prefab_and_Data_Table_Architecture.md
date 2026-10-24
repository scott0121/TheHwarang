# 거타지의 전설: 프리팹 및 데이터 테이블(시트) 아키텍처

## 1. 개요 (Overview)
기존 런타임 절차적 프리미티브 생성(Primitive) 방식을 **완전한 Unity Prefab 로딩 파이프라인**으로 전환하고, 모든 게임 플레이 수치(플레이어, 무기, 몬스터, 던전, 헬퍼 영물)를 **ScriptableObject 테이블 및 외부 편집 가능한 CSV 시트**로 분리하여 완벽한 데이터 주도형(Data-Driven) 설계를 완성했습니다.

---

## 2. 프리팹 시스템 구조 (Prefab Architecture)

모든 프리팹은 `Assets/_MythOfGuotaji/Resources/Prefabs/` 경로에 저장되며, `PrefabDatabaseSO`를 통해 중앙 관리됩니다. 에셋 누락 시에도 게임이 멈추지 않도록 강력한 Fallback 루틴이 내장되어 있습니다.

| 프리팹 명칭 | 파일 경로 | 주요 컴포넌트 및 구성 |
|---|---|---|
| **Player_Guotaji** | `Resources/Prefabs/Player_Guotaji.prefab` | `CharacterController`, `PlayerController`, `BowWeapon`, `DaggerWeapon`, `CatPlayer_Job00` 고양이 무사 외형 스킨 및 애니메이터 연동 |
| **Arrow_Projectile** | `Resources/Prefabs/Arrow_Projectile.prefab` | `Projectile`, `SphereCastAll` 0.4m 스윕 검출체, `Rigidbody` (Kinematic), 비행 실린더 메시 |
| **ChargedArrow_Projectile** | `Resources/Prefabs/ChargedArrow_Projectile.prefab` | `Projectile`, 0.3~3.0s 선형 사거리(0~25m), 무제한 관통, 2배 피해, 1.5배속, 황금 에너틱 비행체 |
| **Monster_Minion** | `Resources/Prefabs/Monster_Minion.prefab` | `CharacterController`, `MonsterAI`, 잡몹 2m 돌진 공격(0.3s 예고 직사각형, 0.3s 돌진), 몬스터 스킨 및 애니메이터 |
| **Monster_Archer** | `Resources/Prefabs/Monster_Archer.prefab` | `CharacterController`, `MonsterAI`, 맹독충(원거리 투사체 6m/s, 사거리 15m, 쿨 0.7s), 맹독 녹색 틴트 |
| **Monster_BigMon** | `Resources/Prefabs/Monster_BigMon.prefab` | `CharacterController`, `MonsterAI`, 거북이(탱커, 전방 90도 0.5s 예고 휩쓸기, 3m 넉백), 단단한 청록 틴트 |
| **Monster_Zhu** | `Resources/Prefabs/Monster_Zhu.prefab` | `CharacterController`, `MonsterAI`, 주(鴸)(사거리 20m, 5m 원형 예고 GUI, 2초 고정 포물선 곡사 폭발, 후딜 0.2s) |
| **Monster_Elite** | `Resources/Prefabs/Monster_Elite.prefab` | `CharacterController`, `MonsterAI` (열쇠 드랍 수문장, 1.5배 크기, 황금 틴트, 애니메이터 연동) |
| **Monster_FoxBoss** | `Resources/Prefabs/Monster_FoxBoss.prefab` | `CharacterController`, `MonsterAI` (보스 여우 요괴, 2.2배 크기, 주황 틴트, 포효 애니메이션 연동) |
| **Helper_SpiritRabbit** | `Resources/Prefabs/Helper_Companion.prefab` | `HelperCompanion` (달빛 영혼 토끼, 3m 진공 보석 흡수, 비전투 안전지대 수집, 스킬 구체 제시) |
| **ExpGem** | `Resources/Prefabs/ExpGem.prefab` | `ExpGem` (몬스터 처치 시 드롭되는 영혼석 보석, 헬퍼 전용 흡수체) |
| **Companion_Howon** | `Resources/Prefabs/Companion_Howon.prefab` | `CharacterController`, `CompanionAI` (플레이어 호위 및 원거리 사격 지원) |
| **DoorSocket** | `Resources/Prefabs/DoorSocket.prefab` | `DoorSocket`, 12m 확장 규격 문(기존 대비 2배), 전투/보스 봉인 배리어 |
| **DungeonRoom_Base**| `Resources/Prefabs/DungeonRoom_Base.prefab`| `RoomController`, 39m 바닥 메시, 방 진입 감지 BoxCollider, 4방향 12m DoorSocket |
| **MainCamera** | `Resources/Prefabs/MainCamera.prefab` | `Camera` (FOV 48, #0B0C10), `QuarterViewCamera` (오프셋/스무딩/Lookahead/52도 회각 설정 최우선 적용), `AudioListener` |

---

## 3. 데이터 테이블 및 ScriptableObject 구조 (Data Table System)

수치 밸런스를 유니티 인스펙터 및 런타임에서 직관적으로 수정하고 반영할 수 있도록 체계화되었습니다.

### 3.1 ScriptableObject 테이블 (`Resources/Data/`)
- **`PlayerStatsTableSO` (`PlayerStatsTable.asset`)**:
  - 기본 체력(`maxHp: 100`), 이동 속도(`moveSpeed: 7.5m/s`)
  - 대시 회피 속도(`dashSpeed: 16m/s`), 대시 지속시간(`dashDuration: 0.2s`), 대시 쿨다운(`dashCooldown: 1.0s`)
  - **글로벌 쿨다운 (GCD)**:
    - 동일 무기 카테고리 GCD: `sameCategoryGCD = 0.5s` (활 발사 후 차징/연사는 0.5초 대기)
    - 다른 무기/마법 카테고리 GCD: `diffCategoryGCD = 0.2s` (활 발사 후 0.2초 만에 단검 사용 가능)

- **`WeaponStatsTableSO` (`WeaponStatsTable.asset`)**:
  - **활 (Bow)**:
    - 기본 공격: 피해량(`baseDamage: 25`), 공격속도(`attackRate: 2.8`), 치명타(`critChance: 15%`, `critMultiplier: 2.0x`), 넉백(`4.5`)
    - 투사체: 속도(`24m/s`), 생존시간(`2.5s`), 충돌반경(`0.4m`)
    - 5연사(Burst): 발사 수(`burstShotCount: 5`), 연사 시간(`burstTotalDuration: 1.5s`), 쿨다운(`burstCooldown: 2.0s`)
    - 동시 피격 점감: 동일 볼리 화살 동시 피격 감쇄비율(`multiHitDamageFalloffRatio: 0.5` -> 1발 100%, 2발 150%, 3발 175%...)
    - 차징 화살: 시작 대기(`chargeStartTime: 0.3s`), 최대 차징(`maxChargeTime: 3.0s`), 사거리 선형 증가(`chargeMinRange: 0m` ~ `chargeMaxRange: 25m`), 사거리 비례 바닥 가이드 궤적 UI(`chargeTelegraphWidth: 0.3m`), 무제한 관통, 피해량 2배, 탄속 36m/s, 넉백 12.0
  - **단검 (Dagger)**:
    - 피해량(`baseDamage: 50`), 사거리 축소(`attackRadius: 3.0m`), 각도(`attackAngle: 150도`), 쿨다운(`cooldown: 0.5s`), 넉백(`9.0`)
    - **패링 반사**: 상대방 원거리 투사체 및 마법 투사체를 정확히 타격 시 날아오던 각도로 완벽 반사(`canReflectProjectiles: true`, `reflectDamageMultiplier: 1.5x`, `reflectSpeedMultiplier: 1.2x`)

- **`MonsterStatsTableSO` (`MonsterStatsTable.asset`)**:
  - 6종 요괴(Minion, Minion_Archer, Minion_BigMon, Minion_Zhu, Elite, FoxBoss)의 전방위 수치 테이블화:
    - 체력, 이속, 공격력, 공격사거리, 공격쿨다운, 넉백파워, 분리반경, 보상(골드, 호감도)
    - **원거리/스킬 패턴 파라미터**: 투사체 속도(`projectileSpeed`), 카이팅 거리(`kiteDistance`), 전조 시간(`telegraphDuration`), 특수공격 반경/각도(`specialAttackRadius`, `specialAttackAngle`), 곡사 비행시간(`flightDuration`), 곡사 높이(`arcHeight`), 공격 후딜(`postAttackDelay`)
    - **잡몹 돌진 파라미터**: 돌진 사거리(`dashDistance: 2.0m`), 돌진 폭(`dashWidth: 0.5m`), 돌진 시간(`dashDuration: 0.3m`)
    - **영혼석 보석 드롭 수량**: `gemDropCount` 및 보석 당 경험치 `expPerGem`
      - 잡몹/맹독충: 1보석 (1 EXP)
      - 거북이/주: 2보석 (1 EXP)
      - 엘리트 수문장: 5보석 (2 EXP)
      - 구미호 보스: 12보석 (3 EXP)

- **`HelperDataSO` (`HelperData.asset`)**:
  - 헬퍼 식별자(`helperId`), 표시명(`displayName: "달빛 영혼 토끼"`), 설명
  - 이동 속도(`moveSpeed: 6.8m/s`), 위험 시 플레이어로 도주 속도(`fleeSpeed: 8.8m/s`)
  - **진공 흡수 반경**: `vacuumRadius = 3.0m` (이동 시 주변 3m 이내 보석 청소기 흡수)
  - **안전 구역 판정 거리**: `gemSafetyRadius = 8.5m` (요괴가 없는 안전 구역의 보석만 탐색 수집)
  - **몬스터 회피 거리**: `fleeDistance = 5.5m` (헬퍼 주변 5.5m 내 요괴 접근 시 플레이어 뒤로 도주)
  - 외형 색상, 오라 색상, 스케일

- **`DungeonConfigTableSO` (`DungeonConfigTable.asset`)**:
  - $6 \times 6$ 그리드, 39m 방 규격, 문 개구부 12m (기존 6m 대비 2배 확장), 몬스터 스폰 수

- **`PrefabDatabaseSO` (`PrefabDatabase.asset`)**:
  - 전역 프리팹 에셋 카탈로그 (플레이어, 몬스터, 발사체, 헬퍼, 보석 등)

---

## 4. 원클릭 에디터 메뉴 도구 (Editor Tools)

유니티 상단 메뉴바의 `MythOfGuotaji` 메뉴에서 원클릭으로 일괄 동기화할 수 있습니다:

1. **`MythOfGuotaji` > `Generate All Prefabs and Tables`**
   - 모든 몬스터, 플레이어, 헬퍼 프리팹 일괄 자동 조립 및 검증
   - 6종 밸런스 테이블 에셋 자동 갱신 및 기본값 초기화
   - CSV 시트 자동 추출 및 `PrefabDatabaseSO` 링크

2. **`MythOfGuotaji` > `Data` > `Export All Tables to CSV Sheets`**
   - ScriptableObject 밸런스 데이터를 외부 CSV 시트 파일로 추출

3. **`MythOfGuotaji` > `Data` > `Import All CSV Sheets to Tables`**
   - CSV 시트에서 수정한 수치를 유니티 테이블로 일괄 역반영
