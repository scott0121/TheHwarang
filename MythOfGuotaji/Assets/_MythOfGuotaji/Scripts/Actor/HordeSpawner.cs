using System.Collections.Generic;
using MythOfGuotaji.Core;
using MythOfGuotaji.Data;
using UnityEngine;

namespace MythOfGuotaji.Actor
{
    /// <summary>
    /// 산해경 대규모 요괴 무리를 소환하는 스포너
    /// - 프리팹 데이터베이스 및 몬스터 밸런스 테이블 연동
    /// </summary>
    public class HordeSpawner : MonoBehaviour
    {
        private static HordeSpawner _instance;
        public static HordeSpawner Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<HordeSpawner>();
                }
                return _instance;
            }
            private set => _instance = value;
        }

        [Header("Prefabs (Core 6)")]
        [SerializeField] private GameObject _minionPrefab;
        [SerializeField] private GameObject _elitePrefab;
        [SerializeField] private GameObject _foxBossPrefab;
        [SerializeField] private GameObject _archerPrefab;
        [SerializeField] private GameObject _bigMonPrefab;
        [SerializeField] private GameObject _zhuPrefab;

        [Header("Prefabs (Gimmick 9)")]
        [SerializeField] private GameObject _dokkaebiFirePrefab;
        [SerializeField] private GameObject _boarChargerPrefab;
        [SerializeField] private GameObject _spiderTetherPrefab;
        [SerializeField] private GameObject _mudSlimePrefab;
        [SerializeField] private GameObject _phantomFoxPrefab;
        [SerializeField] private GameObject _ironShieldPrefab;
        [SerializeField] private GameObject _energyAbsorberPrefab;
        [SerializeField] private GameObject _bombSparrowPrefab;
        [SerializeField] private GameObject _totemTurretPrefab;

        private void Awake()
        {
            Instance = this;
            LoadPrefabsFromDatabase();
        }

        public void LoadPrefabsFromDatabase()
        {
            if (GameDataManager.Instance != null && GameDataManager.Instance.Prefabs != null)
            {
                var prefabs = GameDataManager.Instance.Prefabs;
                if (_minionPrefab == null) _minionPrefab = prefabs.minionMonsterPrefab;
                if (_elitePrefab == null) _elitePrefab = prefabs.eliteMonsterPrefab;
                if (_foxBossPrefab == null) _foxBossPrefab = prefabs.foxBossPrefab;
                if (_archerPrefab == null) _archerPrefab = prefabs.archerMonsterPrefab;
                if (_bigMonPrefab == null) _bigMonPrefab = prefabs.bigMonMonsterPrefab;
                if (_zhuPrefab == null) _zhuPrefab = prefabs.zhuMonsterPrefab;

                if (_dokkaebiFirePrefab == null) _dokkaebiFirePrefab = prefabs.dokkaebiFireMonsterPrefab;
                if (_boarChargerPrefab == null) _boarChargerPrefab = prefabs.boarChargerMonsterPrefab;
                if (_spiderTetherPrefab == null) _spiderTetherPrefab = prefabs.spiderTetherMonsterPrefab;
                if (_mudSlimePrefab == null) _mudSlimePrefab = prefabs.mudSlimeMonsterPrefab;
                if (_phantomFoxPrefab == null) _phantomFoxPrefab = prefabs.phantomFoxMonsterPrefab;
                if (_ironShieldPrefab == null) _ironShieldPrefab = prefabs.ironShieldMonsterPrefab;
                if (_energyAbsorberPrefab == null) _energyAbsorberPrefab = prefabs.energyAbsorberMonsterPrefab;
                if (_bombSparrowPrefab == null) _bombSparrowPrefab = prefabs.bombSparrowMonsterPrefab;
                if (_totemTurretPrefab == null) _totemTurretPrefab = prefabs.totemTurretMonsterPrefab;
            }

            // Resources/Prefabs fallback
            if (_minionPrefab == null) _minionPrefab = Resources.Load<GameObject>("Prefabs/Monster_Minion");
            if (_elitePrefab == null) _elitePrefab = Resources.Load<GameObject>("Prefabs/Monster_Elite");
            if (_foxBossPrefab == null) _foxBossPrefab = Resources.Load<GameObject>("Prefabs/Monster_FoxBoss");
            if (_archerPrefab == null) _archerPrefab = Resources.Load<GameObject>("Prefabs/Monster_Archer");
            if (_bigMonPrefab == null) _bigMonPrefab = Resources.Load<GameObject>("Prefabs/Monster_BigMon");
            if (_zhuPrefab == null) _zhuPrefab = Resources.Load<GameObject>("Prefabs/Monster_Zhu");

            if (_dokkaebiFirePrefab == null) _dokkaebiFirePrefab = Resources.Load<GameObject>("Prefabs/Monster_DokkaebiFire");
            if (_boarChargerPrefab == null) _boarChargerPrefab = Resources.Load<GameObject>("Prefabs/Monster_BoarCharger");
            if (_spiderTetherPrefab == null) _spiderTetherPrefab = Resources.Load<GameObject>("Prefabs/Monster_SpiderTether");
            if (_mudSlimePrefab == null) _mudSlimePrefab = Resources.Load<GameObject>("Prefabs/Monster_MudSlime");
            if (_phantomFoxPrefab == null) _phantomFoxPrefab = Resources.Load<GameObject>("Prefabs/Monster_PhantomFox");
            if (_ironShieldPrefab == null) _ironShieldPrefab = Resources.Load<GameObject>("Prefabs/Monster_IronShield");
            if (_energyAbsorberPrefab == null) _energyAbsorberPrefab = Resources.Load<GameObject>("Prefabs/Monster_EnergyAbsorber");
            if (_bombSparrowPrefab == null) _bombSparrowPrefab = Resources.Load<GameObject>("Prefabs/Monster_BombSparrow");
            if (_totemTurretPrefab == null) _totemTurretPrefab = Resources.Load<GameObject>("Prefabs/Monster_TotemTurret");
        }

        private static readonly MonsterType[] NormalSpawnPool = new MonsterType[]
        {
            MonsterType.Minion, MonsterType.Minion, // 기본 잡몹 가중치 2
            MonsterType.Minion_Archer,
            MonsterType.Minion_BigMon,
            MonsterType.Minion_Zhu,
            MonsterType.Dokkaebi_Fire,
            MonsterType.Boar_Charger,
            MonsterType.Spider_Tether,
            MonsterType.Mud_Slime,
            MonsterType.Phantom_Fox,
            MonsterType.Iron_Shield,
            MonsterType.Energy_Absorber,
            MonsterType.Bomb_Sparrow,
            MonsterType.Totem_Turret
        };

        public List<GameObject> SpawnHorde(Vector3 center, int count, float radius)
        {
            LoadPrefabsFromDatabase();
            List<GameObject> spawned = new();

            for (int i = 0; i < count; i++)
            {
                MonsterType targetType = NormalSpawnPool[Random.Range(0, NormalSpawnPool.Length)];

                var stat = GameDataManager.Instance != null && GameDataManager.Instance.MonsterStats != null
                    ? GameDataManager.Instance.MonsterStats.GetStat(targetType)
                    : null;

                float hp = stat != null ? stat.maxHp : (targetType == MonsterType.Minion_Archer ? 30f : (targetType == MonsterType.Minion_BigMon ? 60f : (targetType == MonsterType.Minion_Zhu ? 50f : 35f)));
                float speed = stat != null ? stat.moveSpeed : (targetType == MonsterType.Minion_Archer ? 2.7f : (targetType == MonsterType.Minion_BigMon ? 2.0f : (targetType == MonsterType.Minion_Zhu ? 1.2f : 4.2f)));
                float damage = stat != null ? stat.attackDamage : (targetType == MonsterType.Minion_Archer ? 7f : (targetType == MonsterType.Minion_BigMon ? 12f : (targetType == MonsterType.Minion_Zhu ? 18f : 6f)));
                Color tint = stat != null ? stat.tintColor : (targetType == MonsterType.Minion_Archer ? new Color(0.2f, 0.9f, 0.3f) : (targetType == MonsterType.Minion_BigMon ? new Color(0.25f, 0.6f, 0.5f) : (targetType == MonsterType.Minion_Zhu ? new Color(0.72f, 0.22f, 0.85f) : new Color(0.85f, 0.2f, 0.2f))));
                float scale = stat != null ? stat.scaleMultiplier : (targetType == MonsterType.Minion_Archer ? 0.9f : (targetType == MonsterType.Minion_BigMon ? 1.4f : (targetType == MonsterType.Minion_Zhu ? 1.15f : 1.0f)));
                float range = stat != null ? stat.attackRange : (targetType == MonsterType.Minion_Archer ? 15f : (targetType == MonsterType.Minion_BigMon ? 20.0f : (targetType == MonsterType.Minion_Zhu ? 20f : 5.0f)));
                float cd = stat != null ? stat.attackCooldown : (targetType == MonsterType.Minion_Archer ? 0.7f : (targetType == MonsterType.Minion_BigMon ? 3.5f : (targetType == MonsterType.Minion_Zhu ? 3.0f : 1.0f)));

                if (stat == null)
                {
                    if (targetType == MonsterType.Minion_Archer) { range = 15f; cd = 0.7f; }
                    else if (targetType == MonsterType.Minion_Zhu) { range = 20f; cd = 3.0f; }
                    else if (targetType == MonsterType.Minion_BigMon) { range = 20f; cd = 3.5f; }
                    else if (targetType == MonsterType.Minion) { range = 5.0f; cd = 1.0f; }
                }

                // 시간에 따른 몬스터 레벨업 스케일링 (5분부터 매 5분마다 1레벨, 레벨당 체력 +20%, 공격력 +10% 합연산)
                int monsterLevel = GameManager.Instance != null ? GameManager.Instance.CurrentMonsterLevel : 0;
                float hpMult = 1f + (monsterLevel * 0.20f);
                float dmgMult = 1f + (monsterLevel * 0.10f);
                float finalHp = hp * hpMult;
                float finalDamage = damage * dmgMult;

                // 스폰 시 몬스터들 간의 충돌 겹침 및 공중 부유 방지: 균등 원형 분할 및 안전 거리(2.5m~반경) 분산 배치
                float angleStep = 360f / Mathf.Max(1, count);
                float baseAngle = Random.Range(0f, 360f);
                float angle = (baseAngle + i * angleStep) * Mathf.Deg2Rad;
                float dist = Random.Range(2.5f, Mathf.Max(3.5f, radius));
                Vector3 spawnPos = new Vector3(center.x + Mathf.Cos(angle) * dist, 0.2f, center.z + Mathf.Sin(angle) * dist);

                GameObject minion = null;
                string poolKey = $"{targetType}_Pool";

                string mobName = targetType switch
                {
                    MonsterType.Minion_Archer => "맹독충 (Minion_Archer)",
                    MonsterType.Minion_BigMon => "거북이 (Minion_BigMon)",
                    MonsterType.Minion_Zhu => "주(鴸) (Minion_Zhu)",
                    MonsterType.Dokkaebi_Fire => "도깨비 화염술사 (Dokkaebi_Fire)",
                    MonsterType.Boar_Charger => "저돌맹진 멧돼지 (Boar_Charger)",
                    MonsterType.Spider_Tether => "요선 거미 (Spider_Tether)",
                    MonsterType.Mud_Slime => "진흙 이괴 (Mud_Slime)",
                    MonsterType.Phantom_Fox => "환영 여우귀 (Phantom_Fox)",
                    MonsterType.Iron_Shield => "흑철 방패병 (Iron_Shield)",
                    MonsterType.Energy_Absorber => "영력 흡수 괴물 (Energy_Absorber)",
                    MonsterType.Bomb_Sparrow => "자폭 참새 떼 (Bomb_Sparrow)",
                    MonsterType.Totem_Turret => "사방신 토템 (Totem_Turret)",
                    _ => "산해경 잡몹 (Minion)"
                };

                GameObject targetPrefab = targetType switch
                {
                    MonsterType.Minion_Archer => _archerPrefab != null ? _archerPrefab : _minionPrefab,
                    MonsterType.Minion_BigMon => _bigMonPrefab != null ? _bigMonPrefab : _minionPrefab,
                    MonsterType.Minion_Zhu => _zhuPrefab != null ? _zhuPrefab : _minionPrefab,
                    MonsterType.Dokkaebi_Fire => _dokkaebiFirePrefab != null ? _dokkaebiFirePrefab : (_archerPrefab != null ? _archerPrefab : _minionPrefab),
                    MonsterType.Boar_Charger => _boarChargerPrefab != null ? _boarChargerPrefab : (_bigMonPrefab != null ? _bigMonPrefab : _minionPrefab),
                    MonsterType.Spider_Tether => _spiderTetherPrefab != null ? _spiderTetherPrefab : _minionPrefab,
                    MonsterType.Mud_Slime => _mudSlimePrefab != null ? _mudSlimePrefab : (_zhuPrefab != null ? _zhuPrefab : _minionPrefab),
                    MonsterType.Phantom_Fox => _phantomFoxPrefab != null ? _phantomFoxPrefab : _minionPrefab,
                    MonsterType.Iron_Shield => _ironShieldPrefab != null ? _ironShieldPrefab : _minionPrefab,
                    MonsterType.Energy_Absorber => _energyAbsorberPrefab != null ? _energyAbsorberPrefab : _minionPrefab,
                    MonsterType.Bomb_Sparrow => _bombSparrowPrefab != null ? _bombSparrowPrefab : _minionPrefab,
                    MonsterType.Totem_Turret => _totemTurretPrefab != null ? _totemTurretPrefab : (_zhuPrefab != null ? _zhuPrefab : _minionPrefab),
                    _ => _minionPrefab
                };

                if (targetPrefab != null && ObjectPoolManager.Instance != null)
                {
                    minion = ObjectPoolManager.Instance.Spawn(targetPrefab, spawnPos, Quaternion.identity);
                }
                else if (targetPrefab != null)
                {
                    minion = Instantiate(targetPrefab, spawnPos, Quaternion.identity);
                }

                if (minion == null)
                {
                    minion = CreateFallbackMonster(mobName, spawnPos, tint);
                }

                var minionCc = minion.GetComponent<CharacterController>();
                if (minionCc != null) minionCc.enabled = false;

                if (Dungeon.MazeGenerator.Instance != null && Dungeon.MazeGenerator.Instance.DungeonRoot != null)
                {
                    minion.transform.SetParent(Dungeon.MazeGenerator.Instance.DungeonRoot);
                }

                minion.transform.position = spawnPos;
                minion.transform.rotation = Quaternion.identity;
                minion.transform.localScale = Vector3.one * scale;
                Physics.SyncTransforms();

                // 외형 틴트 컬러 적용 (모델 메쉬 및 머티리얼)
                var rends = minion.GetComponentsInChildren<Renderer>();
                foreach (var r in rends)
                {
                    if (r != null && r.material != null)
                    {
                        r.material.color = tint;
                        if (r.material.HasProperty("_BaseColor"))
                        {
                            r.material.SetColor("_BaseColor", tint);
                        }
                    }
                }

                if (minionCc != null) minionCc.enabled = true;

                if (minion.TryGetComponent<MonsterAI>(out var ai))
                {
                    ai.Setup(targetType, finalHp, speed, finalDamage, dropsKey: false, poolKey: poolKey, attackRange: range, attackCooldown: cd, level: monsterLevel);
                }

                spawned.Add(minion);
            }

            return spawned;
        }

        public GameObject SpawnEliteKeyKeeper(Vector3 position)
        {
            LoadPrefabsFromDatabase();
            if (_elitePrefab == null)
            {
                _elitePrefab = Resources.Load<GameObject>("Prefabs/Monster_Elite");
            }

            position.y = 1.0f;
            GameObject elite = null;

            var stat = GameDataManager.Instance != null && GameDataManager.Instance.MonsterStats != null
                ? GameDataManager.Instance.MonsterStats.GetStat(MonsterType.Elite)
                : null;

            float baseHp = stat != null ? stat.maxHp : 120f;
            float speed = stat != null ? stat.moveSpeed : 3.5f;
            float baseDamage = stat != null ? stat.attackDamage : 15f;
            Color tint = stat != null ? stat.tintColor : new Color(0.95f, 0.75f, 0.1f);
            float scale = stat != null ? stat.scaleMultiplier : 1.5f;
            string poolKey = _elitePrefab != null ? _elitePrefab.name : "DefaultElite";

            // 시간에 따른 레벨업 스케일링 (체력 +20%, 공격력 +10% 합연산)
            int monsterLevel = GameManager.Instance != null ? GameManager.Instance.CurrentMonsterLevel : 0;
            float hpMult = 1f + (monsterLevel * 0.20f);
            float dmgMult = 1f + (monsterLevel * 0.10f);
            float finalHp = baseHp * hpMult;
            float finalDamage = baseDamage * dmgMult;

            if (_elitePrefab != null && ObjectPoolManager.Instance != null)
            {
                elite = ObjectPoolManager.Instance.Spawn(_elitePrefab, position, Quaternion.identity);
            }
            else if (_elitePrefab != null)
            {
                elite = Instantiate(_elitePrefab, position, Quaternion.identity);
            }

            if (elite == null)
            {
                elite = CreateFallbackMonster("Elite_KeyKeeper", position, tint);
            }

            var eliteCc = elite.GetComponent<CharacterController>();
            if (eliteCc != null) eliteCc.enabled = false;

            if (Dungeon.MazeGenerator.Instance != null && Dungeon.MazeGenerator.Instance.DungeonRoot != null)
            {
                elite.transform.SetParent(Dungeon.MazeGenerator.Instance.DungeonRoot);
            }

            elite.transform.position = position;
            elite.transform.rotation = Quaternion.identity;
            elite.transform.localScale = Vector3.one * scale;
            Physics.SyncTransforms();

            if (eliteCc != null) eliteCc.enabled = true;

            if (elite.TryGetComponent<MonsterAI>(out var ai))
            {
                ai.Setup(MonsterType.Elite, finalHp, speed, finalDamage, dropsKey: true, poolKey: poolKey, level: monsterLevel);
            }

            return elite;
        }

        public GameObject SpawnFoxBoss(Vector3 position)
        {
            LoadPrefabsFromDatabase();
            if (_foxBossPrefab == null)
            {
                _foxBossPrefab = Resources.Load<GameObject>("Prefabs/Monster_FoxBoss");
            }

            position.y = 1.0f;
            GameObject boss = null;

            var stat = GameDataManager.Instance != null && GameDataManager.Instance.MonsterStats != null
                ? GameDataManager.Instance.MonsterStats.GetStat(MonsterType.FoxBoss)
                : null;

            float baseHp = stat != null ? stat.maxHp : 350f;
            float speed = stat != null ? stat.moveSpeed : 4.8f;
            float baseDamage = stat != null ? stat.attackDamage : 20f;
            Color tint = stat != null ? stat.tintColor : new Color(0.9f, 0.4f, 0.1f);
            float scale = stat != null ? stat.scaleMultiplier : 2.2f;
            string poolKey = _foxBossPrefab != null ? _foxBossPrefab.name : "DefaultFoxBoss";

            // 시간에 따른 레벨업 스케일링 (체력 +20%, 공격력 +10% 합연산)
            int monsterLevel = GameManager.Instance != null ? GameManager.Instance.CurrentMonsterLevel : 0;
            float hpMult = 1f + (monsterLevel * 0.20f);
            float dmgMult = 1f + (monsterLevel * 0.10f);
            float finalHp = baseHp * hpMult;
            float finalDamage = baseDamage * dmgMult;

            if (_foxBossPrefab != null && ObjectPoolManager.Instance != null)
            {
                boss = ObjectPoolManager.Instance.Spawn(_foxBossPrefab, position, Quaternion.identity);
            }
            else if (_foxBossPrefab != null)
            {
                boss = Instantiate(_foxBossPrefab, position, Quaternion.identity);
            }

            if (boss == null)
            {
                boss = CreateFallbackMonster("FoxBoss_Gumiho", position, tint);
            }

            var bossCc = boss.GetComponent<CharacterController>();
            if (bossCc != null) bossCc.enabled = false;

            if (Dungeon.MazeGenerator.Instance != null && Dungeon.MazeGenerator.Instance.DungeonRoot != null)
            {
                boss.transform.SetParent(Dungeon.MazeGenerator.Instance.DungeonRoot);
            }

            boss.transform.position = position;
            boss.transform.rotation = Quaternion.identity;
            boss.transform.localScale = Vector3.one * scale;
            Physics.SyncTransforms();

            if (bossCc != null) bossCc.enabled = true;

            if (boss.TryGetComponent<MonsterAI>(out var ai))
            {
                ai.Setup(MonsterType.FoxBoss, finalHp, speed, finalDamage, dropsKey: false, poolKey: poolKey, level: monsterLevel);
            }

            return boss;
        }

        private GameObject CreateFallbackMonster(string name, Vector3 pos, Color color)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = name;
            go.tag = "Monster";
            go.transform.position = pos;

            var renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = color;
                if (renderer.material.HasProperty("_BaseColor"))
                {
                    renderer.material.SetColor("_BaseColor", color);
                }
            }

            if (!go.TryGetComponent<CharacterController>(out var cc))
            {
                cc = go.AddComponent<CharacterController>();
            }
            cc.height = 2f;
            cc.radius = 0.5f;

            if (!go.TryGetComponent<MonsterAI>(out _))
            {
                go.AddComponent<MonsterAI>();
            }

            return go;
        }
    }
}
