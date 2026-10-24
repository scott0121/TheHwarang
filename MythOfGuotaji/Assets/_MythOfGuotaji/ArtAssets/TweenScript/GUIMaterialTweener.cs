using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Timeline;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]

public class GUIMaterialTweener : MonoBehaviour, ITimeControl
{

    [SerializeField, HideInInspector]
    public float HiddenFloat0, HiddenFloat1, HiddenFloat2, HiddenFloat3, HiddenFloat4, HiddenFloat5, HiddenFloat6;
    [SerializeField, HideInInspector]
    public string HiddenName0, HiddenName1, HiddenName2, HiddenName3, HiddenName4, HiddenName5, HiddenName6;
    [SerializeField, HideInInspector]
    public int Counter;
    [SerializeField, HideInInspector]
    public GameObject RI;
    [SerializeField, HideInInspector]
    public Material MR;
    [SerializeField, HideInInspector]
    public Image ImageComponent;
    [SerializeField, HideInInspector]
    public RawImage RawImageComponent;
    [SerializeField, HideInInspector]
    public SpriteRenderer SpriteComponent;
    [SerializeField, HideInInspector]
    public RenderMeshOnCanvas RenderMeshComponent;
    [SerializeField, HideInInspector]
    public float valueBack0, valueBack1, valueBack2, valueBack3, valueBack4, valueBack5, valueBack6;
    [SerializeField, HideInInspector]
    private Material MIBack;

    private void Awake()
    {
        Init();
    }
    private void Init()
    {
        if (RI==null) RI = this.gameObject;
        RawImageComponent = RI.GetComponent<RawImage>();

        if (Counter < 1) Counter = 1;
        if (Application.isPlaying)
        {
            if (RawImageComponent != null)
            {
                MIBack = RawImageComponent.material;
                RawImageComponent.material = Instantiate(MIBack);
                MR = RawImageComponent.material;
            }
            else if (RI.GetComponent<Image>() != null)
            {
                ImageComponent = RI.GetComponent<Image>();
                MIBack = ImageComponent.material;
                ImageComponent.material = Instantiate(MIBack);
                MR = ImageComponent.material;
            }
            else if (RI.GetComponent<SpriteRenderer>() != null)
            {
                SpriteComponent = RI.GetComponent<SpriteRenderer>();
                MIBack = SpriteComponent.sharedMaterial;
                MR = SpriteComponent.sharedMaterial;
            }
            else if (RI.GetComponent<RenderMeshOnCanvas>() != null)
            {
                RenderMeshComponent = RI.GetComponent<RenderMeshOnCanvas>();
                MIBack = MR;                
            }
            else
            {
               
            }
        }
        else
        {
            /*
            if (RawImageComponent != null)
            {
                MR = RawImageComponent.material;
                MIBack = MR;
            }
            else if (RI.GetComponent<Image>() != null)
            {
                ImageComponent = RI.GetComponent<Image>();
                MR = ImageComponent.material;
                MIBack = MR;
            }
            else if (RI.GetComponent<SpriteRenderer>() != null)
            {
                SpriteComponent = RI.GetComponent<SpriteRenderer>();
                MR = SpriteComponent.sharedMaterial;
                MIBack = MR;
            }
            else if (RI.GetComponent<RenderMeshOnCanvas>() != null)
            {
                RenderMeshComponent = RI.GetComponent<RenderMeshOnCanvas>();
                MIBack = MR;
            }
            */
        }
        //GetValue();
    }
    private void OnDestroy()
    {
        if (Application.isPlaying)
        {
            if (RawImageComponent != null)
            {
                RawImageComponent.material = MIBack;
            }
            else if (ImageComponent != null)
            {
                ImageComponent.material = MIBack;
            }
            else if (SpriteComponent != null)
            {
                //SpriteComponent.sharedMaterial = MIBack;
            }
        }
        HiddenFloat0 = valueBack0;
        HiddenFloat1 = valueBack1;
        HiddenFloat2 = valueBack2;
        HiddenFloat3 = valueBack3;
        HiddenFloat4 = valueBack4;
        HiddenFloat5 = valueBack5;
        HiddenFloat6 = valueBack6;
        SetValue();
    }

