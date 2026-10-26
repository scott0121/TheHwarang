using MythOfGuotaji.Actor;
using UnityEngine;

namespace MythOfGuotaji.UI
{
    /// <summary>
    /// 거타지 활 차징 공격 시 머리 위에 표시되는 1.5초 프로그레스 바 인디케이터
    /// - 클릭을 0.5초 이상 유지할 때부터 노출
    /// - 0% ~ 100% (최대 1.5초) 점진적 충전
    /// - 100% 충전 완료 시 시각적 흔들림(Shake) 애니메이션 및 네온 발광
    /// - 발사 또는 취소 시 부드러운 페이드 아웃
    /// </summary>
    public class ChargeIndicatorUI : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private PlayerController _player;

        [Header("Threshold Settings")]
        [SerializeField] private float _showThreshold = 0.3f; // 0.3초부터 노출

        [Header("Visual Settings")]
        [SerializeField] private Vector3 _worldOffset = new Vector3(0f, 2.3f, 0f);
        [SerializeField] private float _barWidth = 140f;
        [SerializeField] private float _barHeight = 16f;
        [SerializeField] private float _shakeIntensity = 9f;
        [SerializeField] private float _shakeDuration = 0.35f;

        private Camera _cam;
        private float _shakeTimer = 0f;
        private bool _wasFullCharged = false;
        private float _displayAlpha = 0f;

        private Texture2D _whiteTex;
        private GameObject _groundAimBox;
        private Transform _fillTransform;
        private Material _aimBoxMaterial;

        private void Awake()
        {
            if (_player == null) _player = GetComponentInParent<PlayerController>();
            _cam = Camera.main;

            _whiteTex = new Texture2D(1, 1);
            _whiteTex.SetPixel(0, 0, Color.white);
            _whiteTex.Apply();
        }

        private void Update()
        {
            if (_player == null)
            {
                _player = FindAnyObjectByType<PlayerController>();
                if (_player == null) return;
            }

            if (_cam == null) _cam = Camera.main;

            bool isCharging = _player.IsCharging;
            float holdDuration = _player.ChargeHoldDuration;
            float ratio = _player.CurrentChargeRatio;

            // 클릭을 0.3초 이상 지속할 때부터 머리 위 게이지 및 바닥 궤적 UI 노출
            bool shouldDisplay = isCharging && (holdDuration >= _showThreshold);
            float targetAlpha = shouldDisplay ? 1f : 0f;
            _displayAlpha = Mathf.MoveTowards(_displayAlpha, targetAlpha, Time.deltaTime * 14f);

            // 3초(100%) 가득 찬 순간 1회 흔들림 트리거
            bool isFullCharge = isCharging && (ratio >= 0.999f || holdDuration >= _player.MaxChargeTime);
            if (isFullCharge)
            {
                if (!_wasFullCharged)
                {
                    _wasFullCharged = true;
                    _shakeTimer = _shakeDuration;
                }
            }
            else if (!isCharging)
            {
                _wasFullCharged = false;
            }

            if (_shakeTimer > 0f)
            {
                _shakeTimer -= Time.deltaTime;
            }

            // 차징 바닥 궤적 UI가 차징 UI와 함께 나오는 것으로 수정 (0.3초부터 노출, 0~25m 선형 증가)
            float chargedRange = _player.CurrentChargedRange;
            UpdateGroundAimBox(shouldDisplay, chargedRange, isFullCharge);
        }

