# 🏹 02. 전투 및 카메라 시스템 (Phase 1)

본 문서는 《거타지의 전설》의 쿼터뷰 카메라, 트윈스틱 조작, 활/단검 하이브리드 전투 메커니즘, AI 동행 동료, 산해경 대규모 요괴 군집 AI 구현 상세를 다룹니다.

---

## 1. 쿼터뷰 카메라 시스템 ([QuarterViewCamera.cs](file:///c:/__Works__/_MythOfGuotaji_/MythOfGuotaji/Assets/_MythOfGuotaji/Scripts/Camera/QuarterViewCamera.cs))

원거리 활 슈팅 액션의 재미를 극대화하기 위해 다음과 같은 기능을 구현했습니다:
* **고정 쿼터뷰 각도**: 수평면 기준 약 $52^\circ$ 하향 시야각으로 캐릭터 주변 및 원거리 적들을 한눈에 조망.
* **Dynamic Lookahead Offset**:
  * 마우스 스크린 좌표를 지면 평면(Ground Plane)에 Raycast하여, 거타지 위치에서 마우스 방향으로 카메라 시야를 부드럽게 확장(`maxLookaheadDistance = 4.5m`).
  * 플레이어가 조준하는 방향의 먼 곳까지 미리 시야가 확보되어 원거리 저격 및 카이팅이 용이.
* **Smooth Follow & Shake**:
  * `Vector3.SmoothDamp`를 통한 부드러운 위치 보간.
  * 강력한 피격 또는 치명타 발생 시 `TriggerShake(intensity, duration)`로 손맛 제공.

---

## 2. 거타지 조작 및 전투 시스템 ([PlayerController.cs](file:///c:/__Works__/_MythOfGuotaji_/MythOfGuotaji/Assets/_MythOfGuotaji/Scripts/Actor/PlayerController.cs))

* **캐릭터 조작 및 마우스 방향 조준 (무빙샷)**:
  * `WASD`: 캐릭터의 8방향 자유 이동 (`_moveSpeed = 7.5f`).
  * `Mouse Raycast`: 캐릭터의 상체 및 시선은 마우스 커서의 지면 투영 좌표를 실시간으로 조준(`_aimDirection`).
  * `Space / Left Shift`: 이동 방향(또는 조준 방향)으로 순간 고속 이동하는 대시 회피(`_dashSpeed = 16f`, 무적 시간 제공, 쿨다운 1.0초).
