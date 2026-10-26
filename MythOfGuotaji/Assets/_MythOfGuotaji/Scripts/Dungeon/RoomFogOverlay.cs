using UnityEngine;

namespace MythOfGuotaji.Dungeon
{
    public enum FogVisibilityState
    {
        Visible,    // 투명 (현재 캐릭터 시야 내)
        Explored,   // 반 어둠 (지나온 방이나 시야 밖, 어둠 50%)
        Unexplored  // 90% 어둠 (미방문 및 시야 밖, 어둠 90%)
    }

    /// <summary>
    /// 개별 방 상공(Y=4.65m)을 덮어 던전 전용 URP 포그 셰이더(MythOfGuotaji/DungeonFog)를 렌더링하는 포그 오버레이
    /// - 0m ~ 30m: 100% 투명
    /// - 30m ~ 45m: 선형 점진적 불투명 페이드
    /// - 45m 이상: 어둠
    /// - 문 개방 통로 기준 15도 시야각 페이드 및 차단 연동
    /// - 투명 / 반 어둠(50%) / 90% 어둠 3단계 지원
    /// </summary>
    public class RoomFogOverlay : MonoBehaviour
    {
        [Header("Fog State")]
        [SerializeField] private FogVisibilityState _state = FogVisibilityState.Unexplored;
        [SerializeField] private float _fadeSpeed = 3.2f;

        private GameObject _shroudObj;
        private Renderer _shroudRend;
        private Material _shroudMat;
        private bool _isCustomShader = false;

        private float _currentAlpha = 0.90f;
        private float _targetAlpha = 0.90f;

        public FogVisibilityState CurrentState => _state;
        public float CurrentAlpha => _currentAlpha;
        public Material ShroudMaterial => _shroudMat;

        public void InitializeOverlay(float roomSize)
        {
            if (_shroudObj != null) return;

            var existingShroud = transform.Find("FogShroud_Overlay");
            if (existingShroud != null)
            {
                _shroudObj = existingShroud.gameObject;
            }
            else
            {
                GameObject shroudPrefab = null;
                if (Data.GameDataManager.Instance != null && Data.GameDataManager.Instance.Prefabs != null)
                {
                    shroudPrefab = Data.PrefabDatabaseSO.ResolvePrefab(ref Data.GameDataManager.Instance.Prefabs.fogShroudPrefab, "Prefabs/FogShroud_Overlay");
                }
                else
                {
                    shroudPrefab = Resources.Load<GameObject>("Prefabs/FogShroud_Overlay");
                }

                if (shroudPrefab != null)
                {
                    _shroudObj = Instantiate(shroudPrefab, transform, false);
                    _shroudObj.name = "FogShroud_Overlay";
                }
                else
                {
                    _shroudObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    _shroudObj.name = "FogShroud_Overlay";
                    _shroudObj.transform.SetParent(transform, false);
                    var col = _shroudObj.GetComponent<Collider>();
                    if (col != null) Destroy(col);
                }
            }

            // 방 크기(39m)를 살짝 덮는 쿼드 배치 (Y=4.65m)
            _shroudObj.transform.localPosition = new Vector3(0f, 4.65f, 0f);
            _shroudObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            _shroudObj.transform.localScale = new Vector3(roomSize * 1.02f, roomSize * 1.02f, 1f);

            _shroudRend = _shroudObj.GetComponent<Renderer>();
            _shroudMat = CreateFogMaterial();
            _shroudRend.material = _shroudMat;

            // 기본 상태: 90% 어둠
            SetState(FogVisibilityState.Unexplored, immediate: true);
        }

        private Material CreateFogMaterial()
        {
            Shader shader = Shader.Find("MythOfGuotaji/DungeonFog") 
                         ?? Shader.Find("Universal Render Pipeline/Unlit") 
                         ?? Shader.Find("Unlit/Transparent") 
                         ?? Shader.Find("Sprites/Default");

            _isCustomShader = (shader != null && shader.name == "MythOfGuotaji/DungeonFog");

            var mat = new Material(shader);
            Color fogColor = new Color(0.015f, 0.015f, 0.025f, 0.90f);

            if (_isCustomShader)
            {
                mat.SetColor("_FogColor", fogColor);
                mat.SetFloat("_MaxDarkness", 0.90f);
                mat.SetFloat("_ClearDistance", 30.0f);
                mat.SetFloat("_FalloffDistance", 45.0f);
                mat.SetFloat("_DoorFadeAngle", 15.0f);
            }
            else
            {
                mat.SetFloat("_Surface", 1); // 1 = Transparent
                mat.SetFloat("_Blend", 0);   // 0 = Alpha Blend
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent + 150;
                mat.color = fogColor;
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", fogColor);
            }

            return mat;
        }

        public void SetState(FogVisibilityState newState, float overrideMaxDarkness = -1f, bool immediate = false)
        {
            _state = newState;

            if (_isCustomShader)
            {
                // 커스텀 셰이더의 경우 픽셀 셰이더가 0~30m는 무조건 100% 투명(0.0) 처리하므로,
                // _MaxDarkness는 Visible/Unexplored 시 기본 0.90, Explored 시 0.50으로 설정됩니다.
                if (overrideMaxDarkness >= 0f)
                {
                    _targetAlpha = overrideMaxDarkness;
                }
                else
                {
                    _targetAlpha = newState switch
                    {
                        FogVisibilityState.Visible => 0.90f,
                        FogVisibilityState.Explored => 0.50f,
                        FogVisibilityState.Unexplored => 0.90f,
                        _ => 0.90f
                    };
                }
            }
            else
            {
                // 폴백 셰이더용 알파
                _targetAlpha = newState switch
                {
                    FogVisibilityState.Visible => 0.0f,
                    FogVisibilityState.Explored => overrideMaxDarkness >= 0f ? overrideMaxDarkness : 0.50f,
                    FogVisibilityState.Unexplored => overrideMaxDarkness >= 0f ? overrideMaxDarkness : 0.90f,
                    _ => 0.90f
                };
            }

            if (immediate)
            {
                _currentAlpha = _targetAlpha;
                ApplyAlpha(_currentAlpha);
            }
        }

        private void Update()
        {
            if (Mathf.Abs(_currentAlpha - _targetAlpha) > 0.005f)
            {
                _currentAlpha = Mathf.MoveTowards(_currentAlpha, _targetAlpha, Time.deltaTime * _fadeSpeed);
                ApplyAlpha(_currentAlpha);
            }
        }

        private void ApplyAlpha(float alpha)
        {
            if (_shroudRend == null || _shroudMat == null) return;

            if (_isCustomShader)
            {
                _shroudRend.enabled = true;
                _shroudMat.SetFloat("_MaxDarkness", alpha);
            }
            else
            {
                if (alpha <= 0.01f)
                {
                    _shroudRend.enabled = false;
                }
                else
                {
                    _shroudRend.enabled = true;
                    Color col = new Color(0.015f, 0.015f, 0.025f, alpha);
                    _shroudMat.color = col;
                    if (_shroudMat.HasProperty("_BaseColor"))
                    {
                        _shroudMat.SetColor("_BaseColor", col);
                    }
                }
            }
        }

        private void OnDestroy()
        {
            if (_shroudMat != null) Destroy(_shroudMat);
        }
    }
}