    public void GetMaterial()
    {
        if (RawImageComponent != null)
        {
            MR = RawImageComponent.material;
        }
        else if (ImageComponent != null)
        {
            MR = ImageComponent.material;
        }
        else if (SpriteComponent != null)
        {
            MR = SpriteComponent.sharedMaterial;
        }

    }
    public void GetValue()
    {
        if (MR)
        {
            switch (Counter)
            {
                case 1:
                    valueBack0 = MR.GetFloat("_" + HiddenName0);
                    break;
                case 2:
                    valueBack0 = MR.GetFloat("_" + HiddenName0);
                    valueBack1 = MR.GetFloat("_" + HiddenName1);
                    break;
                case 3:
                    valueBack0 = MR.GetFloat("_" + HiddenName0);
                    valueBack1 = MR.GetFloat("_" + HiddenName1);
                    valueBack2 = MR.GetFloat("_" + HiddenName2);
                    break;
                case 4:
                    valueBack0 = MR.GetFloat("_" + HiddenName0);
                    valueBack1 = MR.GetFloat("_" + HiddenName1);
                    valueBack2 = MR.GetFloat("_" + HiddenName2);
                    valueBack3 = MR.GetFloat("_" + HiddenName3);
                    break;
                case 5:
                    valueBack0 = MR.GetFloat("_" + HiddenName0);
                    valueBack1 = MR.GetFloat("_" + HiddenName1);
                    valueBack2 = MR.GetFloat("_" + HiddenName2);
                    valueBack3 = MR.GetFloat("_" + HiddenName3);
                    valueBack4 = MR.GetFloat("_" + HiddenName4);
                    break;
                case 6:
                    valueBack0 = MR.GetFloat("_" + HiddenName0);
                    valueBack1 = MR.GetFloat("_" + HiddenName1);
                    valueBack2 = MR.GetFloat("_" + HiddenName2);
                    valueBack3 = MR.GetFloat("_" + HiddenName3);
                    valueBack4 = MR.GetFloat("_" + HiddenName4);
                    valueBack5 = MR.GetFloat("_" + HiddenName5);
                    break;
                case 7:
                    valueBack0 = MR.GetFloat("_" + HiddenName0);
                    valueBack1 = MR.GetFloat("_" + HiddenName1);
                    valueBack2 = MR.GetFloat("_" + HiddenName2);
                    valueBack3 = MR.GetFloat("_" + HiddenName3);
                    valueBack4 = MR.GetFloat("_" + HiddenName4);
                    valueBack5 = MR.GetFloat("_" + HiddenName5);
                    valueBack6 = MR.GetFloat("_" + HiddenName6);
                    break;
                default:
                    valueBack0 = MR.GetFloat("_" + HiddenName0);
                    break;
            }
        }
    }
    public void SetValue()
    {
        if (MR)
        {
            switch (Counter)
            {
                case 1:
                    MR.SetFloat("_" + HiddenName0, HiddenFloat0);
                    break;
                case 2:
                    MR.SetFloat("_" + HiddenName0, HiddenFloat0);
                    MR.SetFloat("_" + HiddenName1, HiddenFloat1);
                    break;
                case 3:
                    MR.SetFloat("_" + HiddenName0, HiddenFloat0);
                    MR.SetFloat("_" + HiddenName1, HiddenFloat1);
                    MR.SetFloat("_" + HiddenName2, HiddenFloat2);
                    break;
                case 4:
                    MR.SetFloat("_" + HiddenName0, HiddenFloat0);
                    MR.SetFloat("_" + HiddenName1, HiddenFloat1);
                    MR.SetFloat("_" + HiddenName2, HiddenFloat2);
                    MR.SetFloat("_" + HiddenName3, HiddenFloat3);
                    break;
                case 5:
                    MR.SetFloat("_" + HiddenName0, HiddenFloat0);
                    MR.SetFloat("_" + HiddenName1, HiddenFloat1);
                    MR.SetFloat("_" + HiddenName2, HiddenFloat2);
                    MR.SetFloat("_" + HiddenName3, HiddenFloat3);
                    MR.SetFloat("_" + HiddenName4, HiddenFloat4);
                    break;
                case 6:
                    MR.SetFloat("_" + HiddenName0, HiddenFloat0);
                    MR.SetFloat("_" + HiddenName1, HiddenFloat1);
                    MR.SetFloat("_" + HiddenName2, HiddenFloat2);
                    MR.SetFloat("_" + HiddenName3, HiddenFloat3);
                    MR.SetFloat("_" + HiddenName4, HiddenFloat4);
                    MR.SetFloat("_" + HiddenName5, HiddenFloat5);
                    break;
                case 7:
                    MR.SetFloat("_" + HiddenName0, HiddenFloat0);
                    MR.SetFloat("_" + HiddenName1, HiddenFloat1);
                    MR.SetFloat("_" + HiddenName2, HiddenFloat2);
                    MR.SetFloat("_" + HiddenName3, HiddenFloat3);
                    MR.SetFloat("_" + HiddenName4, HiddenFloat4);
                    MR.SetFloat("_" + HiddenName5, HiddenFloat5);
                    MR.SetFloat("_" + HiddenName6, HiddenFloat6);
                    break;
                default:
                    MR.SetFloat("_" + HiddenName0, HiddenFloat0);
                    break;
            }
        }
    }

