using UnityEngine;

namespace MythOfGuotaji.Combat
{
    public enum ElementType
    {
        Physical,   // 기본 물리
        Fire,       // 화염 (산해경 화마)
        Lightning,  // 뇌전 (신라 비술/신궁)
        Poison      // 독기 (요괴 독)
    }

    public struct DamageInfo
    {
        public float amount;
        public bool isCritical;
        public Vector3 hitPoint;
        public Vector3 hitDirection;
        public float knockbackForce;
        public ElementType element;
        public GameObject attacker;
        public int volleyId;
        public bool isCharged;
        public bool isReflected;

        public DamageInfo(float amount, bool isCritical, Vector3 hitPoint, Vector3 hitDirection, float knockbackForce = 0f, ElementType element = ElementType.Physical, GameObject attacker = null, int volleyId = 0, bool isCharged = false, bool isReflected = false)
        {
            this.amount = amount;
            this.isCritical = isCritical;
            this.hitPoint = hitPoint;
            this.hitDirection = hitDirection;
            this.knockbackForce = knockbackForce;
            this.element = element;
            this.attacker = attacker;
            this.volleyId = volleyId;
            this.isCharged = isCharged;
            this.isReflected = isReflected;
        }
    }

    public interface IDamageable
    {
        void TakeDamage(DamageInfo info);
        bool IsDead { get; }
        Transform Transform { get; }
    }
}
