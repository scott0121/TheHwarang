using MythOfGuotaji.Actor;
using MythOfGuotaji.Cameras;
using MythOfGuotaji.Combat;
using MythOfGuotaji.Core;
using MythOfGuotaji.Data;
using MythOfGuotaji.Dungeon;
using MythOfGuotaji.Progression;
using MythOfGuotaji.UI;
using UnityEngine;

namespace MythOfGuotaji.Core
{
    /// <summary>
    /// [레거시 호환용] 
    /// 현재 프로젝트는 Outgame(Intro.scene) <-> Ingame(PlayMain.scene) 전환을 위해
    /// RuntimeInitializeOnLoadMethod 기반 자동 생성을 비활성화하고, PlayMainSceneController를 통한
    /// Scene 기반 프리팹 로딩 아키텍처로 전환되었습니다.
    /// </summary>
    public class DungeonBootstrapper : MonoBehaviour
    {
        [Header("Auto Setup on Awake (Disabled by Default - Use PlayMainSceneController)")]
        [SerializeField] private bool _autoSetupScene = false;

        private void Awake()
        {
            if (!_autoSetupScene) return;

            if (FindAnyObjectByType<PlayMainSceneController>() == null)
            {
                var ctrlGo = new GameObject("[PlayMainSceneController]");
                ctrlGo.AddComponent<PlayMainSceneController>();
            }
        }

        private void SetupCoreManagers()
        {
            if (FindAnyObjectByType<GameDataManager>() == null)
            {
                var go = new GameObject("[GameDataManager]");
                go.AddComponent<GameDataManager>();
            }

            if (FindAnyObjectByType<GameManager>() == null)
            {
                var go = new GameObject("[GameManager]");
                go.AddComponent<GameManager>();
            }

            if (FindAnyObjectByType<ObjectPoolManager>() == null)
            {
                var go = new GameObject("[ObjectPoolManager]");
                go.AddComponent<ObjectPoolManager>();
            }

            if (FindAnyObjectByType<HordeSpawner>() == null)
            {
                var go = new GameObject("[HordeSpawner]");
                go.AddComponent<HordeSpawner>();
            }

            if (FindAnyObjectByType<DungeonFogManager>() == null)
            {
                var go = new GameObject("[DungeonFogManager]");
                go.AddComponent<DungeonFogManager>();
            }
        }

        private PlayerController SetupPlayer()
        {
            var player = FindAnyObjectByType<PlayerController>();
            if (player != null) return player;

            GameObject playerPrefab = GameDataManager.Instance != null && GameDataManager.Instance.Prefabs != null
                ? GameDataManager.Instance.Prefabs.playerPrefab
                : null;

            if (playerPrefab == null)
            {
                playerPrefab = Resources.Load<GameObject>("Prefabs/Player_Guotaji");
            }

            GameObject playerObj;
            if (playerPrefab != null)
            {
                playerObj = Instantiate(playerPrefab, new Vector3(0f, 1f, 0f), Quaternion.identity);
                playerObj.name = "Player_Guotaji";
                player = playerObj.GetComponent<PlayerController>();
            }
            else
            {
                playerObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                playerObj.name = "Player_Guotaji";
                playerObj.tag = "Player";
                playerObj.transform.position = new Vector3(0f, 1f, 0f);

                var rend = playerObj.GetComponent<Renderer>();
                if (rend != null)
                {
                    rend.material.color = new Color(0.2f, 0.6f, 0.9f); // 푸른 도포/무사복
                }

                if (!playerObj.TryGetComponent<CharacterController>(out var cc))
                {
                    cc = playerObj.AddComponent<CharacterController>();
                }
                cc.height = 2f;
                cc.radius = 0.5f;

                // 활 및 단검 장착
                playerObj.AddComponent<BowWeapon>();
                playerObj.AddComponent<DaggerWeapon>();

                player = playerObj.AddComponent<PlayerController>();
            }
            return player;
        }

        private void SetupHelper()
        {
            if (FindAnyObjectByType<HelperManager>() == null)
            {
                var go = new GameObject("[HelperManager]");
                go.AddComponent<HelperManager>();
            }
            else
            {
                HelperManager.Instance?.EnsureHelperSpawned();
            }
        }

