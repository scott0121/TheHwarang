using UnityEngine;

namespace MythOfGuotaji.Dungeon
{
    /// <summary>
    /// 던전 방의 지오메트리(바닥, 벽체, 소켓 기둥 등) 및 문 소켓의 물리적 구축 엔진
    /// </summary>
    public static class DungeonRoomBuilder
    {
        public static GameObject BuildStandardRoomGeometry(
            MazeCell cell,
            Vector3 worldPos,
            Transform parent,
            float roomSize,
            float wallHeight,
            float doorWidth,
            ChapterThemeSO theme,
            Data.MapSkinTableSO mapSkinTable)
        {
            GameObject room = new GameObject($"Room_{cell.gridPos.x}_{cell.gridPos.y}_{cell.roomType}");
            room.transform.SetPositionAndRotation(worldPos, Quaternion.identity);
            if (parent != null) room.transform.SetParent(parent);

            var ctrl = room.AddComponent<RoomController>();

            // 1. 바닥 (Floor)
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Floor";
            floor.transform.SetParent(room.transform, false);
            floor.transform.localPosition = Vector3.zero;
            floor.transform.localRotation = Quaternion.identity;
            floor.transform.localScale = new Vector3(roomSize / 10f, 1f, roomSize / 10f);

            var floorRend = floor.GetComponent<Renderer>();
            if (floorRend != null)
            {
                Color floorCol = cell.roomType switch
                {
                    RoomType.Start => new Color(0.18f, 0.35f, 0.22f), // 시작방(안전 녹색)
                    RoomType.Boss => new Color(0.45f, 0.15f, 0.15f),  // 보스방(위험 암적색)
                    RoomType.KeyRoom => new Color(0.45f, 0.38f, 0.12f),// 열쇠방(황금빛)
                    RoomType.Trap => new Color(0.32f, 0.18f, 0.38f),  // 함정방(자주색)
                    _ => new Color(0.22f, 0.24f, 0.28f)              // 일반 미로방(석재 회색)
                };
                floorRend.material.color = floorCol;
                if (floorRend.material.HasProperty("_BaseColor")) floorRend.material.SetColor("_BaseColor", floorCol);
            }

            // 2. 방 트리거 콜라이더
            var boxCol = room.AddComponent<BoxCollider>();
            boxCol.isTrigger = true;
            boxCol.center = new Vector3(0f, 2f, 0f);
            boxCol.size = new Vector3(roomSize * 0.9f, 4f, roomSize * 0.9f);

            // 3. 4방향 문 소켓 빌드 (North: 0°, East: 90°, South: 180°, West: 270°)
            BuildDoorSocket(room, ctrl, Direction.North, new Vector3(0f, 0f, roomSize * 0.5f), Quaternion.identity, roomSize, wallHeight, doorWidth);
            BuildDoorSocket(room, ctrl, Direction.East, new Vector3(roomSize * 0.5f, 0f, 0f), Quaternion.Euler(0f, 90f, 0f), roomSize, wallHeight, doorWidth);
            BuildDoorSocket(room, ctrl, Direction.South, new Vector3(0f, 0f, -roomSize * 0.5f), Quaternion.Euler(0f, 180f, 0f), roomSize, wallHeight, doorWidth);
            BuildDoorSocket(room, ctrl, Direction.West, new Vector3(-roomSize * 0.5f, 0f, 0f), Quaternion.Euler(0f, 270f, 0f), roomSize, wallHeight, doorWidth);

            return room;
        }

