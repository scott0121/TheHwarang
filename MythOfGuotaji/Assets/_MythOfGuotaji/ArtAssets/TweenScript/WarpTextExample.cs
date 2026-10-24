using UnityEngine;
using TMPro;

[ExecuteAlways]
public class WarpTextExample : MonoBehaviour
{
    private TMP_Text m_TextComponent;

    [Header("Warp Settings")]
    public AnimationCurve VertexCurve = new AnimationCurve(
        new Keyframe(0, 0),
        new Keyframe(0.25f, 2.0f),
        new Keyframe(0.5f, 0),
        new Keyframe(0.75f, 2.0f),
        new Keyframe(1, 0f)
    );
    public float CurveScale = 1.0f;

    void Awake()
    {
        m_TextComponent = GetComponent<TMP_Text>();
    }
    void OnEnable()
    {
        // 오브젝트가 켜질 때마다 텍스트가 변경된 것처럼 강제 인식시킵니다.
        if (m_TextComponent != null)
        {
            m_TextComponent.havePropertiesChanged = true;
        }
    }
    void LateUpdate()
    {
        // 텍스트나 인스펙터 설정이 변경되었을 때만 갱신 (성능 최적화)
        if (m_TextComponent != null && m_TextComponent.havePropertiesChanged)
        {
            WarpText();
        }
    }

    private void WarpText()
    {
        m_TextComponent.ForceMeshUpdate();

        TMP_TextInfo textInfo = m_TextComponent.textInfo;
        int characterCount = textInfo.characterCount;

        if (characterCount == 0) return;

        float boundsMinX = m_TextComponent.bounds.min.x;
        float boundsMaxX = m_TextComponent.bounds.max.x;

        // 0으로 나누기 방지
        float boundsWidth = boundsMaxX - boundsMinX;
        if (boundsWidth <= 0) boundsWidth = 0.0001f;

        for (int i = 0; i < characterCount; i++)
        {
            if (!textInfo.characterInfo[i].isVisible)
                continue;

            int vertexIndex = textInfo.characterInfo[i].vertexIndex;
            int materialIndex = textInfo.characterInfo[i].materialReferenceIndex;

            Vector3[] vertices = textInfo.meshInfo[materialIndex].vertices;

            // 각 문자의 중앙 하단(Baseline)을 피벗으로 설정
            Vector3 offsetToMidBaseline = new Vector2((vertices[vertexIndex + 0].x + vertices[vertexIndex + 2].x) / 2, textInfo.characterInfo[i].baseLine);

            vertices[vertexIndex + 0] -= offsetToMidBaseline;
            vertices[vertexIndex + 1] -= offsetToMidBaseline;
            vertices[vertexIndex + 2] -= offsetToMidBaseline;
            vertices[vertexIndex + 3] -= offsetToMidBaseline;

            // 커브 상의 위치 계산 (기존 코드의 CurveScale * 10 적용)
            float x0 = (offsetToMidBaseline.x - boundsMinX) / boundsWidth;
            float x1 = x0 + 0.0001f;
            float y0 = VertexCurve.Evaluate(x0) * CurveScale * 10f;
            float y1 = VertexCurve.Evaluate(x1) * CurveScale * 10f;

            // 회전(Tangent) 각도 계산
            Vector3 horizontal = new Vector3(1, 0, 0);
            Vector3 tangent = new Vector3(x1 * boundsWidth + boundsMinX, y1) - new Vector3(offsetToMidBaseline.x, y0);

            float dot = Mathf.Acos(Vector3.Dot(horizontal, tangent.normalized)) * Mathf.Rad2Deg;
            Vector3 cross = Vector3.Cross(horizontal, tangent);
            float angle = cross.z > 0 ? dot : 360 - dot;

            // 매트릭스 연산을 통한 위치/회전 적용
            Matrix4x4 matrix = Matrix4x4.TRS(new Vector3(0, y0, 0), Quaternion.Euler(0, 0, angle), Vector3.one);

            vertices[vertexIndex + 0] = matrix.MultiplyPoint3x4(vertices[vertexIndex + 0]);
            vertices[vertexIndex + 1] = matrix.MultiplyPoint3x4(vertices[vertexIndex + 1]);
            vertices[vertexIndex + 2] = matrix.MultiplyPoint3x4(vertices[vertexIndex + 2]);
            vertices[vertexIndex + 3] = matrix.MultiplyPoint3x4(vertices[vertexIndex + 3]);

            vertices[vertexIndex + 0] += offsetToMidBaseline;
            vertices[vertexIndex + 1] += offsetToMidBaseline;
            vertices[vertexIndex + 2] += offsetToMidBaseline;
            vertices[vertexIndex + 3] += offsetToMidBaseline;
        }

        // 최종 메쉬 데이터 업로드
        m_TextComponent.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
    }

    // 인스펙터에서 값을 변경할 때 즉각 반영되도록 설정
    private void OnValidate()
    {
        if (m_TextComponent != null)
        {
            m_TextComponent.havePropertiesChanged = true;
        }
    }
}