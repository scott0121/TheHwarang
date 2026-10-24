
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[ExecuteInEditMode]
public class ragmonFaceToCamera : MonoBehaviour
{

    Camera targetCamera = null;
    public Vector3 newUp = new Vector3(10000, -10000, 0);
    void LateUpdate()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
        gameObject.transform.LookAt(targetCamera.transform);
    }
}
