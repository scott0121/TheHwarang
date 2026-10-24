using UnityEngine;

namespace MythOfGuotaji.Combat
{
    /// <summary>
    /// 단검으로 쳐내어 반사(패링) 가능한 투사체, 마법 구체, 투척체 인터페이스
    /// </summary>
    public interface IReflectable
    {
        bool IsFromPlayer { get; }
        Vector3 Position { get; }
        void Reflect(GameObject newAttacker, Vector3? customDirection = null);
    }
}
