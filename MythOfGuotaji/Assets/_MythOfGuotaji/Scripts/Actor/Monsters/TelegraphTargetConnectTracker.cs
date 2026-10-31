using UnityEngine;

namespace MythOfGuotaji.Actor.Monsters
{
    /// <summary>
    /// 자기타겟연결형 (TargetConnect) 텔레그래프 트래커
    /// - 시작점(시전자/몬스터)과 목표점(타겟 플레이어)을 연결하는 트레일/라인을 duration 동안 실시간으로 유지
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class TelegraphTargetConnectTracker : MonoBehaviour
    {
        private LineRenderer _line;
        private Transform _sourceTransform;
        private Vector3 _fallbackSourcePos;
        private Transform _targetTransform;
        private Vector3 _fallbackTargetPos;
        private float _duration;
        private float _elapsed;
        private Material _instancedMat;

        private void Awake()
        {
            _line = GetComponent<LineRenderer>();
            ConfigureLineRenderer();
        }

        private void ConfigureLineRenderer()
        {
            if (_line == null) return;
            _line.positionCount = 2;
            _line.useWorldSpace = true;
            _line.startWidth = 0.25f;
            _line.endWidth = 0.25f;
            _line.numCapVertices = 4;
            _line.numCornerVertices = 4;
            _line.alignment = LineAlignment.TransformZ;
        }

        public void Initialize(Transform source, Vector3 sourcePos, Transform target, Vector3 targetPos, float duration, Color color, float width = 0.25f)
        {
            _sourceTransform = source;
            _fallbackSourcePos = sourcePos;
            _targetTransform = target;
            _fallbackTargetPos = targetPos;
            _duration = Mathf.Max(0.05f, duration);
            _elapsed = 0f;

            if (_line == null) _line = GetComponent<LineRenderer>();
            ConfigureLineRenderer();
            _line.startWidth = width;
            _line.endWidth = width;

            _instancedMat = Combat.ZhuParabolicProjectile.CreateTelegraphMaterial(color);
            _line.material = _instancedMat;
            _line.startColor = color;
            _line.endColor = color;

            UpdatePositions();
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            UpdatePositions();

            if (_elapsed >= _duration)
            {
                Destroy(gameObject);
            }
        }

        private void UpdatePositions()
        {
            if (_line == null) return;

            Vector3 start = _sourceTransform != null ? _sourceTransform.position : _fallbackSourcePos;
            Vector3 end = _targetTransform != null ? _targetTransform.position : _fallbackTargetPos;

            // 지면 위 살짝 띄움
            start.y = Mathf.Max(start.y, 0.08f) + 0.3f;
            end.y = Mathf.Max(end.y, 0.08f) + 0.3f;

            _line.SetPosition(0, start);
            _line.SetPosition(1, end);
        }

        private void OnDestroy()
        {
            if (_instancedMat != null)
            {
                Destroy(_instancedMat);
            }
        }
    }
}
