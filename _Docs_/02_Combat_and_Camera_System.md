# 🏹 02. 전투 및 카메라 시스템 (Combat & Camera System)

본 문서는 《거타지의 전설》의 쿼터뷰 카메라, 트윈스틱 조작, 글로벌 쿨다운(GCD) 규칙, 활/단검 전투 메커니즘, 6종 요괴 AI 및 공격 패턴, 헬퍼 동행 영물 시스템 상세를 다룹니다.

---

## 1. 쿼터뷰 카메라 시스템 ([QuarterViewCamera.cs](file:///c:/__Works__/_MythOfGuotaji_/MythOfGuotaji/Assets/_MythOfGuotaji/Scripts/Camera/QuarterViewCamera.cs))

* **고정 쿼터뷰 각도**: 수평면 기준 약 $52^\circ$ 하향 시야각으로 캐릭터 주변 및 원거리 적들을 한눈에 조망.
* **Dynamic Lookahead Offset**:
  * 마우스 스크린 좌표를 지면 평면(Ground Plane)에 Raycast하여, 거타지 위치에서 마우스 방향으로 카메라 시야를 부드럽게 확장(`maxLookaheadDistance = 4.5m`).
* **Smooth Follow & Shake**:
  * `Vector3.SmoothDamp`를 통한 부드러운 위치 보간 및 피격/패링/폭발 발생 시 `TriggerShake(intensity, duration)` 지원.

---

## 2. 거타지 조작 및 전투 시스템 ([PlayerController.cs](file:///c:/__Works__/_MythOfGuotaji_/MythOfGuotaji/Assets/_MythOfGuotaji/Scripts/Actor/PlayerController.cs))

### 2.1 글로벌 쿨다운 (Global Cooldown, GCD) 규칙
* **동일 카테고리 무기/스킬 GCD**: **0.5초**
  - 예: 활 연사 발동 후 0.5초가 지나야 차징 화살 스킬 사용 가능, 차징 발사 후 0.5초 후 연사 가능.
* **다른 카테고리 무기/마법 GCD**: **0.2초**
  - 예: 활 발사 후 0.2초 뒤 즉시 단검 베기/패링 사용 가능, 단검 사용 후 0.2초 뒤 활 사용 가능.
