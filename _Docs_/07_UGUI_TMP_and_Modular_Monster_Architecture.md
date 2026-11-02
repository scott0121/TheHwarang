# UGUI + TextMeshPro UI 프레임워크 및 모듈형 몬스터 AI 아키텍처 가이드

> **작성일:** 2026-11-01  
> **프로젝트:** MythOfGuotaji  
> **상태:** 구현 완료 및 빌드 무결성 검증 완료 (`dotnet build`: 0 Errors / `void OnGUI` 잔존: 0건)

---

## 1. 개요 및 달성 성과

본 문서는 스팀 출시 상용화 기준에 부합하도록 레거시 IMGUI(`OnGUI`) 시스템을 완전 제거하고, 반응형 **UGUI + TextMeshPro 프레임워크**로의 전역 마이그레이션 및 3,600여 줄의 거대 단일 God Class였던 **`MonsterAI.cs`를 전략 패턴(Strategy Pattern) 기반으로 모듈화**한 아키텍처 사양과 개발 가이드를 기술합니다.

### 핵심 달성 지표
| 지표 항목 | 전환 전 | 전환 후 | 개선 효과 |
|---|---|---|---|
| **프로젝트 전역 `OnGUI` 호출 수** | 8개 컴포넌트 (100% 의존) | **0건 (완전 소멸)** | 매 프레임 GC DrawCall 스파이크 및 해상도 깨짐 완전 해결 |
| **UI 렌더 파이프라인** | IMGUI 레거시 드로우 | UGUI Canvas + TextMeshPro (SDF) | 4K/QHD/FHD 완벽 반응형 스케일링, 고품질 타이포그래피 |
| **`MonsterAI.cs` 코드 라인 수** | 3,632 라인 (God Class) | **893 라인** | 약 **75% 슬림화**, 공통 상태/물리/피격 루틴만 유지 |
| **몬스터별 독립 Behavior 클래스** | 0개 (단일 거대 switch-case) | **16개 독립 클래스** | 몬스터 간 결합도 제로, 신규 몬스터 추가 시 기존 코드 수정 불필요 |
| **빌드 안정성 (`Assembly-CSharp.csproj`)** | 컴파일 오류 위험 상존 | **BUILD OK (0 errors)** | CI/빌드 파이프라인 100% 호환성 확보 |

---

## 2. UGUI + TextMeshPro 프레임워크 구조

프로젝트 경로: `Assets/_MythOfGuotaji/Scripts/UI/Framework/`

```
Assets/_MythOfGuotaji/Scripts/UI/
├── Framework/
│   ├── UIFontProvider.cs       # NotoSansKR-VF SDF 동적 생성 및 Fallback 폰트 관리자
│   ├── UITheme.cs              # 스팀 다크 판타지 디자인 시스템 (색상 팔레트, 폰트 크기, 소팅 레이어)
│   ├── UIKit.cs                # 프로그래머블 UGUI 위젯 팩토리 (버튼, 패널, 텍스트, 프로그레스바)
│   ├── UIScreen.cs             # 화면 수명주기(Show, Hide, Toggle) 및 Canvas/CanvasGroup 추상 기반 클래스
│   ├── UILayout.cs             # RectTransform 앵커/피벗 프리셋 및 AutoLayout 헬퍼
│   ├── UIText.cs               # TMP_Text 래퍼 (타이포그래피 스타일 원클릭 주입)
│   └── UIEventSystem.cs        # 씬 전환 시 EventSystem 자동 생성 및 입력 가드
```

### 2.1 주요 컴포넌트 역할

1. **`UIFontProvider`**:
   - `Resources/Fonts/NotoSansKR-VF.ttf`를 런타임에 로드하여 동적 TextMeshPro Font Asset을 생성하고 캐싱합니다.
   - 프로젝트 내 모든 TMP_Text 생성 시 한글 폰트를 자동 주입하여 네모 박스(Tofu) 현상을 원천 방지합니다.
2. **`UITheme`**:
   - 일관된 스팀 다크 판타지 룩앤필을 위한 디자인 토큰(Background, Panel, AccentGold, TextNormal, Health/Exp 색상 등)을 집중 관리합니다.
   - Canvas SortingOrder(`HUD: 10`, `Popup: 50`, `Debug: 100`, `Notification: 200`)를 표준화하여 렌더링 순서 꼬임을 방지합니다.