        public static void BuildDoorSocket(
            GameObject room,
            RoomController ctrl,
            Direction dir,
            Vector3 localPos,
            Quaternion localRot,
            float roomSize,
            float wallHeight,
            float doorWidth)
        {
            GameObject socketObj = new GameObject($"Socket_{dir}");
            socketObj.transform.SetParent(room.transform, false);
            socketObj.transform.localPosition = localPos;
            socketObj.transform.localRotation = localRot;

            var socket = socketObj.AddComponent<DoorSocket>();

            float sideWallLen = (roomSize - doorWidth) * 0.5f;
            float sideOffset = (doorWidth + sideWallLen) * 0.5f;

            Color wallColor = new Color(0.28f, 0.3f, 0.35f); // 어두운 청회색 석벽

            // 1. 좌측 코너 고정 벽체 (항상 존재)
            GameObject leftWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leftWall.name = "SideWall_Left";
            leftWall.transform.SetParent(socketObj.transform, false);
            leftWall.transform.localPosition = new Vector3(-sideOffset, wallHeight * 0.5f, 0f);
            leftWall.transform.localRotation = Quaternion.identity;
            leftWall.transform.localScale = new Vector3(sideWallLen, wallHeight, 0.9f);
            SetObjColor(leftWall, wallColor);

            // 2. 우측 코너 고정 벽체 (항상 존재)
            GameObject rightWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rightWall.name = "SideWall_Right";
            rightWall.transform.SetParent(socketObj.transform, false);
            rightWall.transform.localPosition = new Vector3(sideOffset, wallHeight * 0.5f, 0f);
            rightWall.transform.localRotation = Quaternion.identity;
            rightWall.transform.localScale = new Vector3(sideWallLen, wallHeight, 0.9f);
            SetObjColor(rightWall, wallColor);

            // 3. 중앙 통로 폐쇄 벽체 (DoorState.Wall 일 때만 활성화)
            GameObject centerWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            centerWall.name = "SolidCenterWall";
            centerWall.transform.SetParent(socketObj.transform, false);
            centerWall.transform.localPosition = new Vector3(0f, wallHeight * 0.5f, 0f);
            centerWall.transform.localRotation = Quaternion.identity;
            centerWall.transform.localScale = new Vector3(doorWidth + 0.1f, wallHeight, 0.9f);
            SetObjColor(centerWall, wallColor);

            // 4. 중앙 잠금 봉인 배리어 (CombatLock/BossLock 시 차단)
            GameObject barrier = GameObject.CreatePrimitive(PrimitiveType.Cube);
            barrier.name = "BarrierVisual";
            barrier.transform.SetParent(socketObj.transform, false);
            barrier.transform.localPosition = new Vector3(0f, wallHeight * 0.5f, 0f);
            barrier.transform.localRotation = Quaternion.identity;
            barrier.transform.localScale = new Vector3(doorWidth + 0.2f, wallHeight * 0.95f, 0.6f);

            // 5. 문 기둥 (Door Pillars)
            GameObject pillars = new GameObject("DoorPillars");
            pillars.transform.SetParent(socketObj.transform, false);
            pillars.transform.localPosition = Vector3.zero;
            pillars.transform.localRotation = Quaternion.identity;

            GameObject pLeft = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pLeft.name = "Pillar_Left";
            pLeft.transform.SetParent(pillars.transform, false);
            pLeft.transform.localPosition = new Vector3(-doorWidth * 0.5f, wallHeight * 0.5f, 0f);
            pLeft.transform.localRotation = Quaternion.identity;
            pLeft.transform.localScale = new Vector3(0.7f, wallHeight * 1.05f, 1.2f);
            SetObjColor(pLeft, new Color(0.18f, 0.2f, 0.24f));

            GameObject pRight = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pRight.name = "Pillar_Right";
            pRight.transform.SetParent(pillars.transform, false);
            pRight.transform.localPosition = new Vector3(doorWidth * 0.5f, wallHeight * 0.5f, 0f);
            pRight.transform.localRotation = Quaternion.identity;
            pRight.transform.localScale = new Vector3(0.7f, wallHeight * 1.05f, 1.2f);
            SetObjColor(pRight, new Color(0.18f, 0.2f, 0.24f));

            socket.InitializeComponents(dir, centerWall, barrier, pillars);
            SetRoomSocketPrivateField(ctrl, dir, socket);
        }

        public static void SetObjColor(GameObject go, Color color)
        {
            var rend = go.GetComponent<Renderer>();
            if (rend != null)
            {
                rend.material.color = color;
                if (rend.material.HasProperty("_BaseColor"))
                {
                    rend.material.SetColor("_BaseColor", color);
                }
            }
        }

        public static void SetRoomSocketPrivateField(RoomController ctrl, Direction dir, DoorSocket socket)
        {
            string fieldName = dir switch
            {
                Direction.North => "_socketNorth",
                Direction.East => "_socketEast",
                Direction.South => "_socketSouth",
                Direction.West => "_socketWest",
                _ => ""
            };
            var field = typeof(RoomController).GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(ctrl, socket);
        }
    }
}