        private void SetupCamera(Transform playerTransform)
        {
            GameObject cameraPrefab = null;
            if (Data.GameDataManager.Instance != null && Data.GameDataManager.Instance.Prefabs != null)
            {
                cameraPrefab = Data.GameDataManager.Instance.Prefabs.mainCameraPrefab;
            }
            if (cameraPrefab == null)
            {
                cameraPrefab = Resources.Load<GameObject>("Prefabs/MainCamera");
            }

            QuarterViewCamera qCam = null;

            if (cameraPrefab != null)
            {
                Camera existingCam = Camera.main;
                // 기존 씬에 QuarterViewCamera가 없는 기본 메인 카메라가 존재하면 교체
                if (existingCam != null && existingCam.GetComponent<QuarterViewCamera>() == null)
                {
                    Destroy(existingCam.gameObject);
                    existingCam = null;
                }

                if (existingCam != null)
                {
                    qCam = existingCam.GetComponent<QuarterViewCamera>();
                    if (qCam == null) qCam = existingCam.gameObject.AddComponent<QuarterViewCamera>();
                    qCam.ApplyPrefabSettings(cameraPrefab.GetComponent<QuarterViewCamera>(), cameraPrefab.GetComponent<Camera>());
                }
                else
                {
                    var spawned = Instantiate(cameraPrefab);
                    spawned.name = "Main Camera";
                    spawned.tag = "MainCamera";
                    qCam = spawned.GetComponent<QuarterViewCamera>();
                }
            }
            else
            {
                Camera cam = Camera.main;
                if (cam == null)
                {
                    var camObj = new GameObject("Main Camera");
                    cam = camObj.AddComponent<Camera>();
                    camObj.tag = "MainCamera";
                }

                qCam = cam.GetComponent<QuarterViewCamera>();
                if (qCam == null)
                {
                    qCam = cam.gameObject.AddComponent<QuarterViewCamera>();
                }
            }

            if (qCam != null)
            {
                qCam.SetTarget(playerTransform);
            }
        }

        private void SetupDungeonGenerator()
        {
            if (FindAnyObjectByType<MazeGenerator>() == null)
            {
                var go = new GameObject("[MazeGenerator]");
                go.AddComponent<MazeGenerator>();
            }
        }

        private void SetupProgressionManagers()
        {
            if (FindAnyObjectByType<ScenarioTreeManager>() == null)
            {
                var go = new GameObject("[ScenarioTreeManager]");
                go.AddComponent<ScenarioTreeManager>();
            }

            if (FindAnyObjectByType<CharacterTreeManager>() == null)
            {
                var go = new GameObject("[CharacterTreeManager]");
                go.AddComponent<CharacterTreeManager>();
            }

            if (FindAnyObjectByType<InRunProgressionManager>() == null)
            {
                var go = new GameObject("[InRunProgressionManager]");
                go.AddComponent<InRunProgressionManager>();
            }
        }

        private void SetupUI()
        {
            // EventSystem (New Input System UGUI 상호작용 및 클릭 보장)
            EnsureEventSystem();

            // 최하단 3중 체력 프로그레스바 UGUI 인스턴스화
            MythOfGuotaji.UI.PlayerHealthBarUI.EnsureInstance();

            // UGUI 비술(Boon) 3지선다 선택 모달 인스턴스화
            MythOfGuotaji.UI.BoonSelectionUI.EnsureInstance();

            if (FindAnyObjectByType<ProgressionDebugUI>() == null)
            {
                var go = new GameObject("[ProgressionDebugUI]");
                go.AddComponent<ProgressionDebugUI>();
            }

            if (FindAnyObjectByType<MonsterHealthBarManager>() == null)
            {
                var go = new GameObject("[MonsterHealthBarManager]");
                go.AddComponent<MonsterHealthBarManager>();
            }

            if (FindAnyObjectByType<BossHealthBarUI>() == null)
            {
                var go = new GameObject("[BossHealthBarUI]");
                go.AddComponent<BossHealthBarUI>();
            }

            if (FindAnyObjectByType<StageClearUI>() == null)
            {
                var go = new GameObject("[StageClearUI]");
                go.AddComponent<StageClearUI>();
            }
        }

        private static void EnsureEventSystem()
        {
            var es = Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>();
            if (es == null)
            {
                var esGo = new GameObject("[EventSystem]");
                es = esGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esGo.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }
            else
            {
                var standalone = es.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                if (standalone != null)
                {
                    standalone.enabled = false;
                    Object.DestroyImmediate(standalone);
                }
                if (es.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>() == null)
                {
                    es.gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                }
            }
        }

        private void SetupLighting()
        {
            var light = FindAnyObjectByType<Light>();
            if (light == null)
            {
                var lightObj = new GameObject("Directional Light");
                var l = lightObj.AddComponent<Light>();
                l.type = LightType.Directional;
                l.intensity = 1.2f;
                lightObj.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            }
        }
    }
}
