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

* **트윈스틱 이동 & 조준**:
  * `WASD`: 이동 방향과 관계없이 마우스 커서 평면 좌표를 실시간으로 바라보며 이동(무빙샷).
  * `Space / Left Shift`: 이동 방향(또는 조준 방향)으로 순간 고속 이동하는 대시 회피(`_dashSpeed = 16f`, 무적 시간 제공, 쿨다운 1.0초).
* **주무기: 활 ([BowWeapon.cs](file:///c:/__Works__/_MythOfGuotaji_/MythOfGuotaji/Assets/_MythOfGuotaji/Scripts/Combat/BowWeapon.cs))**:
  * 마우스 좌클릭 시 연사 사격.
  * **멀티샷(Multi-shot)**: `arrowCount`에 따라 부채꼴(`spreadAngle = 12°`)로 화살 분배 발사.
  * **관통(Pierce)**: `pierceCount`만큼 요괴를 뚫고 지나가며 다수 타격.
  * **치명타**: 기본 치명타 확률(15%) 및 200% 피해 계수.
* **보조무기: 단검 ([DaggerWeapon.cs](file:///c:/__Works__/_MythOfGuotaji_/MythOfGuotaji/Assets/_MythOfGuotaji/Scripts/Combat/DaggerWeapon.cs))**:
  * 마우스 우클릭 시 발동되는 근접 위기 탈출 기술.
  * 전방 $140^\circ$, 반경 3.2m 부채꼴 범위 내 모든 적을 일괄 베어내며 높은 넉백(`_knockback = 8f`)을 가해 거타지를 포위한 요괴 무리를 밀쳐냄.
* **화살 투사체 ([Projectile.cs](file:///c:/__Works__/_MythOfGuotaji_/MythOfGuotaji/Assets/_MythOfGuotaji/Scripts/Combat/Projectile.cs))**:
  * 레이캐스트 기반 연속 충돌 검출로 고속 이동 시 터널링(관통 누락) 방지.
  * 수명 만료 또는 관통 횟수 소진 시 [`ObjectPoolManager`](file:///c:/__Works__/_MythOfGuotaji_/MythOfGuotaji/Assets/_MythOfGuotaji/Scripts/Core/ObjectPoolManager.cs)로 자동 반환.

---

## 3. 산해경 요괴 군집 AI ([MonsterAI.cs](file:///c:/__Works__/_MythOfGuotaji_/MythOfGuotaji/Assets/_MythOfGuotaji/Scripts/Actor/MonsterAI.cs))

수십~수백 마리의 요괴가 화면에 동시 등장해도 자연스럽게 거타지를 포위하도록 군집 알고리즘을 적용했습니다:
* **로컬 분리 (Flocking Separation)**:
  * 인근 몬스터들과의 거리를 기반으로 반발력 벡터를 계산하여, 몬스터들이 서로 겹치지 않고 자연스럽게 부채꼴 형태로 플레이어를 포위하며 압박.
* **피격 넉백 물리**:
  * 화살이나 단검 피격 시 충격 벡터 방향으로 밀려난 후 지수 감쇠 보간(`Vector3.Lerp`)으로 원래 추격 속도로 복귀.
* **요괴 분류**:
  * `Minion`: 대규모 물량형 돌진 요괴.
  * `Elite`: 열쇠방 수문장 요괴 (높은 체력, 처치 시 보스방 열쇠 드롭).
  * `FoxBoss`: 최종 보스 여우 요괴 (강력한 체력과 공격력, 처치 시 스테이지 클리어 확정).

---

## 4. AI 동행 동료 시스템 ([CompanionAI.cs](file:///c:/__Works__/_MythOfGuotaji_/MythOfGuotaji/Assets/_MythOfGuotaji/Scripts/Actor/CompanionAI.cs))

* **미로 추종 이동**: 거타지와의 거리를 $1.8\text{m} \sim 3.5\text{m}$ 범위로 유지하며 미로를 따라 이동.
* **자동 지원 사격 & 어그로 분담**: 사거리($8\text{m}$) 내 요괴를 자동 탐지하여 뇌전 지원 공격을 가하고, 요괴들의 시선을 끌어 거타지의 사격각을 벌어줌.
