# 📜 《거타지의 전설 (Myth of Guotaji)》 문서 저장소 (`_Docs_`)

한국 삼국유사의 "거타지" 설화와 산해경 동양 요괴를 모티브로 한 쿼터뷰 로그라이크 던전 RPG **《거타지의 전설》**의 공식 개발 및 시스템 기획 문서 목록입니다.

---

## 📑 문서 목차

1. [01. 게임 시스템 설계 및 아키텍처 (Game Architecture Plan)](file:///c:/__Works__/_MythOfGuotaji_/_Docs_/01_Game_Architecture_Plan.md)
   * 프로젝트 개요 및 배경 설정, 6대 핵심 아키텍처 다이어그램, 계층 구조 및 모듈 로드맵
2. [02. 전투 및 카메라 시스템 (Phase 1)](file:///c:/__Works__/_MythOfGuotaji_/_Docs_/02_Combat_and_Camera_System.md)
   * 쿼터뷰 52° 고정 각도 및 가변 Lookahead 카메라, WASD/마우스 트윈스틱 무빙샷, 활/단검 하이브리드, AI 동행 동료, Flocking 군집 요괴 AI
3. [03. 절차적 미로 던전 생성 시스템 (Phase 2)](file:///c:/__Works__/_MythOfGuotaji_/_Docs_/03_Procedural_Maze_Dungeon_System.md)
   * $N \times N$ 미로 DFS Spanning Tree + 루프 생성, 시작방/보스방 고정, 열쇠방/함정방 유효 배치 알고리즘, 일반방 자유 이동 및 특수방 잠금 혼합형 도어 메커니즘, 챕터별 테마 스킨 분리
4. [04. 성장 및 이중 트리 / 인런 축복 시스템](file:///c:/__Works__/_MythOfGuotaji_/_Docs_/04_Progression_and_Dual_Tree_System.md)
   * 영구/초기화 불가 메인 시나리오 트리(인연 포인트), 언제든 100% 환급 리셋 가능한 직업별 캐릭터 트리, 스테이지 내 3지선다 일시 축복(로그라이크 런), JSON 영구 세이브
5. [05. 플레이테스트 및 디버그 가이드](file:///c:/__Works__/_MythOfGuotaji_/_Docs_/05_Playtest_and_Debug_Guide.md)
   * 유니티 에디터 실행 방법, 조작키 일람, HUD 안내, F1 원클릭 개발/테스트 제어판, 샘플 ScriptableObject 에셋 생성 메뉴

---

## 📂 유니티 소스 코드 경로 안내

* 코어 시스템: [`Assets/_MythOfGuotaji/Scripts/Core/`](file:///c:/__Works__/_MythOfGuotaji_/MythOfGuotaji/Assets/_MythOfGuotaji/Scripts/Core)
* 카메라 제어: [`Assets/_MythOfGuotaji/Scripts/Camera/`](file:///c:/__Works__/_MythOfGuotaji_/MythOfGuotaji/Assets/_MythOfGuotaji/Scripts/Camera)
* 전투 (활 & 단검): [`Assets/_MythOfGuotaji/Scripts/Combat/`](file:///c:/__Works__/_MythOfGuotaji_/MythOfGuotaji/Assets/_MythOfGuotaji/Scripts/Combat)
* 캐릭터 및 요괴: [`Assets/_MythOfGuotaji/Scripts/Actor/`](file:///c:/__Works__/_MythOfGuotaji_/MythOfGuotaji/Assets/_MythOfGuotaji/Scripts/Actor)
* 절차적 미로 던전: [`Assets/_MythOfGuotaji/Scripts/Dungeon/`](file:///c:/__Works__/_MythOfGuotaji_/MythOfGuotaji/Assets/_MythOfGuotaji/Scripts/Dungeon)
* 이중 트리 및 성장: [`Assets/_MythOfGuotaji/Scripts/Progression/`](file:///c:/__Works__/_MythOfGuotaji_/MythOfGuotaji/Assets/_MythOfGuotaji/Scripts/Progression)
* UI 및 디버그 패널: [`Assets/_MythOfGuotaji/Scripts/UI/`](file:///c:/__Works__/_MythOfGuotaji_/MythOfGuotaji/Assets/_MythOfGuotaji/Scripts/UI)