3. **`UIKit`**:
   - 에셋 프리팹 종속 없이 런타임에 코드로 완벽한 UGUI 컴포넌트를 조립할 수 있는 빌더 API를 제공합니다.
   - `CreateCanvas()`, `CreatePanel()`, `CreateButton()`, `CreateProgressBar()`, `CreateText()` 등 원스톱 생성 지원.
4. **`UIScreen`**:
   - 모든 풀스크린 또는 팝업 UI의 베이스 클래스로 `Canvas`, `CanvasGroup`, `GraphicRaycaster`를 자동 관리합니다.
   - 페이드 인/아웃 전환 및 가시성 토글(`ToggleVisibility()`)을 기본 제공합니다.

---

## 3. 마이그레이션 완료 컴포넌트 목록

프로젝트 내 IMGUI가 완전 제거되고 UGUI+TMP로 재구축된 핵심 컴포넌트 6종 내역입니다:

1. **`ChargeIndicatorUI.cs`** (`Assets/_MythOfGuotaji/Scripts/UI/ChargeIndicatorUI.cs`):
   - 플레이어 공격 차징 게이지 및 패링 타이밍 인디케이터.
   - UGUI WorldSpace/ScreenSpace Canvas + Radial/Horizontal Image Fill + TMP_Text 전환 완료.
2. **`MonsterHealthBarManager.cs`** (`Assets/_MythOfGuotaji/Scripts/UI/MonsterHealthBarManager.cs`):
   - 필드 내 몬스터들의 체력바와 상태 이상 아이콘.
   - UGUI Overlay Canvas + 객체 풀링(Object Pool) 기반 `BarItem` 구조로 전환 (DrawCall 최적화).
3. **`HelperCompanion.cs`** (`Assets/_MythOfGuotaji/Scripts/Actor/HelperCompanion.cs`):
   - 동료 헬퍼 캐릭터의 머리 위 상태 텍스트.
   - WorldSpace Canvas + Dynamic TMP_Text 라벨로 고해상도 렌더링.
4. **`ProgressionDebugUI.cs`** (`Assets/_MythOfGuotaji/Scripts/UI/ProgressionDebugUI.cs`):
   - [F1] 키 토글 디버그 콘솔.
   - `UIScreen` 기반의 UGUI 패널로 재구축. 런 레벨, 경험치 강제 주입, 클리어/사망 치트 버튼 제공.
5. **`MonsterTestController.cs`** (`Assets/_MythOfGuotaji/Scripts/Test/MonsterTestController.cs`):
   - 몬스터 스폰 및 AI 패턴 검증용 테스트 베드 패널.
   - 드롭다운/버튼 그리드 UGUI 레이아웃으로 개편.
6. **`IntroOutgameManager.cs`** (`Assets/_MythOfGuotaji/Scripts/UI/IntroOutgameManager.cs`):
   - 게임 인트로 타이틀, 세이브 로드, 신당(허브) 스테이지 선택 및 코어 메모리 해금 UI.
   - 데이터 로직(`ScenarioDataSpecV11b.cs`)을 분리하고 순수 UGUI + TMP 스크린 기반으로 전환 완료.

---

## 4. 모듈형 몬스터 AI 아키텍처

프로젝트 경로: `Assets/_MythOfGuotaji/Scripts/Actor/Monsters/`