* **주무기: 활 ([BowWeapon.cs](file:///c:/__Works__/_MythOfGuotaji_/MythOfGuotaji/Assets/_MythOfGuotaji/Scripts/Combat/BowWeapon.cs))**:
  * **일반 사격 (좌클릭 탭)**:
    * 마우스 방향으로 **사거리 15m**의 기본 화살 발사.
    * **멀티샷(Multi-shot)**: `arrowCount`에 따라 부채꼴(`spreadAngle = 12°`)로 화살 분배 발사.
    * **관통(Pierce)**: `pierceCount`만큼 요괴를 뚫고 지나가며 다수 타격.
    * 전용 프리팹: `Arrow_Projectile.prefab` (풀링 키: `DefaultNormalArrow`).
  * **강력한 한발 (좌클릭 최대 2초 모으기 차징샷)**:
    * 좌클릭을 누르고 있으면 최대 2.0초간 영기를 응축.
    * 2.0초 달성(또는 1.5초 이상 충전 후 릴리즈) 시 마우스 방향으로 발사.
    * **피해량 2배 (`_baseDamage * 2.0f`)**, **사거리 25m**, **무제한 관통(Unlimited Pierce)**, **강력한 넉백(`_chargedKnockback = 12.0f`)**.
    * **화살 개수(멀티샷) 업그레이드의 영향을 받지 않는 순수 단발 고정**.
    * 전용 프리팹: `Arrow_Charged.prefab` (풀링 키: `DefaultChargedArrow`).
* **보조무기: 단검 ([DaggerWeapon.cs](file:///c:/__Works__/_MythOfGuotaji_/MythOfGuotaji/Assets/_MythOfGuotaji/Scripts/Combat/DaggerWeapon.cs))**:
  * 마우스 우클릭 시 조준 방향 전방 $140^\circ$, 반경 3.2m 부채꼴 범위 내 모든 적을 일괄 베어내는 근접 위기 탈출 기술.
* **화살 투사체 ([Projectile.cs](file:///c:/__Works__/_MythOfGuotaji_/MythOfGuotaji/Assets/_MythOfGuotaji/Scripts/Combat/Projectile.cs))**:
  * 비행 거리 누적 추적: 일반 화살은 15m, 차징 화살은 25m 도달 시 자동 반환/소멸.
  * 무제한 관통 투사체는 중복 타격 방지(`_hitInstanceIds`)를 적용하여 다수의 요괴 무리를 쾌감 있게 관통 타격.

---

## 3. 산해경 요괴 군집 AI ([MonsterAI.cs](file:///c:/__Works__/_MythOfGuotaji_/MythOfGuotaji/Assets/_MythOfGuotaji/Scripts/Actor/MonsterAI.cs))

수십~수백 마리의 요괴가 화면에 동시 등장해도 자연스럽게 거타지를 포위하도록 군집 알고리즘을 적용했습니다:
* **로컬 분리 (Flocking Separation)**:
  * 인근 몬스터들과의 거리를 기반으로 반발력 벡터를 계산하여, 몬스터들이 서로 겹치지 않고 자연스럽게 부채꼴 형태로 플레이어를 포위하며 압박.
* **피격 넉백 물리**:
  * 화살이나 단검 피격 시 충격 벡터 방향으로 밀려난 후 지수 감쇠 보간(`Vector3.Lerp`)으로 원래 추격 속도로 복귀.
* **요괴 분류 및 공격 패턴**:
  * `Minion (산해경 잡몹)`:
    * **돌진 시작 거리**: 플레이어 주변 **5m** 이내 접근 시 발동.
    * **고정 목표 돌진**: 돌진 시작 시 플레이어의 당시 위치를 락온하며, 플레이어가 이탈하더라도 **최초 목표지점까지 끝까지 돌진**. 0.3초간 바닥 예고 가이드라인 표시 후 0.3초간 돌진.
    * **도착 즉시 70cm 광역 공격**: 돌진 도착 지점 반경 **70cm(0.7m)** 내 플레이어가 있을 경우 광역 피해 부여.
    * **2m 후속 근접 공격 연계**: 도착 후 반경 **2m** 내 플레이어가 위치해 있으면 즉시 회전하여 후속 근접 바이트 공격 감행.
  * `Minion_Archer (맹독충)`: 원거리 투사체 발사형 (사거리 15m, 쿨타임 0.7s, 탄속 6m/s, 카이팅 거리 4.5m).
  * `Minion_BigMon (거북이)`:
    * **탐지 및 공격 사거리**: 평상시 느릿하게 이동하다가 최대 **20m** 거리에서 플레이어를 발견 시 시전.
    * **가이드라인 예고**: 바닥에 길이 **20m**, 폭 **0.3m** 직사각형 궤적 UI 투영.
    * **5초간 20m 점진적 가속 돌진**: 등속이 아닌 점진적 가속 곡선($s(u) = 20 \times u^2$)으로 처음엔 느리게 출발하여 점차 고속으로 5초간 20m 끝까지 돌진. 돌진 도중 플레이어나 벽에 충돌 시 즉시 정지.
    * **쿵! 사운드 & 3m 원형 충격파 공격**: 20m 완주 또는 벽/플레이어 충돌 정지 시 "쿵!" 저음 타격 사운드, 화면 흔들림과 함께 **반경 3m 원형 충격파**로 광역 피해 및 강력한 넉백 부여.
  * `Minion_Zhu (주(鴸))`: 사거리 20m, 쿨 3초, 5m 원형 전조 장판 후 2초 고정 포물선 폭발 투사체 투하.
  * `Elite`: 열쇠방 수문장 요괴 (높은 체력, 처치 시 보스방 열쇠 드롭).
  * `FoxBoss`: 최종 보스 여우 요괴 (강력한 체력과 공격력, 처치 시 스테이지 클리어 확정).

---

## 4. AI 동행 동료 시스템 ([CompanionAI.cs](file:///c:/__Works__/_MythOfGuotaji_/MythOfGuotaji/Assets/_MythOfGuotaji/Scripts/Actor/CompanionAI.cs))

* **미로 추종 이동**: 거타지와의 거리를 $1.8\text{m} \sim 3.5\text{m}$ 범위로 유지하며 미로를 따라 이동.
* **자동 지원 사격 & 어그로 분담**: 사거리($8\text{m}$) 내 요괴를 자동 탐지하여 뇌전 지원 공격을 가하고, 요괴들의 시선을 끌어 거타지의 사격각을 벌어줌.
