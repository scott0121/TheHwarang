using UnityEngine;

namespace MythOfGuotaji.Dungeon
{
    [CreateAssetMenu(fileName = "ChapterTheme_01", menuName = "MythOfGuotaji/Dungeon/ChapterTheme")]
    public class ChapterThemeSO : ScriptableObject
    {
        [Header("Chapter Info")]
        public int chapterIndex = 1;
        public string chapterName = "제1장: 서해 비경과 요괴굴";

        [Header("Room Prefabs & Materials")]
        public GameObject baseRoomPrefab;
        public Material floorMaterial;
        public Material wallMaterial;
        public Color ambientColor = new Color(0.2f, 0.25f, 0.35f);
    }
}