```
Assets/_MythOfGuotaji/Scripts/Actor/Monsters/
├── Core/
│   ├── MonsterBehavior.cs          # 전략 패턴 기반 추상 행동 베이스 클래스
│   ├── MonsterBehaviorRegistry.cs  # MonsterType 어트리뷰트 기반 자동 리플렉션 팩토리
│   └── MonsterTelegraphs.cs        # 장판 예고(Red Circle/Rect), 궤적 이펙트 공용 헬퍼
└── Behaviors/ (16종 개별 클래스)
    ├── MeleeMonsterBehavior.cs     # 일반 근접 공격 베이스
    ├── MinionBehavior.cs           # 잡몹 (빠른 러시, 기본 타격)
    ├── ArcherBehavior.cs           # 원거리 궁수 (투사체 발사, 카이팅 거리 유지)
    ├── BigMonBehavior.cs           # 대형 몬스터 (슈퍼아머 휘두르기, 내려찍기)
    ├── ZhuBehavior.cs              # 저팔계/돌진수 (벽 충돌 기절, 맹렬한 돌진)
    ├── EliteBehavior.cs            # 정예 몬스터 (다단 베기, 투척 폭탄)
    ├── FoxBossBehavior.cs          # 구미호 보스 (단계별 페이즈, 분신 소환, 영혼 불꽃)
    ├── DokkaebiFireBehavior.cs     # 도깨비불 (패링 가능한 유도 화염구)
    ├── BoarChargerBehavior.cs      # 멧돼지 (선형 궤적 돌진)
    ├── SpiderTetherBehavior.cs     # 거미 (유저를 끌어당기는 거미줄 결속)
    ├── MudSlimeBehavior.cs         # 진흙 슬라임 (이속 저하 장판 생성)
    ├── BombSparrowBehavior.cs      # 폭탄 참새 (카미카제 자폭 다이빙)
    ├── TotemTurretBehavior.cs      # 토템 포탑 (고정형 회전 탄막)
    ├── IronShieldBehavior.cs       # 강철 방패병 (전방 데미지 100% 흡수/반사)
    ├── EnergyAbsorberBehavior.cs   # 마력 흡수체 (스킬 흡수 후 방출)
    └── PhantomFoxBehavior.cs       # 환영 여우 (은신, 백스텝 암습)
```

### 4.1 동작 원리

1. **`MonsterAI.cs`의 경량화**:
   - `MonsterAI`는 오직 공통 기능(체력 관리, 길찾기/물리 이동, 피격/사망 처리, 애니메이터 연동)만 담당합니다.
   - 시작(`Awake`) 시 `MonsterBehaviorRegistry.CreateBehavior(this, _type)`를 호출하여 자신의 타입에 맞는 전략 클래스를 주입받습니다.
   - 모든 공격 및 특수 스킬 실행은 주입받은 `_behavior.PerformAttack()` 또는 `_behavior.TickCustomBehavior()`로 위임됩니다.

2. **개방-폐쇄 원칙 (Open-Closed Principle, OCP) 준수**:
   - 새로운 몬스터 타입을 추가할 때 기존의 거대한 `MonsterAI.cs` 소스 코드를 단 1줄도 수정할 필요가 없습니다.

---

### 4.2 돌진류 몬스터 및 기동 스킬 아키텍처 (Charge & Leap Skill Policy)

#### 1) 돌진 목표점 및 충돌 정책
- **최대 거리 질주 원칙**: 캐릭터와의 거리가 최대 돌진 거리보다 가깝더라도, 돌진 목표점은 벽/닫힌 문에 가로막히지 않는 한 **항상 최대 사거리(거북이 20m, 멧돼지 15m)**까지 설정됩니다.
- **오브젝트 충돌 필터링**: 돌진 중에는 **벽, 닫힌 문, 플레이어**만을 유효 충돌체로 판정하며, 다른 몬스터나 장애물과는 충돌하지 않고 자연스럽게 관통하여 비벼짐이나 멈춤 현상을 원천 방지합니다.

#### 2) 돌진류 기동 스킬 3대 유형 (ChargeSkillType)
MonsterSkillTableSO에 정의된 3가지 기동 메카닉:
1. **StopOnPlayer**: 돌진 중 플레이어 충돌 시 즉시 정지하며 주변 반경에 충격파 폭발 피해 (기본 거북이/멧돼지).
2. **PenetratePlayer**: 플레이어를 타격 및 넉백시키고 정지하지 않고 목표점까지 끝까지 관통 질주.
3. **LeapSmash (도약 포물선 강타)**: 목표점까지 공중으로 날아가 착지 시 범위 피해 (비행 중 지상 충돌 없음, 정점 높이/체공 시간 설정 가능).

#### 3) 엘리트 도깨비 (Monster_Elite) 2번 스킬 사양
- **스킬 ID**: elite_leap_smash
- **발동 조건**: 플레이어 거리 20m 이내, 쿨다운 6초
- **시전 시퀀스**:
  - 플레이어 현재 위치에 **5m 원형 텔레그래프 장판(패링 불가)** 생성
  - 0.5초 Windup 후 **1.5초 동안 최대 10m 높이 포물선 도약 비행** (지상 충돌 없음)
  - 착지 시 **5m 반경 광역 충격파 피해** 및 강한 넉백, 패링 불가(Unparryable)

---

## 5. 신규 컨텐츠 확장 가이드

### 5.1 신규 몬스터 추가 3단계 가이드

