# 🎮 05. 플레이테스트 및 디버그 가이드

본 문서는 유니티 에디터 상에서 《거타지의 전설》을 즉시 실행하고 각 시스템을 테스트하는 방법과 단축키 및 디버그 패널 사용법을 안내합니다.

---

## 1. 프로젝트 실행 방법

1. **Unity Editor 6000.6.6f1**에서 본 프로젝트를 엽니다.
2. 프로젝트 뷰에서 [`Assets/Scenes/SampleScene.unity`](file:///c:/__Works__/_MythOfGuotaji_/MythOfGuotaji/Assets/Scenes/SampleScene.unity)를 더블 클릭하여 로드합니다.
3. 상단의 **Play(▶)** 버튼을 누릅니다.
4. [`DungeonBootstrapper.cs`](file:///c:/__Works__/_MythOfGuotaji_/MythOfGuotaji/Assets/_MythOfGuotaji/Scripts/Core/DungeonBootstrapper.cs)의 `[RuntimeInitializeOnLoadMethod]`가 작동하여:
   * 쿼터뷰 카메라 자동 생성 및 각도/Lookahead 세팅
   * 거타지 플레이어 캐릭터 자동 스폰 (활 및 단검 장착)
   * $6 \times 6$ 절차적 미로 던전 (시작방, 일반방, 열쇠방, 보스방) 자동 생성
   * 인게임 HUD 및 F1 디버그 패널이 즉시 활성화됩니다.

---

## 2. 조작 가이드 (Key Bindings)

| 동작 | 입력 키 / 조작 | 설명 |
| :--- | :--- | :--- |
| **이동** | `W`, `A`, `S`, `D` | 쿼터뷰 8방향 이동 (마우스 조준과 독립된 무빙샷 가능) |
| **조준** | `마우스 커서` | 지면 기준 마우스 방향 상시 조준 + 카메라 가변 시야 확장 |
| **주무기 (활 사격)** | `마우스 좌클릭` | 연사 사격 (스킬에 따라 멀티샷, 관통, 치명타 적용) |
| **보조무기 (단검 베기)** | `마우스 우클릭` | 근접 부채꼴 범위 공격 (포위 탈출 및 강력한 넉백) |
| **회피 대시** | `Space` 또는 `Left Shift` | 조준 방향(또는 이동 방향)으로 순간 회피 (무적 판정) |
| **개발 제어판 토글** | `F1` | 화면 우측 상단 테스트 제어판 켜기/끄기 |

---

## 3. 화면 UI 안내

* **좌측 상단 HUD**:
  * **거타지 HP**: 현재 체력 / 최대 체력 실시간 표시
  * **보스방 열쇠 상태**:
    * ❌ 미보유: 보스방 진입 불가 (노란색 열쇠방을 찾아 정예 수문장을 격파해야 함)
    * 🔑 보유 중: 보스방(붉은색 방)의 문이 개방되며 여우 요괴와 결전 가능
  * **처치한 요괴 수**: 누적 킬 카운트
  * **런 레벨 & EXP**: 스테이지 내 일시 레벨업 진행도
* **중앙 모달 UI (축복 선택)**:
  * 런 레벨업 시 게임이 일시 정지되며 3장의 무작위 축복 카드 중 하나를 선택할 수 있습니다.

---

## 4. F1 개발 & 테스트 제어판 ([ProgressionDebugUI.cs](file:///c:/__Works__/_MythOfGuotaji_/MythOfGuotaji/Assets/_MythOfGuotaji/Scripts/UI/ProgressionDebugUI.cs))

화면 우측 상단에 표시되는 개발 패널을 통해 다음 기능을 원클릭으로 테스트할 수 있습니다:
* **🎲 미로 던전 즉시 재배치**: 새로운 무작위 시드로 $6 \times 6$ 미로 던전을 실시간 재구축.
* **🔑 보스방 열쇠 즉시 획득 (Cheat)**: 열쇠방을 거치지 않고 보스방 문을 즉시 개방.
* **잡몹 10마리 소환**: 현재 플레이어 주변에 산해경 미니언 10마리 즉시 스폰 (Flocking 군집 테스트).
* **정예 열쇠 수문장 소환**: 처치 시 보스방 열쇠를 드롭하는 엘리트 요괴 스폰.
* **🦊 여우 요괴(최종 보스) 소환**: 던전 보스 구미호 즉시 스폰 및 전투 테스트.
* **인연 포인트 +5 / 스킬 포인트 +3**: 성장 재화 치트 지급.
* **🔄 캐릭터 스킬 트리 전체 리셋**: 투자한 스킬 포인트를 100% 회수하고 초기화하는 동작 검증.

---

## 5. 샘플 ScriptableObject 에셋 생성 메뉴

유니티 상단 메뉴 바에서 **`MythOfGuotaji > Generate Sample ScriptableObjects`**를 클릭하면 [`Assets/_MythOfGuotaji/Data/`](file:///c:/__Works__/_MythOfGuotaji_/MythOfGuotaji/Assets/_MythOfGuotaji/Data) 경로에 시나리오 노드, 스킬 노드, 챕터 테마 샘플 에셋이 자동 생성됩니다.
