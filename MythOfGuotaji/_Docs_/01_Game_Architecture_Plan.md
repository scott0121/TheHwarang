# 🏹 거타지의 전설 (Myth of Guotaji) — 게임 시스템 설계 및 아키텍처

본 문서는 한국 삼국유사의 "거타지" 설화와 산해경 동양 요괴를 모티브로 한 쿼터뷰 던전크롤링 로그라이크 슈팅 액션 RPG **《거타지의 전설》**의 종합 시스템 설계 및 아키텍처 문서입니다.

---

## 1. 프로젝트 개요 및 환경

* **프로젝트명**: 거타지의 전설 (Myth of Guotaji)
* **장르**: 쿼터뷰 슈팅 액션 RPG / 던전 크롤러 / 로그라이크
* **엔진 및 파이프라인**: Unity 6 (6000.6.6f1) + Universal Render Pipeline (URP 17.6.0)
* **입력 시스템**: Unity New Input System (1.20.0)
* **세계관 및 모티브**:
  * **삼국유사 거타지 설화**: 신라 진성여왕 시기 사신선에 탑승했던 명궁 거타지가 서해 작도(까치섬)에서 노인(서해신)의 부탁을 받아 요괴 여우를 활로 처치하는 이야기.
  * **산해경(山海經)**: 동양의 신비롭고 기괴한 요괴들이 가득한 미로 던전. 던전 최심부의 수장인 구미호(여우 요괴) 토벌.

---

## 2. 확정된 핵심 게임플레이 사양

* **조작 및 전투**: **WASD 이동 + 마우스 커서 조준/사격** 기반의 트윈스틱 슈터 스타일. 무빙샷 중심의 활(주무기)과 위기 탈출/넉백용 단검(보조무기) 운용.
* **미로 탐색 및 방 잠금 방식**: 일반 미로 방들은 문이 열려 있어 **자유롭게 이동(몬스터들의 방 간 심리스 추격 가능)**하되, **특정 함정방 / 열쇠방 / 보스방** 진입 시에만 문이 잠기는 **혼합(Hybrid) 방식**.
* **동료 시스템**: 시나리오 트리를 통해 합류한 동료가 **전투 현장에 최대 1~2명 직접 소환되어 함께 싸우는 AI 동료**로 참여 (미로 추종 이동, 어그로 분담, 고유 지원 공격).
* **3중 성장 구조 (영구 2종 + 스테이지 일시 1종)**:
  1. **시나리오 트리 (영구 / 초기화 불가)**: 인연 포인트로 해금. 던전 오픈, 동료 합류, 직업 방향성 분기 결정.
  2. **캐릭터 트리 (영구 / 언제든 리셋 가능)**: 직업별 고유 스킬 및 스탯 커스터마이징 (100% 포인트 환급).
  3. **스테이지 내 일시 성장 (로그라이크 런 한정)**: 요괴 처치 경험치로 레벨업 시 3지선다 무작위 축복(Boon) 획득. 스테이지 실패 또는 종료 시 초기화.

---

## 3. 전체 시스템 아키텍처 다이어그램

```mermaid
flowchart TD
    subgraph Core["코어 매니저 & 데이터 레이어"]
        GM["GameManager (게임 상태/루프)"]
        SaveSys["SaveSystem (영구 세이브: 시나리오/캐릭터 트리)"]
        Pool["ObjectPoolManager (화살/요괴/이펙트 풀)"]
        Boot["DungeonBootstrapper (씬 자동 조립)"]
    end

    subgraph Meta["메타 성장 & 이중 트리 시스템"]
        ST["시나리오 트리 (인연 포인트 / 초기화 불가)"]
        CT["캐릭터 트리 (직업별 커스텀 / 리셋 가능)"]
        ST -->|던전 해금 / 동료 합류 / 직업 방향 결정| GM
        ST -->|전직/클래스 해금| CT
    end

    subgraph StageLoop["스테이지 런타임 (로그라이크 루프)"]
        DG["절차적 미로 생성기 (N x N 그리드)"]
        Room["RoomController & DoorSocket (혼합 잠금)"]
        Combat["전투 시스템 (활 무빙샷 + 단검 베기 + AI 동료)"]
        Horde["산해경 요괴 군집 (Flocking Separation)"]
        InRun["InRunProgression (3지선다 축복/레벨업)"]
    end

    GM --> StageLoop
    CT -->|스탯/스킬 패시브 적용| Combat
    DG -->|시작방 → 미로(열쇠방 탐색) → 보스방(여우 요괴)| Room
    Room --> Combat
    Combat --> InRun
    StageLoop -->|클리어 시 인연 포인트/보상 획득| Meta
    StageLoop -->|실패 시 스테이지 초기화 & 재도전| DG
```

---

## 4. 모듈별 문서 가이드

* **전투 및 카메라 시스템**: [02_Combat_and_Camera_System.md](file:///c:/__Works__/_MythOfGuotaji_/_Docs_/02_Combat_and_Camera_System.md)
* **절차적 미로 던전 생성기**: [03_Procedural_Maze_Dungeon_System.md](file:///c:/__Works__/_MythOfGuotaji_/_Docs_/03_Procedural_Maze_Dungeon_System.md)
* **성장 및 이중 트리 / 인런 축복 시스템**: [04_Progression_and_Dual_Tree_System.md](file:///c:/__Works__/_MythOfGuotaji_/_Docs_/04_Progression_and_Dual_Tree_System.md)
* **플레이테스트 및 디버그 가이드**: [05_Playtest_and_Debug_Guide.md](file:///c:/__Works__/_MythOfGuotaji_/_Docs_/05_Playtest_and_Debug_Guide.md)