#### 1단계: `MonsterType` 열거형에 ID 등록
`Assets/_MythOfGuotaji/Scripts/Core/GameTypes.cs` 또는 몬스터 타입 정의 열거형에 새 몬스터를 선언합니다:
```csharp
public enum MonsterType
{
    // ... 기존 몬스터들
    ShadowNinja = 17, // 신규 몬스터
}
```

#### 2단계: `MonsterBehavior`를 상속받는 독립 클래스 생성
`Assets/_MythOfGuotaji/Scripts/Actor/Monsters/ShadowNinjaBehavior.cs` 파일을 생성합니다:
```csharp
using UnityEngine;
using MythOfGuotaji.Actor;

namespace MythOfGuotaji.Actor
{
    // 어트리뷰트 등록만으로 팩토리에 자동 바인딩됩니다.
    [MonsterBehavior(MonsterType.ShadowNinja)]
    public class ShadowNinjaBehavior : MonsterBehavior
    {
        public override void ApplyPatternStats()
        {
            AttackRange = 3.5f;
            AttackCooldown = 2.0f;
            TelegraphDuration = 0.4f;
        }

        public override void PerformAttack()
        {
            LastAttackTime = Time.time;
            PlayAttackAnimation();
            // 특수 패턴: 플레이어 배후로 순간이동 후 참격
            TeleportBehindTarget();
        }

        private void TeleportBehindTarget()
        {
            if (Target == null) return;
            Vector3 behind = Target.position - Target.forward * 1.5f;
            Owner.transform.position = behind;
            Owner.transform.rotation = Target.rotation;
        }
    }
}
```

#### 3단계: 빌드 및 검증
- 새로 생성한 파일은 `dotnet build` 시 즉시 자동 컴파일되며, 게임 내에서 해당 `MonsterType`으로 스폰되면 즉시 작동합니다.

---

### 5.2 신규 UI 화면 추가 5단계 가이드

#### 1단계: `UIScreen` 상속 클래스 생성
`Assets/_MythOfGuotaji/Scripts/UI/MyCustomScreen.cs`를 생성합니다:
```csharp
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MythOfGuotaji.UI.Framework;

namespace MythOfGuotaji.UI
{
    public class MyCustomScreen : UIScreen
    {
        private TMP_Text _titleText;
        private Button _actionButton;

        protected override void Awake()
        {
            // 캔버스 정렬 순서 지정 (Theme 소팅 레이어 활용)
            SortingOrder = UITheme.Sorting.Popup;
            base.Awake();
            BuildUI();
        }

        private void BuildUI()
        {
            // 1. 반투명 배경 패널 생성
            var panel = UIKit.CreatePanel(RootCanvas.transform, "MainPanel", 
                new Vector2(600, 400), UITheme.Colors.DarkBackground);

            // 2. 제목 텍스트 추가
            _titleText = UIKit.CreateText(panel.transform, "TitleText", 
                "신규 시스템 타이틀", UITheme.Typography.HeadingMedium, UITheme.Colors.AccentGold);
            UILayout.SetAnchorAndPivot(_titleText.rectTransform, UILayout.Preset.TopCenter);
            _titleText.rectTransform.anchoredPosition = new Vector2(0, -30);

            // 3. 상호작용 버튼 생성
            _actionButton = UIKit.CreateButton(panel.transform, "ConfirmBtn", "확 인", () =>
            {
                Debug.Log("Button Clicked!");
                Hide();
            });
            UILayout.SetAnchorAndPivot(_actionButton.GetComponent<RectTransform>(), UILayout.Preset.BottomCenter);
            _actionButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 30);
        }
    }
}
```

#### 2단계: 화면 호출 및 가시성 제어
게임 내 컨트롤러나 매니저에서 다음과 같이 호출합니다:
```csharp
// 화면 표시
myScreen.Show();

// 화면 숨김
myScreen.Hide();

// 단축키 토글
myScreen.ToggleVisibility();
```

---

## 6. 결론

금번 리팩토링 및 마이그레이션을 통해 스팀 출시를 위해 반드시 해결해야 했던 **IMGUI 레거시 부채**와 **거대 몬스터 AI 결합도** 문제가 완전히 해결되었습니다. 

향후 추가될 모든 보스/일반 몬스터와 신규 상점/인벤토리/특성 UI는 위 가이드라인에 따라 단일 책임을 갖는 독립 모듈 형태로 안전하고 빠르게 확장할 수 있습니다.
