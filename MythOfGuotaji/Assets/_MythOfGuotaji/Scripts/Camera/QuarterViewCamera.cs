using UnityEngine;
using UnityEngine.InputSystem;

namespace MythOfGuotaji.Cameras
{
    /// <summary>
    /// 슈팅 액션 RPG에 최적화된 넓은 시야의 쿼터뷰 카메라
    /// - 타겟 부드러운 추적
    /// - 마우스 조준 방향에 따른 가변 Lookahead 오프셋 (원거리 시야 확보)
    /// - 카메라 셰이크(흔들림) 효과
    /// </summary>
    public class QuarterViewCamera : MonoBehaviour
    {
        public static QuarterViewCamera Instance { get; private set; }

        [Header("Target & Positioning")]
        [SerializeField] private Transform _target;
        [SerializeField] private Vector3 _offset = new Vector3(0f, 18f, -14f);
        [SerializeField] private float _smoothTime = 0.15f;

        [Header("Lookahead Settings")]
        [SerializeField] private bool _enableLookahead = true;
        [SerializeField] private float _maxLookaheadDistance = 4.5f;
        [SerializeField] private float _lookaheadSmooth = 5f;

        [Header("Camera Shake")]
        private float _shakeTimer = 0f;
        private float _shakeIntensity = 0f;

        private Vector3 _currentVelocity;
        private Vector3 _currentLookaheadOffset;
        private Camera _cam;

        private void Awake()
        {
            Instance = this;
            _cam = GetComponent<Camera>();
            if (_cam == null) _cam = Camera.main;

            // Main Camera 프리팹이 존재할 경우 프리팹 수치를 최우선으로 동기화
            LoadAndApplyPrefabSettings();
        }

        /// <summary>
        /// MainCamera 프리팹 수치를 최우선(Highest Priority)으로 읽어와 동기화
        /// </summary>
        public void LoadAndApplyPrefabSettings()
        {
            GameObject prefab = null;
            if (Data.GameDataManager.Instance != null && Data.GameDataManager.Instance.Prefabs != null)
            {
                prefab = Data.GameDataManager.Instance.Prefabs.mainCameraPrefab;
            }
            if (prefab == null)
            {
                prefab = Resources.Load<GameObject>("Prefabs/MainCamera");
            }

            if (prefab != null && prefab != gameObject)
            {
                var prefabQCam = prefab.GetComponent<QuarterViewCamera>();
                var prefabCam = prefab.GetComponent<Camera>();
                ApplyPrefabSettings(prefabQCam, prefabCam);
            }
            else if (transform.rotation == Quaternion.identity)
            {
                // 기본 쿼터뷰 각도 설정 (약 52도 하향)
                transform.rotation = Quaternion.Euler(52f, 0f, 0f);
            }
        }

        public void ApplyPrefabSettings(QuarterViewCamera prefabQCam, Camera prefabCam = null)
        {
            if (prefabQCam != null)
            {
                _offset = prefabQCam._offset;
                _smoothTime = prefabQCam._smoothTime;
                _enableLookahead = prefabQCam._enableLookahead;
                _maxLookaheadDistance = prefabQCam._maxLookaheadDistance;
                _lookaheadSmooth = prefabQCam._lookaheadSmooth;
                transform.rotation = prefabQCam.transform.rotation;
            }

            if (prefabCam != null && _cam != null)
            {
                _cam.fieldOfView = prefabCam.fieldOfView;
                _cam.nearClipPlane = prefabCam.nearClipPlane;
                _cam.farClipPlane = prefabCam.farClipPlane;
                _cam.clearFlags = prefabCam.clearFlags;
                _cam.backgroundColor = prefabCam.backgroundColor;
                _cam.orthographic = prefabCam.orthographic;
                _cam.orthographicSize = prefabCam.orthographicSize;
            }
        }

        private void LateUpdate()
        {
            if (_target == null) return;

            // 1. 마우스 조준 방향에 따른 Lookahead 오프셋 계산
            Vector3 targetLookahead = Vector3.zero;
            if (_enableLookahead && _cam != null && Mouse.current != null)
            {
                Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
                Ray ray = _cam.ScreenPointToRay(mouseScreenPos);
                Plane groundPlane = new Plane(Vector3.up, _target.position);

                if (groundPlane.Raycast(ray, out float enter))
                {
                    Vector3 mouseWorldPos = ray.GetPoint(enter);
                    Vector3 dirToMouse = mouseWorldPos - _target.position;
                    dirToMouse.y = 0f;

                    float dist = Mathf.Clamp(dirToMouse.magnitude * 0.35f, 0f, _maxLookaheadDistance);
                    targetLookahead = dirToMouse.normalized * dist;
                }
            }

            _currentLookaheadOffset = Vector3.Lerp(_currentLookaheadOffset, targetLookahead, Time.deltaTime * _lookaheadSmooth);

            // 2. 기본 타겟 위치 + 오프셋 + Lookahead
            Vector3 desiredPosition = _target.position + _offset + _currentLookaheadOffset;

            // 3. 카메라 흔들림(Shake) 적용
            if (_shakeTimer > 0f)
            {
                _shakeTimer -= Time.deltaTime;
                Vector3 shakeOffset = Random.insideUnitSphere * _shakeIntensity;
                desiredPosition += shakeOffset;
            }

            // 4. 부드러운 카메라 이동
            transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref _currentVelocity, _smoothTime);
        }

        public void SetTarget(Transform newTarget)
        {
            _target = newTarget;
        }

        public void TriggerShake(float intensity, float duration)
        {
            _shakeIntensity = intensity;
            _shakeTimer = duration;
        }
    }
}
