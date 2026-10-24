using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[ExecuteInEditMode]
public class manaCatFaceToMinimap : MonoBehaviour
{
    void LateUpdate()
    {       
        gameObject.transform.rotation = Quaternion.LookRotation(Vector3.up, Vector3.forward);
    }
}
