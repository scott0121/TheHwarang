using UnityEngine;
using UnityEngine.InputSystem;

namespace MythOfGuotaji.UI
{
    /// <summary>
    /// 게임 내 마우스 에임 포인터 및 화면 영역 제한(Confined) 관리자:
    /// 1. 64x64 크기의 선명한 고대비 에임(크로스헤어) 커서 생성 및 지정
    /// 2. 마우스 포인터가 게임 창 밖으로 나가지 않도록 CursorLockMode.Confined 영역 제한
    /// 3. 인게임 UI(Boon 선택 등) 활성화/비활성화 시 커서 상태 연동
    /// </summary>
    public class AimCursorManager : MonoBehaviour
    {
        private static AimCursorManager _instance;
        public static AimCursorManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<AimCursorManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("[AimCursorManager]");
                        _instance = go.AddComponent<AimCursorManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        [Header("Cursor Settings")]
        [SerializeField] private int _cursorSize = 64;
        [SerializeField] private bool _confineCursor = true;
        [SerializeField] private Color _aimColor = new Color(0.2f, 1.0f, 0.95f, 1.0f); // 선명한 형광 청록
        [SerializeField] private Color _outlineColor = new Color(0.05f, 0.05f, 0.05f, 0.95f); // 짙은 테두리(그림자)
        
        [SerializeField] private Texture2D _aimCursorTexture;

        private Vector2 _hotspot;
        private bool _isAimCursorActive = true;

        // Scene-based 아키텍처 전환에 따라 RuntimeInitializeOnLoadMethod를 제거하고,
        // PlayMain 씬 내의 AimCursorManager 프리팹 오브젝트를 통해 구동합니다.
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;

            GenerateAimCursorTexture();
            ApplyAimCursor();
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
                Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
                Cursor.lockState = CursorLockMode.None;
            }
        }

        private void Start()
        {
            ApplyAimCursor();
        }

        private void Update()
        {
            // 마우스 커서 창 영역 제한 상태 유지
            if (_isAimCursorActive && _confineCursor)
            {
                if (Cursor.lockState != CursorLockMode.Confined)
                {
                    Cursor.lockState = CursorLockMode.Confined;
                }
            }

            // 개발 및 편의용: 좌측 Alt 키 유지 시 마우스 임시 잠금 해제 (창 전환용)
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.leftAltKey.wasPressedThisFrame)
            {
                Cursor.lockState = CursorLockMode.None;
            }
            else if (keyboard != null && keyboard.leftAltKey.wasReleasedThisFrame)
            {
                if (_isAimCursorActive && _confineCursor)
                {
                    Cursor.lockState = CursorLockMode.Confined;
                }
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus && _isAimCursorActive && _confineCursor)
            {
                Cursor.lockState = CursorLockMode.Confined;
                Cursor.visible = true;
            }
        }

        /// <summary>
        /// 64x64 크기의 선명한 크로스헤어 텍스처 프로그래밍 생성
        /// (외곽 원형 링 + 4방향 십자선 + 중앙 조준점 + 짙은 외곽 테두리)
        /// </summary>
        private void GenerateAimCursorTexture()
        {
            int size = Mathf.Max(32, _cursorSize);
            if (_aimCursorTexture == null)
            {
                _aimCursorTexture = new Texture2D(size, size, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp
                };

                Color transparent = new Color(0, 0, 0, 0);
                Color[] pixels = new Color[size * size];
                for (int i = 0; i < pixels.Length; i++) pixels[i] = transparent;

                float center = (size - 1) * 0.5f;
                _hotspot = new Vector2(center, center);

                float ringRadius = size * 0.32f;
                float ringThickness = 1.6f;
                float dotRadius = 2.2f;
                int crossStart = Mathf.RoundToInt(size * 0.16f);
                int crossEnd = Mathf.RoundToInt(size * 0.44f);

                // 1단계: 테두리(그림자/외곽선) 그리기
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float dx = x - center;
                        float dy = y - center;
                        float dist = Mathf.Sqrt(dx * dx + dy * dy);

                        // 중앙 점 테두리
                        if (dist <= dotRadius + 1.2f)
                        {
                            pixels[y * size + x] = _outlineColor;
                        }

                        // 외곽 링 테두리
                        if (Mathf.Abs(dist - ringRadius) <= (ringThickness + 1.1f))
                        {
                            pixels[y * size + x] = _outlineColor;
                        }

                        // 4방향 십자선 테두리 (가로/세로 두께 4px)
                        if (Mathf.Abs(dx) <= 2.2f && ((dy >= crossStart - 1 && dy <= crossEnd + 1) || (dy <= -crossStart + 1 && dy >= -crossEnd - 1)))
                        {
                            pixels[y * size + x] = _outlineColor;
                        }
                        if (Mathf.Abs(dy) <= 2.2f && ((dx >= crossStart - 1 && dx <= crossEnd + 1) || (dx <= -crossStart + 1 && dx >= -crossEnd - 1)))
                        {
                            pixels[y * size + x] = _outlineColor;
                        }
                    }
                }

                // 2단계: 핵심 밝은 에임 컬러 덮어씌우기
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float dx = x - center;
                        float dy = y - center;
                        float dist = Mathf.Sqrt(dx * dx + dy * dy);

                        // 중앙 점 본체
                        if (dist <= dotRadius)
                        {
                            pixels[y * size + x] = Color.white;
                        }

                        // 외곽 링 본체
                        if (Mathf.Abs(dist - ringRadius) <= ringThickness * 0.65f)
                        {
                            pixels[y * size + x] = _aimColor;
                        }

                        // 4방향 십자선 본체 (두께 1.5px)
                        if (Mathf.Abs(dx) <= 0.8f && ((dy >= crossStart && dy <= crossEnd) || (dy <= -crossStart && dy >= -crossEnd)))
                        {
                            pixels[y * size + x] = _aimColor;
                        }
                        if (Mathf.Abs(dy) <= 0.8f && ((dx >= crossStart && dx <= crossEnd) || (dx <= -crossStart && dx >= -crossEnd)))
                        {
                            pixels[y * size + x] = _aimColor;
                        }
                    }
                }
                _aimCursorTexture.SetPixels(pixels);
                _aimCursorTexture.Apply();
            }
        }

        /// <summary>
        /// 인게임 에임 커서 활성화 및 Confined 영역 제한 적용
        /// </summary>
        public void ApplyAimCursor()
        {
            _isAimCursorActive = true;
            if (_aimCursorTexture != null)
            {
                Cursor.SetCursor(_aimCursorTexture, _hotspot, CursorMode.Auto);
            }
            Cursor.visible = true;

            if (_confineCursor)
            {
                Cursor.lockState = CursorLockMode.Confined;
            }
            Debug.Log("<color=cyan>[AimCursorManager] 대형 에임 포인터 적용 및 화면 영역 제한(Confined) 활성화 완료.</color>");
        }

        /// <summary>
        /// UI(선택창/인벤토리 등) 팝업 시 기본 커서로 일시 전환
        /// </summary>
        public void SetNormalCursorForUI()
        {
            _isAimCursorActive = false;
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        /// <summary>
        /// UI가 닫혔을 때 다시 에임 커서 및 영역 제한 복원
        /// </summary>
        public void RestoreAimCursor()
        {
            ApplyAimCursor();
        }
    }
}
