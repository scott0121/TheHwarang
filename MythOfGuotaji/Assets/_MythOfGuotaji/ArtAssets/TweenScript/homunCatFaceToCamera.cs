using UnityEngine;

[ExecuteInEditMode]
public class HomunCatFaceToCamera : MonoBehaviour
{
    private Camera targetCamera;

    void Start()
    {
        // 기준이 될 카메라를 찾습니다. 기본적으로 메인 카메라를 사용합니다.
        // (만약 미니맵 전용 카메라를 바라봐야 한다면 해당 카메라를 연결해 주어야 합니다.)
        targetCamera = Camera.main;
    }

    void LateUpdate()
    {
        if (targetCamera == null) return;

        // [방법 1] 카메라의 회전과 똑같이 맞추는 방법 (가장 추천)
        // 2D 스프라이트나 UI(Quad 등)가 왜곡 없이 화면에 평행하게 보이도록 할 때 가장 좋습니다.
        //transform.rotation = targetCamera.transform.rotation;

         // [방법 2] 카메라의 위치를 직접 쳐다보게 하는 방법
        // 3D 모델의 얼굴이나 포탑 등이 카메라 쪽으로 고개를 돌려야 할 때 적합합니다.
        // 단, 2D 스프라이트의 경우 좌우가 반전되어 보일 수 있습니다.
        transform.LookAt(targetCamera.transform);
        
    }
}