        private void UpdateGroundAimBox(bool shouldShow, float range, bool isFull)
        {
            if (shouldShow && _player != null)
            {
                if (_groundAimBox == null)
                {
                    SetupGroundAimBox();
                }

                _groundAimBox.SetActive(true);

                Vector3 playerPos = _player.transform.position;
                _groundAimBox.transform.position = new Vector3(playerPos.x, 0.05f, playerPos.z);

                Vector3 aimDir = _player.AimDirection;
                aimDir.y = 0f;
                if (aimDir.sqrMagnitude > 0.001f)
                {
                    _groundAimBox.transform.rotation = Quaternion.LookRotation(aimDir.normalized, Vector3.up);
                }

                // 현재 차징 길이에 맞춰 바닥 궤적 박스 길이 동적 확장 (0.3m 폭, range 길이)
                float displayLength = Mathf.Max(0.4f, range);
                float width = 0.3f;
                if (_fillTransform != null)
                {
                    _fillTransform.localPosition = new Vector3(0f, 0.02f, displayLength * 0.5f);
                    _fillTransform.localScale = new Vector3(width, 0.01f, displayLength);
                }

                // 은은한 네온 펄스 효과 (완충 시 눈부시게 발광)
                if (_aimBoxMaterial != null)
                {
                    float pulse = Mathf.PingPong(Time.time * (isFull ? 7f : 3f), 0.25f);
                    Color c = isFull 
                        ? new Color(0.2f, 1.0f, 1.0f, 0.65f + pulse) 
                        : new Color(0.15f, 0.85f, 1.0f, 0.30f + pulse * 0.5f);
                    _aimBoxMaterial.color = c;
                    if (_aimBoxMaterial.HasProperty("_BaseColor")) _aimBoxMaterial.SetColor("_BaseColor", c);
                }
            }
            else
            {
                if (_groundAimBox != null && _groundAimBox.activeSelf)
                {
                    _groundAimBox.SetActive(false);
                }
            }
        }

        private void SetupGroundAimBox()
        {
            if (_groundAimBox != null) return;

            GameObject aimBoxPrefab = null;
            if (Data.GameDataManager.Instance != null && Data.GameDataManager.Instance.Prefabs != null)
            {
                aimBoxPrefab = Data.PrefabDatabaseSO.ResolvePrefab(ref Data.GameDataManager.Instance.Prefabs.chargeAimBoxPrefab, "Prefabs/Telegraph_ChargeAimBox");
            }
            else
            {
                aimBoxPrefab = Resources.Load<GameObject>("Prefabs/Telegraph_ChargeAimBox");
            }

            if (aimBoxPrefab != null)
            {
                _groundAimBox = Instantiate(aimBoxPrefab, Vector3.zero, Quaternion.identity);
                _groundAimBox.name = "[ChargeAimBox_Telegraph]";
            }
            else
            {
                _groundAimBox = new GameObject("[ChargeAimBox_Telegraph]");
                _groundAimBox.transform.position = Vector3.zero;
            }

            float range = 1.0f;
            float width = 0.3f;

            // 1. 바닥 면 (Semi-transparent inner fill)
            var existingFill = _groundAimBox.transform.Find("FillPlane");
            GameObject fillObj;
            if (existingFill != null)
            {
                fillObj = existingFill.gameObject;
                _fillTransform = fillObj.transform;
            }
            else
            {
                fillObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                fillObj.name = "FillPlane";
                fillObj.transform.SetParent(_groundAimBox.transform, false);
                _fillTransform = fillObj.transform;
                var fillCol = fillObj.GetComponent<Collider>();
                if (fillCol != null) Destroy(fillCol);

                // 2. 테두리 외곽선 (Left & Right Border, Front Tip)
                CreateBorderLine(fillObj.transform, new Vector3(-0.48f, 0.5f, 0f), new Vector3(0.08f, 1.2f, 1f));
                CreateBorderLine(fillObj.transform, new Vector3(0.48f, 0.5f, 0f), new Vector3(0.08f, 1.2f, 1f));
                CreateBorderLine(fillObj.transform, new Vector3(0f, 0.5f, 0.5f), new Vector3(1f, 1.2f, 0.02f));
            }

            _fillTransform.localPosition = new Vector3(0f, 0.02f, range * 0.5f);
            _fillTransform.localScale = new Vector3(width, 0.01f, range);

            var fillRend = fillObj.GetComponent<Renderer>();
            if (fillRend != null)
            {
                _aimBoxMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color"));
                Color aimColor = new Color(0.15f, 0.85f, 1.0f, 0.40f);
                _aimBoxMaterial.color = aimColor;
                if (_aimBoxMaterial.HasProperty("_BaseColor")) _aimBoxMaterial.SetColor("_BaseColor", aimColor);
                fillRend.material = _aimBoxMaterial;
            }

            _groundAimBox.SetActive(false);
        }

