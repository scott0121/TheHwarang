namespace MythOfGuotaji.Dungeon
{
    public enum RoomType
    {
        Start,      // 고정 시작방 (거타지 안전지대 스폰)
        Normal,     // 일반 미로방 (진입 후 연결된 몬스터 방 자동 개방)
        Trap,       // 함정방 (다수 요괴 매복 방)
        KeyRoom,    // 중간보스방 (1블록->2블록 유일 관문: 입구 1개, 잠긴 출구 1개 고정)
        Boss        // 최종보스방 (단일 입구, 문 앞 접근 시 자동 개방)
    }

    public enum Direction
    {
        North = 0,
        East = 1,
        South = 2,
        West = 3
    }

    public enum DoorState
    {
        Open,       // 통로 개방 (자동문 제어 상태)
        Wall,       // 벽으로 완전히 막힘 (미로의 외벽/연결되지 않은 면)
        CombatLock, // 전투 중 퇴로 차단 상태
        BossLock    // 중간보스방 출구 전용 잠긴 문 (중간보스 처치 열쇠 + 전투 종료 후 접근 필요)
    }

    /// <summary>
    /// [던전 문 정책 - 규칙 2]: 문의 종류는 자동으로 열리는 문(AutoDoor)과 잠긴 문(LockedDoor) 두 가지로 구분
    /// </summary>
    public enum DungeonDoorType
    {
        AutoDoor = 0,   // 자동으로 열리는 문 (일반 방, 시작방 첫 문, 중간보스 입구, 최종보스 입구)
        LockedDoor = 1  // 잠긴 문 (중간보스방 -> 후반부 던전 연결 출구 관문)
    }
}
