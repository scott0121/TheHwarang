using System;
using PrimeTween;
using TMPro;
using UnityEngine;

namespace MythOfGuotaji.UI
{
    /// <summary>
    /// 공격 데미지 타입
    /// </summary>
    public enum DamageCounterType
    {
        Normal,     // 화살/단검 일반공격: 흰색 숫자 + Outline, size=30
        Reflected,  // 패링해서 반격한 공격: 노란색 숫자 + Outline, size=35
        Critical    // 일반/반사의 크리티컬: 붉은색 숫자 + Outline, size=45
    }

    /// <summary>
    /// 몬스터 머리 위에 3초간 1미터 상승하면서 FadeOut 되는 TextMeshPro UGUI 기반 데미지 카운터
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class AttackDamageCounter : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI _textMesh;
        [SerializeField] private CanvasGroup _canvasGroup;

        [Header("Animation Settings")]
        [SerializeField] private DamageCounterType _counterType = DamageCounterType.Normal;
        [SerializeField] private float _riseDistance = 1.0f; // 1미터 올라감
        [SerializeField] private float _duration = 3.0f;     // 3초간 진행
        [SerializeField] private float _endScale = 0.02f;
        [SerializeField] private AnimationCurve _scaleAnimationCurve;

        private Sequence _sequence;
        private Camera _mainCam;
        private Action<AttackDamageCounter> _onCompleteCallback;

        public DamageCounterType CounterType => _counterType;

        private void Awake()
        {
            if (_canvasGroup == null) _canvasGroup = GetComponent<CanvasGroup>();
            if (_textMesh == null) _textMesh = GetComponentInChildren<TextMeshProUGUI>();
            _mainCam = Camera.main;
        }

        private void OnEnable()
        {
            if (_mainCam == null) _mainCam = Camera.main;
        }

        private void LateUpdate()
        {
            // 항상 메인 카메라를 향해 Billboard 정면 유지
            if (_mainCam == null) _mainCam = Camera.main;
            if (_mainCam != null)
            {
                transform.rotation = _mainCam.transform.rotation;
            }
        }

        /// <summary>
        /// 데미지 카운터 초기화 및 PrimeTween 연출 시작
        /// </summary>
        public void Initialize(float damage, DamageCounterType type, Vector3 spawnPosition, Action<AttackDamageCounter> onComplete = null)
        {
            if (_canvasGroup == null) _canvasGroup = GetComponent<CanvasGroup>();
            if (_textMesh == null) _textMesh = GetComponentInChildren<TextMeshProUGUI>();
            if (_mainCam == null) _mainCam = Camera.main;

            _counterType = type;
            _onCompleteCallback = onComplete;

            // 1. 데미지 텍스트 표기
            int displayDamage = Mathf.Max(1, Mathf.RoundToInt(damage));
            if (_textMesh != null)
            {
                _textMesh.text = displayDamage.ToString();
                ApplyStyle(type);
            }

            // 2. 초기 위치 및 회전, 알파 설정
            transform.position = spawnPosition;
            if (_mainCam != null)
            {
                transform.rotation = _mainCam.transform.rotation;
            }
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1.0f;
            }

            // 3. PrimeTween 애니메이션 시작 (몬스터 머리 위에서 3초간 1m 상승 & FadeOut)
            if (_sequence.isAlive)
            {
                _sequence.Stop();
            }

            float targetY = spawnPosition.y + _riseDistance;
            transform.localScale = Vector3.zero;
            _sequence = Sequence.Create()
                .Group(Tween.PositionY(transform, targetY, _duration, Ease.Linear))
                .Group(Tween.Alpha(_canvasGroup, 0f, _duration, Ease.InQuad))
                .Group(Tween.Scale(transform, _endScale, _duration, _scaleAnimationCurve))
                .OnComplete(this, target => target.Finish());
        }

        /// <summary>
        /// 타입별 스타일(색상, 폰트 크기, 아웃라인) 적용
        /// </summary>
        public void ApplyStyle(DamageCounterType type)
        {
            if (_textMesh == null) return;

            switch (type)
            {
                case DamageCounterType.Normal:
                    // 화살/단검 일반공격 = 흰색 숫자 + Outline, size=30
                    _textMesh.color = Color.white;
                    _textMesh.fontSize = 30f;
                    _textMesh.outlineColor = Color.black;
                    _textMesh.outlineWidth = 0.2f;
                    break;

                case DamageCounterType.Reflected:
                    // 패링해서 반격한 공격 = 노란색 숫자 + Outline, size=35
                    _textMesh.color = new Color(1.0f, 0.92f, 0.016f); // Yellow
                    _textMesh.fontSize = 35f;
                    _textMesh.outlineColor = Color.black;
                    _textMesh.outlineWidth = 0.22f;
                    break;

                case DamageCounterType.Critical:
                    // 일반/반사의 크리티컬 = 붉은색 숫자 + Outline, size = 45
                    _textMesh.color = new Color(1.0f, 0.25f, 0.25f); // Red
                    _textMesh.fontSize = 45f;
                    _textMesh.outlineColor = Color.black;
                    _textMesh.outlineWidth = 0.25f;
                    break;
            }
        }

        private void Finish()
        {
            if (_onCompleteCallback != null)
            {
                _onCompleteCallback.Invoke(this);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        private void OnDisable()
        {
            if (_sequence.isAlive)
            {
                _sequence.Stop();
            }
        }
    }
}
