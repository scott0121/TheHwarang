using MythOfGuotaji.Actor;
using MythOfGuotaji.UI.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MythOfGuotaji.UI
{
    /// <summary>
    /// 거타지 활 차징 공격 시 머리 위에 표시되는 1.5초 프로그레스 바 인디케이터 (UGUI + TMP)
    /// - 클릭을 0.3초 이상 유지할 때부터 노출
    /// - 0% ~ 100% 점진적 충전
    /// - 100% 충전 완료 시 시각적 흔들림(Shake) 애니메이션 및 네온 발광
    /// - UGUI Canvas 기반으로 OnGUI 배제 및 GC 최소화
    /// </summary>
    public class ChargeIndicatorUI : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private PlayerController _player;

        [Header("Threshold Settings")]
        [SerializeField] private float _showThreshold = 0.3f;

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

        private GameObject _groundAimBox;
        private Transform _fillTransform;
        private Material _aimBoxMaterial;

        // UGUI References
        private Canvas _canvas;
        private RectTransform _rootRect;
        private CanvasGroup _canvasGroup;
        private RectTransform _barRect;
        private Image _fillImage;
        private Image _borderImage;
        private TextMeshProUGUI _chargeLabel;

        private void Awake()
        {
            if (_player == null) _player = GetComponentInParent<PlayerController>();
            _cam = Camera.main;

            BuildUGUI();
        }

        private void BuildUGUI()
        {
            if (_canvas != null) return;

            _canvas = UIKit.CreateCanvas("[ChargeIndicator_Canvas]", UITheme.Sorting.HUD, transform, false);
            _rootRect = (RectTransform)_canvas.transform;

            var container = UIKit.CreateRect("ChargeContainer", _canvas.transform);
            _barRect = container;
            _canvasGroup = container.gameObject.AddComponent<CanvasGroup>();
            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;

            _barRect.sizeDelta = new Vector2(_barWidth, _barHeight);
            _barRect.pivot = new Vector2(0.5f, 0.5f);

            // 외곽 테두리
            _borderImage = UIKit.CreatePanel(_barRect, "Border", new Color(0.6f, 0.6f, 0.7f, 0.65f), true);
            UIKit.Stretch(_borderImage.rectTransform, -2f, -2f, -2f, -2f);

            // 배경
            var bg = UIKit.CreatePanel(_barRect, "Bg", new Color(0.06f, 0.06f, 0.1f, 0.88f), true);

            // 채움 바
            var fillObj = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            fillObj.transform.SetParent(_barRect, false);
            _fillImage = fillObj.GetComponent<Image>();
            _fillImage.sprite = UIKit.WhiteSprite;
            _fillImage.type = Image.Type.Filled;
            _fillImage.fillMethod = Image.FillMethod.Horizontal;
            _fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            _fillImage.fillAmount = 0f;
            _fillImage.raycastTarget = false;
            UIKit.Stretch((RectTransform)fillObj.transform);

            // 라벨
            _chargeLabel = UIKit.CreateLabel(_barRect, "Label", "", 11f, Color.white, TextAlignmentOptions.Center, FontStyles.Bold, false, false);
            UIKit.Stretch(_chargeLabel.rectTransform);
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

            bool shouldDisplay = isCharging && (holdDuration >= _showThreshold);
            float targetAlpha = shouldDisplay ? 1f : 0f;
            _displayAlpha = Mathf.MoveTowards(_displayAlpha, targetAlpha, Time.deltaTime * 14f);

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

            float chargedRange = _player.CurrentChargedRange;
            UpdateGroundAimBox(shouldDisplay, chargedRange, isFullCharge);
            UpdateUGUI(ratio, isFullCharge);
        }

        private void UpdateUGUI(float ratio, bool isFull)
        {
            if (_canvasGroup == null || _player == null || _cam == null) return;

            if (_displayAlpha <= 0.01f)
            {
                _canvasGroup.alpha = 0f;
                return;
            }

            Vector3 worldPos = _player.transform.position + _worldOffset;
            Vector3 screenPos = _cam.WorldToScreenPoint(worldPos);

            if (screenPos.z <= 0f)
            {
                _canvasGroup.alpha = 0f;
                return;
            }

            Vector2 shakeOffset = Vector2.zero;
            if (_shakeTimer > 0f)
            {
                float factor = _shakeTimer / _shakeDuration;
                float angle = _shakeTimer * 55f;
                shakeOffset.x = Mathf.Sin(angle) * _shakeIntensity * factor;
                shakeOffset.y = Mathf.Cos(angle * 0.7f) * _shakeIntensity * 0.6f * factor;
            }

            _canvasGroup.alpha = _displayAlpha;

            // 캔버스 로컬 위치로 변환
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_rootRect, screenPos, null, out Vector2 localPoint);
            _barRect.anchoredPosition = localPoint + shakeOffset;

            // 채움 비율
            _fillImage.fillAmount = Mathf.Clamp01(ratio);

            // 색상 변화
            Color fillColor;
            if (isFull)
            {
                float pulse = Mathf.PingPong(Time.time * 6f, 0.4f);
                fillColor = Color.Lerp(new Color(0.1f, 0.9f, 1f), Color.white, pulse);
                _borderImage.color = new Color(0.3f, 1f, 1f, 0.95f);
                _chargeLabel.color = new Color(0.2f, 1f, 1f);
                _chargeLabel.text = "★ FULL CHARGE! (25m) ★";
            }
            else
            {
                fillColor = Color.Lerp(new Color(1f, 0.65f, 0.1f), new Color(1f, 0.92f, 0.25f), ratio);
                _borderImage.color = new Color(0.6f, 0.6f, 0.7f, 0.65f);
                _chargeLabel.color = Color.white;
                _chargeLabel.text = $"{(ratio * 100):F0}% ({_player.CurrentChargedRange:F1}m)";
            }

            _fillImage.color = fillColor;
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

                float displayLength = Mathf.Max(0.4f, range);
                float width = 0.3f;
                if (_fillTransform != null)
                {
                    _fillTransform.localPosition = new Vector3(0f, 0.02f, displayLength * 0.5f);
                    _fillTransform.localScale = new Vector3(width, 0.01f, displayLength);
                }

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

                CreateBorderLine(fillObj.transform, new Vector3(-0.48f, 0.5f, 0f), new Vector3(0.08f, 1.2f, 1f));
                CreateBorderLine(fillObj.transform, new Vector3(0.48f, 0.5f, 0f), new Vector3(0.08f, 1.2f, 1f));
                CreateBorderLine(fillObj.transform, new Vector3(0f, 0.5f, 0.5f), new Vector3(1f, 1.2f, 0.02f));
            }

            _fillTransform.localPosition = new Vector3(0f, 0.02f, range * 0.5f);
            _fillTransform.localScale = new Vector3(width, 0.01f, range);

            var fillRend = fillObj.GetComponent<Renderer>();
            if (fillRend != null)
            {
                _aimBoxMaterial = new Material(Actor.ExpGem.ResolveSafeShader("Universal Render Pipeline/Unlit", "Universal Render Pipeline/Lit", "Sprites/Default", "Unlit/Color"));
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
                var mat = new Material(Actor.ExpGem.ResolveSafeShader("Universal Render Pipeline/Unlit", "Universal Render Pipeline/Lit", "Sprites/Default", "Unlit/Color"));
                Color borderCol = new Color(0.35f, 0.95f, 1.0f, 0.85f);
                mat.color = borderCol;
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", borderCol);
                r.material = mat;
            }
        }

        private void OnDestroy()
        {
            if (_groundAimBox != null) Destroy(_groundAimBox);
            if (_aimBoxMaterial != null) Destroy(_aimBoxMaterial);
        }
    }
}
