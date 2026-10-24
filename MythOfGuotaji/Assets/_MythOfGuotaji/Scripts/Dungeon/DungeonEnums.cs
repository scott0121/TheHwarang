namespace MythOfGuotaji.Dungeon
{
    public enum RoomType
    {
        Start,      // 고정 시작방 (거타지 안전 스폰)
        Normal,     // 일반 미로방 (문 상시 개방, 자유 이동/추격)
        Trap,       // 함정방 (진입 시 문 잠김, 처치 후 개방)
        KeyRoom,    // 열쇠방 (진입 시 문 잠김, 정예 요괴 처치 후 보스 열쇠 획득)
        Boss        // 고정 보스방 (열쇠 획득 후 진입 가능, 여우 요괴)
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
        Open,       // 통로 개방 (자유 이동)
        Wall,       // 벽으로 완전히 막힘 (미로의 외벽/연결되지 않은 면)
        CombatLock, // 전투/함정/웨이브 중 닫힌 문
        BossLock    // 보스방 전용 잠긴 문 (열쇠 필요)
    }
}
