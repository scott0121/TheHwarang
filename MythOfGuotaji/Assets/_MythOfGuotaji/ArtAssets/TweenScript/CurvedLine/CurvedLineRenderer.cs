using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class CurvedLineRenderer : MonoBehaviour
{
    //PUBLIC
    public float lineSegmentSize = 0.15f;
    public float lineWidth = 0.1f;
    [Header("Gizmos")]
    public bool showGizmos = true;
    public float gizmoSize = 0.1f;
    public Color gizmoColor = new Color(1, 0, 0, 0.5f);
    public int SegmentCount = 20; // 곡선 부드러움 정도
    public float CurveLagSpeed = 5f; // 중간점이 따라오는 속도 (낮을수록 많이 휨)
    public float SnapDistanceThreshold = 1.5f; // 끝점이 이 거리 이상 급격히 이동하면 즉시 스냅 (이전 위치에서 끌려오는 현상 방지)

    private Vector3 _currentMidPoint;
    private LineRenderer _lineRenderer;
    //PRIVATE
    private CurvedLinePoint[] linePoints = new CurvedLinePoint[0];
    private Vector3[] linePositions = new Vector3[0];
    private Vector3[] linePositionsOld = new Vector3[0];
    private bool _needsReset = true;

    void Awake()
    {
        _lineRenderer = GetComponent<LineRenderer>();
        _lineRenderer.positionCount = SegmentCount;
        _lineRenderer.useWorldSpace = true; // 월드 좌표 필수
        linePoints = GetComponentsInChildren<CurvedLinePoint>();
        linePositions = new Vector3[linePoints.Length];
        linePositionsOld = new Vector3[linePoints.Length];
    }

    void OnEnable()
    {
        _needsReset = true;
    }

    void OnDisable()
    {
        _needsReset = true;
    }

    // 캐릭터 이동, 카메라, Fusion Render()가 모두 끝난 LateUpdate에서 갱신하여 1프레임 잔상 방지
    private void LateUpdate()
    {
        UpdateLine();
    }

    // 외부에서 레이저 대상을 바꾸거나 새로 켤 때 즉시 직선으로 초기화
    public void ResetLine()
    {
        _needsReset = true;
        UpdateLine();
    }

    public void UpdateLine()
    {
        GetPoints();
        SetPointsToLine();
    }

    // 기존 Update() 호출부와의 호환성 유지
    public void Update()
    {
        UpdateLine();
    }

    private void OnDrawGizmos()
    {
        if (linePoints.Length == 0)
        {
            GetPoints();
        }

        //settings for gizmos
        foreach (var linePoint in linePoints)
        {
            if (linePoint != null)
            {
                linePoint.showGizmo = showGizmos;
                linePoint.gizmoSize = gizmoSize;
                linePoint.gizmoColor = gizmoColor;
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        UpdateLine();
    }

    private void GetPoints()
    {
        if (linePositions.Length != linePoints.Length)
        {
            linePositions = new Vector3[linePoints.Length];
        }
        for (int i = 0; i < linePoints.Length; i++)
        {
            if (linePoints[i] != null)
            {
                linePoints[i].UpdatePosition(); // Force update to latest targets
                linePositions[i] = linePoints[i].transform.position;
            }
        }
    }

    private void SetPointsToLine()
    {
        if (linePositions.Length < 2) return;

        //create old positions if they dont match
        if (linePositionsOld.Length != linePositions.Length)
        {
            linePositionsOld = new Vector3[linePositions.Length];
        }

        Vector3 targetMidPoint = (linePositions[0] + linePositions[1]) / 2f;

        // 끝점이나 시작점이 순간적으로 크게 이동했거나 리셋이 필요할 경우 즉시 스냅
        bool isLargeJump = linePositionsOld.Length >= 2 &&
            (Vector3.Distance(linePositions[1], linePositionsOld[1]) > SnapDistanceThreshold ||
             Vector3.Distance(linePositions[0], linePositionsOld[0]) > SnapDistanceThreshold);

        if (_needsReset || isLargeJump)
        {
            _currentMidPoint = targetMidPoint;
            _needsReset = false;

            for (int i = 0; i < linePositions.Length; i++)
            {
                linePositionsOld[i] = linePositions[i];
            }

            DrawQuadraticBezierCurve(linePositions[0], _currentMidPoint, linePositions[1]);
            return;
        }

        //check if line points have moved
        bool moved = false;
        for (int i = 0; i < linePositions.Length; i++)
        {
            if (linePositions[i] != linePositionsOld[i])
            {
                moved = true;
                linePositionsOld[i] = linePositions[i];
            }
        }

        //update if moved or reset needed
        if (moved || _currentMidPoint != targetMidPoint)
        {
            _currentMidPoint = Vector3.Lerp(_currentMidPoint, targetMidPoint, Time.deltaTime * CurveLagSpeed);
            DrawQuadraticBezierCurve(linePositions[0], _currentMidPoint, linePositions[1]);
        }
    }

    private void DrawQuadraticBezierCurve(Vector3 p0, Vector3 p1, Vector3 p2)
    {
        for (int i = 0; i < SegmentCount; i++)
        {
            float t = i / (float)(SegmentCount - 1);
            // 2차 베지어 공식: (1-t)^2 * P0 + 2(1-t)t * P1 + t^2 * P2
            Vector3 pixel = CalculateBezierPoint(t, p0, p1, p2);
            _lineRenderer.SetPosition(i, pixel);
        }
    }

    private Vector3 CalculateBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2)
    {
        float u = 1 - t;
        float tt = t * t;
        float uu = u * u;

        Vector3 p = (uu * p0) + (2 * u * t * p1) + (tt * p2);
        return p;
    }
}