    void ITimeControl.OnControlTimeStart()
    {
        Init();
    }
    void ITimeControl.OnControlTimeStop()
    {
        if (Application.isPlaying)
        {
            if (RawImageComponent != null)
            {
                RawImageComponent.material = MIBack;
            }
            else if (ImageComponent != null)
            {
                ImageComponent.material = MIBack;
            }
            else if (SpriteComponent != null)
            {
                SpriteComponent.material = MIBack;
            }
        }
        HiddenFloat0 = valueBack0;
        HiddenFloat1 = valueBack1;
        HiddenFloat2 = valueBack2;
        HiddenFloat3 = valueBack3;
        HiddenFloat4 = valueBack4;
        HiddenFloat5 = valueBack5;
        HiddenFloat6 = valueBack6;
        SetValue();
    }
    void ITimeControl.SetTime(double time)
    {
        SetValue();
        if (Application.isPlaying == false)
        {
            SetValue();
        }
    }

#if UNITY_EDITOR

    [CustomEditor(typeof(GUIMaterialTweener))]
    public class GUIMaterialTweener_Editor : Editor
    {
        SerializedProperty HiddenFloat0, HiddenFloat1, HiddenFloat2, HiddenFloat3, HiddenFloat4, HiddenFloat5, HiddenFloat6;
        SerializedProperty valueBack0, valueBack1, valueBack2, valueBack3, valueBack4, valueBack5, valueBack6;
        SerializedProperty HiddenName0, HiddenName1, HiddenName2, HiddenName3, HiddenName4, HiddenName5, HiddenName6;
        SerializedProperty Counter;
        SerializedProperty RI;
        SerializedProperty MR;

        private void OnEnable()
        {
            HiddenFloat0 = serializedObject.FindProperty("HiddenFloat0");
            HiddenFloat1 = serializedObject.FindProperty("HiddenFloat1");
            HiddenFloat2 = serializedObject.FindProperty("HiddenFloat2");
            HiddenFloat3 = serializedObject.FindProperty("HiddenFloat3");
            HiddenFloat4 = serializedObject.FindProperty("HiddenFloat4");
            HiddenFloat5 = serializedObject.FindProperty("HiddenFloat5");
            HiddenFloat6 = serializedObject.FindProperty("HiddenFloat6");
            valueBack0 = serializedObject.FindProperty("valueBack0");
            valueBack1 = serializedObject.FindProperty("valueBack1");
            valueBack2 = serializedObject.FindProperty("valueBack2");
            valueBack3 = serializedObject.FindProperty("valueBack3");
            valueBack4 = serializedObject.FindProperty("valueBack4");
            valueBack5 = serializedObject.FindProperty("valueBack5");
            valueBack6 = serializedObject.FindProperty("valueBack6");
            HiddenName0 = serializedObject.FindProperty("HiddenName0");
            HiddenName1 = serializedObject.FindProperty("HiddenName1");
            HiddenName2 = serializedObject.FindProperty("HiddenName2");
            HiddenName3 = serializedObject.FindProperty("HiddenName3");
            HiddenName4 = serializedObject.FindProperty("HiddenName4");
            HiddenName5 = serializedObject.FindProperty("HiddenName5");
            HiddenName6 = serializedObject.FindProperty("HiddenName6");
            Counter = serializedObject.FindProperty("Counter");
            //Counter.intValue = 1;
            RI = serializedObject.FindProperty("RI");
            MR = serializedObject.FindProperty("MR");
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.LabelField("-----------------------------\n" +
                "  Material Shader Property Animator for \n  UGUI  UI Shader\n-----------------------------", GUILayout.Height(60));
            EditorGUILayout.PropertyField(RI, new GUIContent("Base Object"));
            EditorGUILayout.PropertyField(MR, new GUIContent("Material"));
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("-------------------------------\n" +
                "   타임라인에 연동해서 \n Animate Value 값을 변화시킵니다. 타임라인 종료후\n   Default Value로 세팅됩니다.\n" +
                "   Property Name 값은 문자열로 직접 입력합니다. \n   Material 창에 보이는 이름\n   그대로 입력합니다.\n " +
                "-------------------------------", GUILayout.Height(100));
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Number of elements to Animate-->");
                Counter.intValue = EditorGUILayout.IntSlider(Counter.intValue, 1, 7);
            }

