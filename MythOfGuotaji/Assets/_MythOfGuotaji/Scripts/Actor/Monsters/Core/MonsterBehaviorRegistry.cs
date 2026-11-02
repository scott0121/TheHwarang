using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace MythOfGuotaji.Actor
{
    /// <summary>
    /// MonsterBehavior 파생 클래스를 특정 MonsterType 에 연결한다. (중복 등록 시 경고)
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class MonsterBehaviorAttribute : Attribute
    {
        public MonsterType Type { get; }
        public MonsterBehaviorAttribute(MonsterType type) { Type = type; }
    }

    /// <summary>
    /// [MonsterBehavior] 어트리뷰트를 가진 클래스를 리플렉션으로 1회 수집하여 MonsterType → Behavior 팩토리로 보관한다.
    /// 등록되지 않은 타입은 기본 근접 행동(MeleeMonsterBehavior)으로 대체된다.
    /// </summary>
    public static class MonsterBehaviorRegistry
    {
        private static Dictionary<MonsterType, Type> _map;

        private static void EnsureBuilt()
        {
            if (_map != null) return;

            _map = new Dictionary<MonsterType, Type>();
            foreach (var type in typeof(MonsterBehavior).Assembly.GetTypes())
            {
                if (type.IsAbstract || !typeof(MonsterBehavior).IsAssignableFrom(type)) continue;

                foreach (var attr in type.GetCustomAttributes<MonsterBehaviorAttribute>(false))
                {
                    if (_map.TryGetValue(attr.Type, out var existing))
                    {
                        Debug.LogWarning($"[MonsterBehaviorRegistry] {attr.Type} 중복 등록: {existing.Name} / {type.Name} (후자 무시)");
                        continue;
                    }
                    _map[attr.Type] = type;
                }
            }
        }

        public static bool IsRegistered(MonsterType type)
        {
            EnsureBuilt();
            return _map.ContainsKey(type);
        }

        /// <summary>해당 타입의 Behavior 인스턴스를 만들어 owner 에 연결</summary>
        public static MonsterBehavior Create(MonsterType type, MonsterAI owner)
        {
            EnsureBuilt();

            MonsterBehavior behavior;
            if (_map.TryGetValue(type, out var behaviorType))
            {
                behavior = (MonsterBehavior)Activator.CreateInstance(behaviorType);
            }
            else
            {
                Debug.LogWarning($"[MonsterBehaviorRegistry] {type} 에 등록된 Behavior 가 없어 기본 근접 행동을 사용합니다.");
                behavior = new MeleeMonsterBehavior();
            }

            behavior.Bind(owner, type);
            return behavior;
        }

        /// <summary>등록 현황 (디버그/테스트용)</summary>
        public static IReadOnlyDictionary<MonsterType, Type> GetRegistrations()
        {
            EnsureBuilt();
            return _map;
        }
    }
}
