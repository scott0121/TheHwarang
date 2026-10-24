# 🌳 04. 성장 및 이중 트리 / 인런 축복 시스템

본 문서는 《거타지의 전설》의 3중 성장 아키텍처(불가역 메인 시나리오 트리, 언제든 리셋 가능한 직업별 캐릭터 트리, 스테이지 내부 한정 로그라이크 축복)와 영구 세이브 시스템을 다룹니다.

---

## 1. 3중 성장 시스템 비교 분석

| 구분 | ① 시나리오 트리 (Scenario Tree) | ② 캐릭터 트리 (Character Tree) | ③ 스테이지 일시 성장 (In-Run Boon) |
| :--- | :--- | :--- | :--- |
| **적용 범위** | 게임 전체 메타 진행도 | 직업별 고유 스킬 및 패시브 | 현재 스테이지 런(Run) 한정 |
| **소모 재화** | **인연 포인트** (보스 격파/스토리 획득) | 스킬 포인트 (레벨업/수련 획득) | 요괴 처치 경험치 / 제단 |
| **초기화 가능 여부** | **초기화 불가 (Irreversible)**<br>*(새 게임 시작 시에만 리셋)* | **언제든 100% 환급 리셋 가능**<br>*(보스/메타에 맞춰 자유 빌드 변경)* | **스테이지 실패/클리어 시 자동 소멸** |
| **주요 효과** | 신규 던전 오픈, AI 동료 합류, 직업 전직 갈래 | 화살 수 증가, 관통 수치, 단검 쿨감 | 화살 +1, 이동속도 +1.5, 최대체력 등 3지선다 |

---

## 2. 메인 시나리오 트리 ([ScenarioTreeManager.cs](file:///c:/__Works__/_MythOfGuotaji_/MythOfGuotaji/Assets/_MythOfGuotaji/Scripts/Progression/ScenarioTreeManager.cs))

* **노드 데이터 ([ScenarioNodeSO.cs](file:///c:/__Works__/_MythOfGuotaji_/MythOfGuotaji/Assets/_MythOfGuotaji/Scripts/Progression/ScenarioNodeSO.cs))**:
  * `nodeId`: 고유 식별자 (예: `ROOT_START`, `SCENE_RECRUIT_HOWON`).
  * `requiredAffinityPoints`: 소모 인연 포인트.
  * `prerequisiteNodeIds`: 선행 조건 노드 목록.
  * `exclusiveNodeIds`: 상호 배타 노드 (선택 시 반대쪽 선택지는 영구 봉인).
  * `unlockType`: `UnlockDungeon`(던전 해금), `RecruitCompanion`(동료 합류), `UnlockProfession`(직업 분기).
* **규칙 및 불변성**:
  * 한번 해금된 노드는 되돌릴 수 없으며 영구 세이브 데이터에 기록됩니다.

---

## 3. 직업별 캐릭터 트리 ([CharacterTreeManager.cs](file:///c:/__Works__/_MythOfGuotaji_/MythOfGuotaji/Assets/_MythOfGuotaji/Scripts/Progression/CharacterTreeManager.cs))

* **스킬 노드 ([CharacterSkillNodeSO.cs](file:///c:/__Works__/_MythOfGuotaji_/MythOfGuotaji/Assets/_MythOfGuotaji/Scripts/Progression/CharacterSkillNodeSO.cs))**:
  * 각 스킬마다 최대 레벨(MaxLevel)과 레벨당 스킬 포인트 비용 설정.
  * 수정자(Modifier): `ExtraArrows`(추가 화살), `PierceCount`(관통), `MoveSpeedPercent`(이속 증가) 등.
* **100% 포인트 환급 리셋 (`ResetAllSkills()`)**:
  * 유저가 언제든지 기존에 투자한 모든 포인트를 1원도 손실 없이 회수하고 새로운 빌드로 재구성할 수 있습니다.

---

## 4. 스테이지 내 일시 축복 ([InRunProgressionManager.cs](file:///c:/__Works__/_MythOfGuotaji_/MythOfGuotaji/Assets/_MythOfGuotaji/Scripts/Progression/InRunProgressionManager.cs))

* **런 레벨업 루프**:
  * 미로에서 요괴를 처치할 때마다 런 경험치(`currentRunExp`)를 획득.
  * 요구 경험치 달성 시 레벨업과 함께 `OnBoonSelectionRequested` 이벤트가 발생하며 게임이 일시 정지되고 3지선다 모달 UI가 화면에 표시.
* **축복 카드 ([InRunBoonSO.cs](file:///c:/__Works__/_MythOfGuotaji_/MythOfGuotaji/Assets/_MythOfGuotaji/Scripts/Progression/InRunBoonSO.cs))**:
  * 등급: Common, Rare, Legendary.
  * 플레이어가 1개를 선택하면 거타지의 런타임 스탯에 즉시 누적 적용.
  * 거타지가 사망하거나 스테이지를 클리어하면 축복과 레벨은 모두 리셋.

---

## 5. 영구 세이브 시스템 ([SaveSystem.cs](file:///c:/__Works__/_MythOfGuotaji_/MythOfGuotaji/Assets/_MythOfGuotaji/Scripts/Core/SaveSystem.cs))

* `Application.persistentDataPath` 경로의 `MythOfGuotaji_Save.json` 파일에 JSON 포맷으로 영구 저장/로드:
  * 해금된 시나리오 노드 ID 목록
  * 보유 인연 포인트 및 골드
  * 합류한 동료 목록
  * 현재 선택된 직업 및 캐릭터 스킬 투자 내역