            EditorGUILayout.Space();
            switch (Counter.intValue)
            {
                case 1:
                    EditorGUILayout.PropertyField(HiddenName0, new GUIContent("Property Name"));
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.PropertyField(HiddenFloat0, new GUIContent("Animate Value"));
                        EditorGUILayout.PropertyField(valueBack0, new GUIContent("Default Value"));
                    }
                    break;
                case 2:
                    EditorGUILayout.PropertyField(HiddenName0, new GUIContent("Property Name"));
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.PropertyField(HiddenFloat0, new GUIContent("Animate Value"));
                        EditorGUILayout.PropertyField(valueBack0, new GUIContent("Default Value"));
                    }
                    EditorGUILayout.Space();
                    EditorGUILayout.PropertyField(HiddenName1, new GUIContent("Property Name"));
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.PropertyField(HiddenFloat1, new GUIContent("Animate Value"));
                        EditorGUILayout.PropertyField(valueBack1, new GUIContent("Default Value"));
                    }
                    break;
                case 3:
                    EditorGUILayout.PropertyField(HiddenName0, new GUIContent("Property Name"));
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.PropertyField(HiddenFloat0, new GUIContent("Animate Value"));
                        EditorGUILayout.PropertyField(valueBack0, new GUIContent("Default Value"));
                    }
                    EditorGUILayout.Space();
                    EditorGUILayout.PropertyField(HiddenName1, new GUIContent("Property Name"));
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.PropertyField(HiddenFloat1, new GUIContent("Animate Value"));
                        EditorGUILayout.PropertyField(valueBack1, new GUIContent("Default Value"));
                    }
                    EditorGUILayout.Space();
                    EditorGUILayout.PropertyField(HiddenName2, new GUIContent("Property Name"));
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.PropertyField(HiddenFloat2, new GUIContent("Animate Value"));
                        EditorGUILayout.PropertyField(valueBack2, new GUIContent("Default Value"));
                    }
                    break;
                case 4:
                    EditorGUILayout.PropertyField(HiddenName0, new GUIContent("Property Name"));
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.PropertyField(HiddenFloat0, new GUIContent("Animate Value"));
                        EditorGUILayout.PropertyField(valueBack0, new GUIContent("Default Value"));
                    }
                    EditorGUILayout.Space();
                    EditorGUILayout.PropertyField(HiddenName1, new GUIContent("Property Name"));
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.PropertyField(HiddenFloat1, new GUIContent("Animate Value"));
                        EditorGUILayout.PropertyField(valueBack1, new GUIContent("Default Value"));
                    }
                    EditorGUILayout.Space();
                    EditorGUILayout.PropertyField(HiddenName2, new GUIContent("Property Name"));
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.PropertyField(HiddenFloat2, new GUIContent("Animate Value"));
                        EditorGUILayout.PropertyField(valueBack2, new GUIContent("Default Value"));
                    }
                    EditorGUILayout.Space();
                    EditorGUILayout.PropertyField(HiddenName3, new GUIContent("Property Name"));
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.PropertyField(HiddenFloat3, new GUIContent("Animate Value"));
                        EditorGUILayout.PropertyField(valueBack3, new GUIContent("Default Value"));
                    }

                    break;
                case 5:
                    EditorGUILayout.PropertyField(HiddenName0, new GUIContent("Property Name"));
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.PropertyField(HiddenFloat0, new GUIContent("Animate Value"));
                        EditorGUILayout.PropertyField(valueBack0, new GUIContent("Default Value"));
                    }
                    EditorGUILayout.Space();
                    EditorGUILayout.PropertyField(HiddenName1, new GUIContent("Property Name"));
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.PropertyField(HiddenFloat1, new GUIContent("Animate Value"));
                        EditorGUILayout.PropertyField(valueBack1, new GUIContent("Default Value"));
                    }
                    EditorGUILayout.Space();
                    EditorGUILayout.PropertyField(HiddenName2, new GUIContent("Property Name"));
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.PropertyField(HiddenFloat2, new GUIContent("Animate Value"));
                        EditorGUILayout.PropertyField(valueBack2, new GUIContent("Default Value"));
                    }
                    EditorGUILayout.Space();
                    EditorGUILayout.PropertyField(HiddenName3, new GUIContent("Property Name"));
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.PropertyField(HiddenFloat3, new GUIContent("Animate Value"));
                        EditorGUILayout.PropertyField(valueBack3, new GUIContent("Default Value"));
                    }
                    EditorGUILayout.Space();
                    EditorGUILayout.PropertyField(HiddenName4, new GUIContent("Property Name"));
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.PropertyField(HiddenFloat4, new GUIContent("Animate Value"));
                        EditorGUILayout.PropertyField(valueBack4, new GUIContent("Default Value"));
                    }
                    break;
                case 6:
                    EditorGUILayout.PropertyField(HiddenName0, new GUIContent("Property Name"));
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.PropertyField(HiddenFloat0, new GUIContent("Animate Value"));
                        EditorGUILayout.PropertyField(valueBack0, new GUIContent("Default Value"));
                    }
                    EditorGUILayout.Space();
                    EditorGUILayout.PropertyField(HiddenName1, new GUIContent("Property Name"));
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.PropertyField(HiddenFloat1, new GUIContent("Animate Value"));
                        EditorGUILayout.PropertyField(valueBack1, new GUIContent("Default Value"));
                    }
                    EditorGUILayout.Space();
                    EditorGUILayout.PropertyField(HiddenName2, new GUIContent("Property Name"));
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.PropertyField(HiddenFloat2, new GUIContent("Animate Value"));
                        EditorGUILayout.PropertyField(valueBack2, new GUIContent("Default Value"));
                    }
                    EditorGUILayout.Space();
                    EditorGUILayout.PropertyField(HiddenName3, new GUIContent("Property Name"));
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.PropertyField(HiddenFloat3, new GUIContent("Animate Value"));
                        EditorGUILayout.PropertyField(valueBack3, new GUIContent("Default Value"));
                    }
                    EditorGUILayout.Space();
                    EditorGUILayout.PropertyField(HiddenName4, new GUIContent("Property Name"));
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.PropertyField(HiddenFloat4, new GUIContent("Animate Value"));
                        EditorGUILayout.PropertyField(valueBack4, new GUIContent("Default Value"));
                    }
                    EditorGUILayout.Space();
                    EditorGUILayout.PropertyField(HiddenName5, new GUIContent("Property Name"));
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.PropertyField(HiddenFloat5, new GUIContent("Animate Value"));
                        EditorGUILayout.PropertyField(valueBack5, new GUIContent("Default Value"));
                    }
                    break;
                case 7:
                    EditorGUILayout.PropertyField(HiddenName0, new GUIContent("Property Name"));
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.PropertyField(HiddenFloat0, new GUIContent("Animate Value"));
                        EditorGUILayout.PropertyField(valueBack0, new GUIContent("Default Value"));
                    }
                    EditorGUILayout.Space();
                    EditorGUILayout.PropertyField(HiddenName1, new GUIContent("Property Name"));
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.PropertyField(HiddenFloat1, new GUIContent("Animate Value"));
                        EditorGUILayout.PropertyField(valueBack1, new GUIContent("Default Value"));
                    }
                    EditorGUILayout.Space();
                    EditorGUILayout.PropertyField(HiddenName2, new GUIContent("Property Name"));
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.PropertyField(HiddenFloat2, new GUIContent("Animate Value"));
                        EditorGUILayout.PropertyField(valueBack2, new GUIContent("Default Value"));
                    }
                    EditorGUILayout.Space();
                    EditorGUILayout.PropertyField(HiddenName3, new GUIContent("Property Name"));
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.PropertyField(HiddenFloat3, new GUIContent("Animate Value"));
                        EditorGUILayout.PropertyField(valueBack3, new GUIContent("Default Value"));
                    }
                    EditorGUILayout.Space();
                    EditorGUILayout.PropertyField(HiddenName4, new GUIContent("Property Name"));
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.PropertyField(HiddenFloat4, new GUIContent("Animate Value"));
                        EditorGUILayout.PropertyField(valueBack4, new GUIContent("Default Value"));
                    }
                    EditorGUILayout.Space();
                    EditorGUILayout.PropertyField(HiddenName5, new GUIContent("Property Name"));
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.PropertyField(HiddenFloat5, new GUIContent("Animate Value"));
                        EditorGUILayout.PropertyField(valueBack5, new GUIContent("Default Value"));
                    }
                    EditorGUILayout.Space();
                    EditorGUILayout.PropertyField(HiddenName6, new GUIContent("Property Name"));
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.PropertyField(HiddenFloat6, new GUIContent("Animate Value"));
                        EditorGUILayout.PropertyField(valueBack6, new GUIContent("Default Value"));
                    }
                    break;
                default:
                    EditorGUILayout.LabelField("Input number to edit property (1~7)");
                    break;
            }
            serializedObject.ApplyModifiedProperties();
        }
    }
#endif
}