* 모든 수치는 [`PlayerStatsTableSO`](file:///c:/__Works__/_MythOfGuotaji_/MythOfGuotaji/Assets/_MythOfGuotaji/Scripts/Data/PlayerStatsTableSO.cs)에서 인스펙터/시트로 조율 가능.

### 2.2 캐릭터 이동 및 조작
* `WASD`: 캐릭터의 8방향 자유 이동 (`_moveSpeed = 7.5m/s`).
* `Mouse Raycast`: 캐릭터의 상체 및 시선은 마우스 커서의 지면 투영 좌표를 실시간으로 조준.
* `Space / Left Shift`: 이동/조준 방향으로 고속 대시 회피 (`dashSpeed = 16m/s`, 무적 판정, 쿨다운 1.0초).
* **외형 및 애니메이션 연동**:
  - `CatPlayer_Job00` 프리팹 연동 (`MoveSpeed` 파라미터, `Jump` 트리거, 상체 공격 `Upper Layer`의 `Attack1 0` 및 `IsInteracting`).

---

## 3. 주무기: 활 ([BowWeapon.cs](file:///c:/__Works__/_MythOfGuotaji_/MythOfGuotaji/Assets/_MythOfGuotaji/Scripts/Combat/BowWeapon.cs))

### 3.1 퀵 클릭 5연사 (Burst Volley)
* 마우스 좌클릭을 0.3초 미만으로 짧게 탭할 시, **1.5초간 5발의 화살을 연속 발사** (0.375초 간격).
* 이동하면서 5발을 연속으로 사격 가능하며, 대시 또는 단검 사용 시 부드럽게 캔슬 가능.
* **동시 피격 점감 공식 (Multi-hit Falloff)**:
  - 덩치가 큰 요괴나 다발 화살에 한 틱에 동시 적중하더라도 데미지가 점감 적용:
  - 1발: 100% (누적 100)
  - 2발: 50% (누적 150)
  - 3발: 25% (누적 175)
  - 4발: 12.5% (누적 187.5)
  - 공식: $Damage = Base \times (0.5)^{n-1}$ (감쇄 비율은 `WeaponStatsTableSO`에서 수정 가능).

### 3.2 차징 화살 (Linear Charged Shot)
* **차징 메커니즘**:
  - 0.3초부터 차징이 시작되어 최대 3.0초까지 충전.
  - 관통 사거리가 충전 시간에 비례하여 **0m에서 25m까지 선형으로 증가** ($Range = 25 \times \frac{t - 0.3}{2.7}$).
  - 중간에 마우스 버튼을 놓아도 그 즉시 충전된 사거리만큼 발사.
  - 피해량 2배, 무제한 관통, 고속 비행(36m/s), 강력한 넉백(12.0)은 충전 시간과 무관하게 유지.
* **바닥 궤적 UI (Telegraph Ground Guide)**:
  - 0.3초 차징 시작과 동시에 캐릭터 발밑에서 마우스 조준 방향으로 **폭 0.3m, 길이 현재 사거리**의 반투명 황금 궤적 박스가 실시간으로 늘어나며 표시됨.

---

## 4. 보조무기: 단검 ([DaggerWeapon.cs](file:///c:/__Works__/_MythOfGuotaji_/MythOfGuotaji/Assets/_MythOfGuotaji/Scripts/Combat/DaggerWeapon.cs))

* **근접 베기 공격**:
  - 마우스 우클릭 시 조준 방향 전방 $150^\circ$, 사거리 **3.0m** 부채꼴 베기 (피해량 50, 넉백 9.0).
* **투사체/마법 반사 (Parrying Reflection)**:
  - 휘두르는 순간 반경 3.8m 내의 **적 원거리 투사체, 곡사 마법탄, 특수 구체**를 정확히 타격하면 **날아오던 각도 그대로 반사**.
  - **1미터 평면 정밀도 판정**: 단검 휘두르기 궤도를 지면 기준 **높이 1.0m 수평 평면**으로 계산하여, 공중에 높이 떠 있는 곡사 투사체는 즉시 반사되지 않고 지면 1m 궤도($y \in [0.35m, 1.65m]$)로 내려왔을 때만 반사되도록 정밀도 대폭 향상.
  - 반사된 투사체는 플레이어의 공격 판정(`IsFromPlayer = true`)으로 전환되어 적에게 1.5배의 피해를 반사하여 가격.
  - 반사 성공 시 카메라 셰이크와 청록색 초승달 검기 이펙트 발생.

---

## 5. 산해경 요괴 군집 및 공격 패턴 ([MonsterAI.cs](file:///c:/__Works__/_MythOfGuotaji_/MythOfGuotaji/Assets/_MythOfGuotaji/Scripts/Actor/MonsterAI.cs))

| 요괴 명칭 | 타입 | HP / 이속 | 공격력 / 사거리 / 쿨 | 주요 공격 패턴 및 메커니즘 |
|---|---|---|---|---|
| **산해경 잡몹** | `Minion` | 35 / 4.2 | 6 / 2.0m / 1.0s | **5m 돌진 & 착지 70cm 충격파**: 캐릭터가 5m 이내 접근 시 0.3초 예고선 후 최초 목표 지점까지 0.3초간 돌진. 돌진 직후 **반경 70cm 광역 공격** 판정 후, 2m 내에 플레이어가 남아있으면 즉시 근접 공격. |
| **맹독충** | `Minion_Archer` | 30 / 2.7 | 7 / 15.0m / 0.7s | **원거리 투사체 공격**: 6m/s 속도의 독침 투사체를 발사하고, 거리가 5m 이내로 좁혀지면 카이팅 후퇴. |
| **거북이** | `Minion_BigMon` | 60 / 2.0 | 12 / 20.0m / 4.0s | **20m 돌진 (인디케이터 폭 1.5m, 3m 넉백) & 3m 충격파**: 최대 20m 거리에서 플레이어 발견 시 바닥에 **20m x 1.5m 가이드라인** 생성 후 3초간 가속 돌진. **벽 차단 및 시선 체크**: 돌진 목표 설정 시 및 0.4초 예고 후 돌진 시작 시 시선 체크를 수행하여 플레이어가 벽 뒤에 있으면 돌진을 즉시 취소하고 우회 추격. **아군 충돌 무시**: 같은 편 몬스터는 충돌 무시로 통과하며, 오직 **플레이어 및 지형 장애물**에만 충돌 및 정지. 돌진 중 경로 내 플레이어에게 피해 및 3미터 넉백 부여. 완주/충돌 시 쿵! 사운드와 함께 **반경 3m 원형 충격파** 발동. |
| **주(鴸)** | `Minion_Zhu` | 50 / 1.2 | 18 / 20.0m / 3.0s | **곡사 광역 폭발**: 캐릭터 위치에 **지름 5m 원형 예고 GUI** 표시 후 포물선 투사체 발사. **무조건 2초 고정 비행** 후 착지 폭발 (5m 광역 데미지). 발사 후 0.2초 후 즉시 이동 재개. |
| **정예 수문장** | `Elite` | 120 / 3.5 | 15 / 1.8m / 1.2s | 높은 체력과 방어력, 사망 시 **보스방 열쇠(`BossKey`)** 드롭. |
| **구미호 요괴** | `FoxBoss` | 350 / 4.8 | 20 / 2.5m / 0.8s | 스테이지 최종 보스, 어그로 시 포효 애니메이션, 광폭화 연속 공격. |

---

## 6. 영혼석 & 생명 하트 드롭 & 헬퍼 영물 시스템 ([HelperCompanion.cs](file:///c:/__Works__/_MythOfGuotaji_/MythOfGuotaji/Assets/_MythOfGuotaji/Scripts/Actor/HelperCompanion.cs), [RecoveryHeart.cs](file:///c:/__Works__/_MythOfGuotaji_/MythOfGuotaji/Assets/_MythOfGuotaji/Scripts/Actor/RecoveryHeart.cs))

* **영혼석(ExpGem) 드롭**:
  - 몬스터는 사망 시 경험치를 직접 주지 않고 지면에 영혼석 보석을 드롭.
  - 플레이어는 보석을 직접 줍지 않으며 헬퍼가 진공 흡수 수집.
* **생명 하트(RecoveryHeart) 드롭 & 체력 회복**:
  - 요괴 처치 시 일정 확률(25%~100%)로 루비빛 생명 하트 드롭.
  - **이중 획득 지원**: 플레이어가 직접 몸(1.25m)으로 닿아서 획득하거나, 헬퍼가 3m 진공 자석으로 흡수 수집 가능.
  - **회복 수치 ScriptableObject 이관**: 하트 1개당 체력 회복량 **5 HP** (`PlayerStatsTableSO.heartHealAmount = 5f`, `MonsterStatsTableSO.heartHealAmount = 5f`).
  - 획득 시 거타지 몸에서 사랑스러운 **하트 버스트 이펙트(`HCFX_Heart`)** 발동.
* **헬퍼 (달빛 영혼 토끼, Helper_SpiritRabbit)**:
  - 공격력은 전무하며 평소 플레이어의 뒤를 따름.
  - **안전 구역 탐색**: 요괴가 없는 안전 구역(반경 8.5m 내 요괴 부재)에 위치한 보석만을 찾아 이동.
  - **3m 진공 흡수**: 헬퍼가 이동할 때 주변 **3.0m 이내의 모든 보석과 생명 하트를 진공청소기처럼 빨아들여 흡수**.
  - **위협 회피**: 5.5m 이내로 요괴가 접근하면 즉시 보석 줍기를 중단하고 플레이어 뒤로 긴급 도주.
  - **비술 제련 및 스킬 제시**:
    - 보석을 모아 레벨업 경험치가 가득 차면 머리 위에 황금 비술 구체를 띄우며 플레이어에게 스킬(Boon) 3지선다 선택창을 제시.

### 6.1 인게임 축복 업그레이드 마스터 테이블 ([InRunBoonTableSO.cs](file:///c:/__Works__/_MythOfGuotaji_/MythOfGuotaji/Assets/_MythOfGuotaji/Scripts/Data/InRunBoonTableSO.cs))

| 제목 (Title) | 스탯 유형 (Stat) | 0단계 기본 수치 (Step) | Max 수치 (Cap) | 최대 업그레이드 회수 | 기본 희귀도 |
|---|---|---|---|---|---|
| **용사의 숨결** (`Vigor`) | 최대 체력 | **+15 HP** | **+75 HP** | **5회** | `Common` |
| **축지법** (`SwiftWind`) | 이동 속도 | **+0.75 m/s** | **+3.0 m/s** | **4회** | `Common` |
| **바람 걸음** (`WindStep`) | 대시 쿨다운 단축 | **-0.15초** | **-0.60초** | **4회** | `Common` |
| **천마의 시위** (`BowDamage`) | 기본 화살 공격력 | **+2.5** | **+12.5** | **5회** | `Common` |
| **쾌속 속사** (`BowRate`) | 화살 연사 속도 | **+15%** | **+60%** | **4회** | `Common` |
| **관통시위** (`ArrowPierce`) | 화살 추가 관통 수 | **+1회** | **+3회** | **3회** | `Rare` |
| **산해경 신궁** (`ArrowMulti`) | 화살 동시 발사 수 | **+1발** | **+2발** | **2회** | `Legendary` |
| **섬광 관통사** (`ChargeSnipe`) | 차징 관통 사거리 | **+4.0m** | **+16.0m** | **4회** | `Rare` |
| **거암 격타** (`ChargeDamage`) | 차징 피해 배율 | **+0.25배** | **+1.0배** | **4회** | `Rare` |
| **신속한 당김** (`QuickDraw`) | 차징 소요 시간 단축 | **+15%** | **+45%** | **3회** | `Common` |
| **예리한 칼날** (`DaggerEdge`) | 단검 근접 공격력 | **+6.0** | **+30.0** | **5회** | `Common` |
| **비수 궤적** (`DaggerRadius`) | 단검 공격 반경 | **+0.3m** | **+1.2m** | **4회** | `Common` |
| **달빛 패링** (`MoonParry`) | 투사체 반사 피해 배율 | **+0.35배** | **+1.4배** | **4회** | `Rare` |
| **유연한 연계** (`FluidCadence`) | 글로벌 쿨다운(GCD) 단축 | **-0.06초** | **-0.24초** | **4회** | `Common` |
| **약초 채집** (`HeartHerb`) | 생명 하트 드롭 확률 | **+15%** | **+60%** | **4회** | `Common` |
| **생명의 샘** (`VitalSpring`) | 하트 획득 시 추가 회복량 | **+3.0 HP** | **+15.0 HP** | **5회** | `Common` |

* **최대 회수 도달 규칙**: 특정 축복이 최대 업그레이드 회수(`maxUpgradeCount`)에 도달하면 `[MAX]` 상태가 되어 이후 레벨업 선택지 풀에서 자동으로 제외됩니다.
* **상한선 클램핑**: 희귀도 배수가 적용되더라도 누적 수치가 `maxValue`를 절대 초과하지 않도록 자동 보정됩니다.

### 6.2 축복 희귀도 배수 및 적용 테이블 ([BoonRarityTableSO.cs](file:///c:/__Works__/_MythOfGuotaji_/MythOfGuotaji/Assets/_MythOfGuotaji/Scripts/Data/BoonRarityTableSO.cs))

| 희귀도 (Rarity) | 배수 (Multiplier) | 출현 가중치 (Weight) | UI 연출 색상 | 적용 방식 (Apply Mode) |
|---|---|---|---|---|
| **일반 (Common)** | **1.0x** | **70%** (70) | 연회색 (`#E6E6E6`) | 연속 스탯 곱연산 |
| **희귀 (Rare)** | **1.5x** | **24%** (24) | 청록/시안 (`#00D9FF`) | 연속 스탯 곱연산 |
| **전설 (Legendary)** | **2.2x** | **6%** (6) | 황금/앰버 (`#FFD700`) | 연속 스탯 곱연산 |

* **적용 방식 분리 규칙**:
  * `MultiplyValue`: HP, 이동속도, 공격력, 쿨다운 감소 등 연속형 수치는 `기본 상승 수치 × 희귀도 배수`로 증가량이 증폭됩니다.
  * `FixedValue`: 관통 수(+1회), 화살 발사 수(+1발) 등 이산형 정수 스탯은 밸런스 붕괴를 막기 위해 희귀도 배수를 곱하지 않고 고정 수치로 안전하게 적용됩니다.

---

## 7. 비주얼 이펙트 (VFX) 시스템 ([VFXManager.cs](file:///c:/__Works__/_MythOfGuotaji_/MythOfGuotaji/Assets/_MythOfGuotaji/Scripts/Core/VFXManager.cs), [ScreenDamageVignetteUI.cs](file:///c:/__Works__/_MythOfGuotaji_/MythOfGuotaji/Assets/_MythOfGuotaji/Scripts/UI/ScreenDamageVignetteUI.cs))

* **탄환류(Projectiles) 최우선 뎁스 보장**:
  - 모든 화살, 독침, 곡사 투사체의 렌더러에 `sortingOrder = 5000` 및 `renderQueue = 3500`을 강제 적용하여 바닥 이펙트나 폭발 파티클에 가려지지 않고 항상 최상단에 선명하게 렌더링.
* **전체 이펙트 투명도 70% 일괄 감쇄**:
  - `VFXManager`에서 스폰되는 모든 파티클 시스템의 StartColor, ColorOverLifetime 및 재질의 BaseColor/Color/TintColor 알파값을 70% 수준으로 자동 보정하여 캐릭터 가시성 확보.
* **추가 웨이브 스폰 연출 및 0.3초 대기**:
  - 2차, 3차 요괴 웨이브 출현 시 스폰 무리 중심에서 `HCFX_Buff_Down` 강림 결계 연출이 재생되며, 스폰된 몬스터들은 **0.3초간 대기 후 플레이어를 향해 행동 개시**.
* **화면 주변 피격 비네트 플래시 (Screen-Edge Damage Vignette)**:
  - 거타지 피격 시 화면 최외곽 테두리에 붉은 혈흔 비네트 플래시가 즉시 발동되어 시각적인 피격 인지 제공 (`_currentAlpha = 0.85f` -> 부드러운 감쇄).

| 카테고리 | 트리거 시점 | 사용 VFX 프리팹 | 크기(Scale) 및 연출 효과 |
|---|---|---|---|
| **화면 피격 플래시**| 거타지 피격 시 | 절차적 붉은 비네트 오버레이 | 화면 가장자리 붉은 플래시 페이드아웃 |
| **체력 회복 버프** | 생명 하트 획득 시 | `HCFX_Heart` | 본체 몸에서 분홍/루비빛 하트 파티클 비산 |
| **플레이어 대시** | Space / Left Shift 대시 시 | `HCFX_Dust_Dash_01` | 1.0 (지면 먼지 구름 및 대시 스피드 라인) |
| **웨이브 스폰 결계**| 2차, 3차 웨이브 스폰 시 | `HCFX_Buff_Down` | 2.5 (스폰 무리 중심 강림 소환 결계) |
| **활 차징 단계** | 마우스 좌클릭 홀드 중 | `HCFX_Charging_01_Step1 / Step2 / Step3` | 충전 시간에 따른 3단계 집중 기운 아우라 |
| **차징 화살 발사** | 차징 해제 발사 시 | `HCFX_Charging_01_Step3_End` | 1.2 (발사 순간 황금 파동 폭발) |
| **차징 화살 비행** | 화살 관통 비행 중 | `HCFX_Energy_06` (트레일, Order 4900) | 황금빛 관통 트레일 |
| **화살 적중** | 일반 요괴 타격 / 치명타 | `HCFX_Hit_04` / `HCFX_Hit_07` | 0.85 (스파크 타격) / 1.3 (황금 크리티컬 섬광) |
| **단검 베기** | 우클릭 근접 휘두르기 | `HCFX_Flash_02` | 1.2 (150도 초승달 섬광 검기) |
| **단검 투사체 반사**| 투사체/마법 타격 성공 시 | `HCFX_MagicCircle_01` + `HCFX_Hit_06` | 1.2 (반사 방진 마법진 및 고속 스파크) |
| **Minion 돌진/착지**| 5m 돌진 중 / 착지 직후 | `HCFX_Dust_01` / `HCFX_Dust_FloorHit_01` | 0.8 (돌진 흙먼지) / 0.9 (반경 70cm 바닥 착지 충격파) |
| **맹독충 독침** | 발사 머즐 / 투사체 본체 | `HCFX_Poison_01` / `HCFX_ElementOrb_04` | 0.7 (독 연기 머즐) / 0.75 (초록빛 원소 구체) |
| **BigMon 돌진** | 20m 가속 돌격 중 | `HCFX_Dust_Dash_01` (0.15초 주기) | 1.4 (질주 대시 흙먼지 분출) |
| **BigMon 충돌/정지**| 충돌 또는 20m 도달 시 | `HCFX_Dust_FloorHit_03` + `HCFX_Explosion_03` | 1.8 (반경 3m 지진파 바닥 균열 및 분진 폭발) |
| **주(鴸) 곡사 투사체**| 투사체 본체 / 트레일 | `HCFX_ElementOrb_01` / `HCFX_Fire_01` | 0.85 (보랏빛 화염 구체) / 0.7 (화염 궤적) |
| **주(鴸) 바닥 폭발** | 2초 후 착지 시 | `HCFX_Explosion_01` | 1.9 (반경 5m 광역 화염 폭발) |
| **영혼석 흡수** | 헬퍼의 보석 수집 시 | `HCFX_Marble_01_Get` | 1.2 (영혼 구체 획득 반짝임) |
| **레벨업 / 비술 제시**| 경험치 완충 / 축복 선택 시 | `HCFX_Buff_Up` | 1.2 (플레이어 상승 버프 빛기둥) |

---

## 8. 몬스터 스킬-스텟-VFX 매핑 테이블 ([MonsterSkillVFXTableSO.cs](file:///c:/__Works__/_MythOfGuotaji_/MythOfGuotaji/Assets/_MythOfGuotaji/Scripts/Data/MonsterSkillVFXTableSO.cs))

몬스터명 - 특수스킬 - 스텟 - VFX를 1:1 매핑하여 ScriptableObject로 집중 관리:

| 몬스터명 | 특수스킬 명칭 | 핵심 스텟 규격 | 시전/머즐 VFX | 투사체/트레일 VFX | 적중/폭발 VFX |
|---|---|---|---|---|---|
| **산해경 잡몹** | 5m 돌진 & 착지 70cm 충격파 | 사거리 5m, 피해 6, 쿨 1s, 전조 0.3s, 반경 0.7m, 넉백 1.5m | `HCFX_Dust_01` | `HCFX_Dust_01` | `HCFX_Dust_FloorHit_01` |
| **맹독충** | 맹독 투사체 원거리 사격 | 사거리 15m, 피해 7, 쿨 0.7s, 탄속 6m/s | `HCFX_Poison_01` | `HCFX_ElementOrb_04` | `HCFX_Poison_02` |
| **거북이** | 20m 가속 돌진 & 3m 충격파 | 사거리 20m, 폭 1.5m, 3초 가속, 피해 12, 넉백 3.0m, 충격파 3m | `HCFX_Dust_Dash_01` | `HCFX_Dust_Dash_01` | `HCFX_Dust_FloorHit_03` + `HCFX_Explosion_03` |
| **주(鴸)** | 지름 5m 곡사 화염탄 대폭발 | 사거리 20m, 피해 18, 쿨 3s, 비행 2s 고정, 정점 6m, 폭발 5m | `HCFX_Fire_01` | `HCFX_ElementOrb_01` | `HCFX_Explosion_01` |
| **정예 수문장** | 수문장의 철퇴 강타 | 사거리 1.8m, 피해 15, 쿨 1.2s, 전조 0.35s, 넉백 2.5m | `HCFX_Flash_01` | `HCFX_Dust_01` | `HCFX_Hit_06` |
| **구미호 보스** | 구미호의 포효 & 광폭화 연타 | 사거리 2.5m, 피해 20, 쿨 0.8s, 전조 0.3s, 넉백 3.0m | `HCFX_MagicCircle_01` | `HCFX_Charging_01_Step3` | `HCFX_Explosion_03` |

---

## 9. 스테이지 인런 축복(Boon) 밸런싱 (50% 감쇄 및 신규 전투 스텟)

* **생존 & 기동**:
  - `용사의 숨결`: 최대 체력 +15 (기존 30 대비 50% 감쇄)
  - `축지법`: 이동 속도 +0.75m/s (기존 1.5m/s 대비 50% 감쇄)
  - `바람 걸음`: 대시 쿨다운 -0.2초
* **활 & 연사**:
  - `천마의 시위`: 일반 화살 공격력 +2.5
  - `쾌속 속사`: 연사 속도 +15%
  - `관통시위`: 화살 관통 횟수 +1
  - `산해경 신궁`: 화살 발사 수 +1 (Legendary)
* **차징 화살**:
  - `섬광 관통사`: 차징 관통 사거리 +4m, 차징 피해 배율 +0.25배
  - `신속한 당김`: 차징 시간 15% 단축
* **단검 & 패링 반사**:
  - `예리한 칼날`: 단검 근접 공격력 +6
  - `달빛 패링`: 투사체 반사 피해 배율 +0.35배, 단검 반경 +0.3m
* **글로벌 쿨다운 & 생명 회복**:
  - `유연한 연계`: 무기 전환 GCD -0.08초
  - `약초 채집`: 생명 하트 드롭 확률 +15%
  - `생명의 샘`: 하트 획득 시 추가 회복 +3 HP