        private void CreateBorderLine(Transform parent, Vector3 localPos, Vector3 localScale)
        {
            var line = GameObject.CreatePrimitive(PrimitiveType.Cube);
            line.name = "BorderLine";
            line.transform.SetParent(parent, false);
            line.transform.localPosition = localPos;
            line.transform.localScale = localScale;

            var c = line.GetComponent<Collider>();
            if (c != null) Destroy(c);

            var r = line.GetComponent<Renderer>();
            if (r != null)
            {
                var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color"));
                Color borderCol = new Color(0.35f, 0.95f, 1.0f, 0.85f);
                mat.color = borderCol;
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", borderCol);
                r.material = mat;
            }
        }

        private void OnGUI()
        {
            if (_displayAlpha <= 0.01f || _player == null || _cam == null) return;

            Vector3 worldPos = _player.transform.position + _worldOffset;
            Vector3 screenPos = _cam.WorldToScreenPoint(worldPos);

            // 카메라 뒤에 있으면 렌더링 생략
            if (screenPos.z <= 0f) return;

            // 흔들림 오프셋 계산
            Vector2 shakeOffset = Vector2.zero;
            if (_shakeTimer > 0f)
            {
                float factor = _shakeTimer / _shakeDuration;
                float angle = _shakeTimer * 55f;
                shakeOffset.x = Mathf.Sin(angle) * _shakeIntensity * factor;
                shakeOffset.y = Mathf.Cos(angle * 0.7f) * _shakeIntensity * 0.6f * factor;
            }

            float ratio = _player.CurrentChargeRatio;
            bool isFull = ratio >= 0.999f;

            float x = screenPos.x - (_barWidth * 0.5f) + shakeOffset.x;
            float y = (Screen.height - screenPos.y) - (_barHeight * 0.5f) + shakeOffset.y;

            Color prevColor = GUI.color;

            // 1. 배경 박스 (다크 반투명)
            Rect bgRect = new Rect(x - 2, y - 2, _barWidth + 4, _barHeight + 4);
            GUI.color = new Color(0.06f, 0.06f, 0.1f, 0.88f * _displayAlpha);
            GUI.DrawTexture(bgRect, _whiteTex);

            // 2. 채움 바 (0%~99%: 웜 골드/앰버 -> 100%: 눈부신 영기 시안/스카이블루)
            Color fillColor;
            if (isFull)
            {
                float pulse = Mathf.PingPong(Time.time * 6f, 0.4f);
                fillColor = Color.Lerp(new Color(0.1f, 0.9f, 1f), Color.white, pulse);
            }
            else
            {
                fillColor = Color.Lerp(new Color(1f, 0.65f, 0.1f), new Color(1f, 0.92f, 0.25f), ratio);
            }

            fillColor.a *= _displayAlpha;
            GUI.color = fillColor;

            Rect fillRect = new Rect(x, y, _barWidth * ratio, _barHeight);
            GUI.DrawTexture(fillRect, _whiteTex);

            // 3. 테두리
            GUI.color = isFull 
                ? new Color(0.3f, 1f, 1f, 0.95f * _displayAlpha) 
                : new Color(0.6f, 0.6f, 0.7f, 0.65f * _displayAlpha);
            DrawOutline(new Rect(x - 1, y - 1, _barWidth + 2, _barHeight + 2), 1.5f);

            // 4. 텍스트 라벨
            GUI.color = Color.white * _displayAlpha;
            GUIStyle textStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 11,
                fontStyle = FontStyle.Bold
            };

            string text = isFull ? "★ FULL CHARGE! (25m) ★" : $"{(ratio * 100):F0}% ({_player.CurrentChargedRange:F1}m)";
            textStyle.normal.textColor = isFull ? new Color(0.2f, 1f, 1f) : Color.white;
            GUI.Label(new Rect(x, y - 1, _barWidth, _barHeight), text, textStyle);

            GUI.color = prevColor;
        }

        private void DrawOutline(Rect rect, float thickness)
        {
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, thickness), _whiteTex);
            GUI.DrawTexture(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), _whiteTex);
            GUI.DrawTexture(new Rect(rect.x, rect.y, thickness, rect.height), _whiteTex);
            GUI.DrawTexture(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), _whiteTex);
        }

        private void OnDestroy()
        {
            if (_whiteTex != null) Destroy(_whiteTex);
            if (_groundAimBox != null) Destroy(_groundAimBox);
            if (_aimBoxMaterial != null) Destroy(_aimBoxMaterial);
        }
    }
}
