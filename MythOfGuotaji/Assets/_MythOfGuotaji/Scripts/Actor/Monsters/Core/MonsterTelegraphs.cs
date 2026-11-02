using System.Collections;
using UnityEngine;

namespace MythOfGuotaji.Actor
{
    /// <summary>
    /// 몬스터 공격 범위 예고(Telegraph) 및 시각 이펙트 생성 유틸리티
    /// </summary>
    public static class MonsterTelegraphs
    {
                public static GameObject CreateCircleTelegraph(Vector3 center, float radius, Color color)
        {
            GameObject telegraphObj = new GameObject("[CircleTelegraph]");
            telegraphObj.transform.position = new Vector3(center.x, 0.05f, center.z);
            telegraphObj.transform.rotation = Quaternion.identity;

            GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cylinder.name = "TelegraphArea";
            cylinder.transform.SetParent(telegraphObj.transform, false);

            cylinder.transform.localPosition = Vector3.zero;
            cylinder.transform.localRotation = Quaternion.identity;
            cylinder.transform.localScale = new Vector3(radius * 2f, 0.01f, radius * 2f);

            var col = cylinder.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);

            var rend = cylinder.GetComponent<Renderer>();
            if (rend != null)
            {
                Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit")
                                  ?? Shader.Find("Sprites/Default")
                                  ?? Shader.Find("Legacy Shaders/Transparent/Diffuse");

                Material mat = new Material(unlitShader);
                mat.color = color;
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
                mat.renderQueue = 3100;
                rend.material = mat;
            }

            return telegraphObj;
        }

        public static GameObject CreateFloorGuideBox(Vector3 startPos, Vector3 dir, float length, float width, Color color)
        {
            GameObject telegraphObj = new GameObject("[FloorGuideBox]");
            telegraphObj.transform.position = startPos + Vector3.up * 0.05f;
            telegraphObj.transform.rotation = Quaternion.LookRotation(dir);

            GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            plane.name = "FloorArea";
            plane.transform.SetParent(telegraphObj.transform, false);

            plane.transform.localPosition = new Vector3(0f, 0f, length * 0.5f);
            plane.transform.localRotation = Quaternion.identity;
            plane.transform.localScale = new Vector3(width * 0.1f, 1f, length * 0.1f);

            var col = plane.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);

            var rend = plane.GetComponent<Renderer>();
            if (rend != null)
            {
                Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit");
                if (unlitShader == null) unlitShader = Shader.Find("Sprites/Default");
                if (unlitShader == null) unlitShader = Shader.Find("Legacy Shaders/Transparent/Diffuse");

                Material mat = new Material(unlitShader);
                mat.color = color;
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);

                mat.SetFloat("_Surface", 1f);
                mat.SetFloat("_Blend", 0f);
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.renderQueue = 3000;

                rend.material = mat;
            }

            CreateTelegraphBorderLine(telegraphObj.transform, new Vector3(-width * 0.5f, 0.01f, length * 0.5f), new Vector3(0.08f, 0.02f, length), color);
            CreateTelegraphBorderLine(telegraphObj.transform, new Vector3(width * 0.5f, 0.01f, length * 0.5f), new Vector3(0.08f, 0.02f, length), color);
            CreateTelegraphBorderLine(telegraphObj.transform, new Vector3(0f, 0.01f, length), new Vector3(width, 0.02f, 0.08f), color);

            return telegraphObj;
        }

        public static void CreateTelegraphBorderLine(Transform parent, Vector3 localPos, Vector3 localScale, Color color)
        {
            GameObject border = GameObject.CreatePrimitive(PrimitiveType.Cube);
            border.name = "BorderLine";
            border.transform.SetParent(parent, false);
            border.transform.localPosition = localPos;
            border.transform.localRotation = Quaternion.identity;
            border.transform.localScale = localScale;

            var col = border.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);

            var rend = border.GetComponent<Renderer>();
            if (rend != null)
            {
                Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit");
                if (unlitShader == null) unlitShader = Shader.Find("Sprites/Default");

                Color borderColor = new Color(color.r, color.g, color.b, Mathf.Min(1.0f, color.a * 2.0f + 0.3f));
                Material mat = new Material(unlitShader);
                mat.color = borderColor;
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", borderColor);

                rend.material = mat;
            }
        }

        public static GameObject CreateWorldSectorTelegraph(Vector3 center, Vector3 forward, float radius, float arcAngle, Color color)
        {
            GameObject sectorObj = new GameObject("[SectorTelegraph]");
            center.y = 0.05f;
            sectorObj.transform.position = center;
            forward.y = 0f;
            sectorObj.transform.rotation = Quaternion.LookRotation(forward.normalized);

            int segments = 24;
            Mesh mesh = new Mesh();
            mesh.name = "SectorMesh";

            Vector3[] vertices = new Vector3[segments + 2];
            int[] triangles = new int[segments * 3];

            vertices[0] = Vector3.zero;
            float startAngle = -arcAngle * 0.5f;
            float angleStep = arcAngle / segments;

            for (int i = 0; i <= segments; i++)
            {
                float currentAngle = startAngle + (angleStep * i);
                Quaternion rot = Quaternion.Euler(0f, currentAngle, 0f);
                vertices[i + 1] = rot * Vector3.forward * radius;
            }

            for (int i = 0; i < segments; i++)
            {
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = i + 2;
            }

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();

            var mf = sectorObj.AddComponent<MeshFilter>();
            mf.mesh = mesh;
            var mr = sectorObj.AddComponent<MeshRenderer>();

            Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit");
            if (unlitShader == null) unlitShader = Shader.Find("Sprites/Default");

            Material mat = new Material(unlitShader);
            mat.color = color;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);

            mat.SetFloat("_Surface", 1f);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.renderQueue = 3000;

            mr.material = mat;
            return sectorObj;
        }

        public static GameObject CreateDashTelegraph(Vector3 startPos, Vector3 dashDir, float length, float width)
        {
            var telegraph = CreateFloorGuideBox(startPos, dashDir, length, width, new Color(1f, 0.25f, 0.1f, 0.35f));
            telegraph.name = "[DashTelegraph]";
            return telegraph;
        }

        public static GameObject CreateSweepTelegraph(Vector3 center, Vector3 forward, float radius, float arcAngle)
        {
            return CreateWorldSectorTelegraph(center, forward, radius, arcAngle, new Color(1f, 0.3f, 0.1f, 0.4f));
        }

        public static void SpawnSweepVisualEffect(Vector3 center, Vector3 forward, float radius)
        {
            GameObject fxObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            fxObj.name = "SweepSlashFX";
            center.y = 0.6f;
            fxObj.transform.position = center + forward.normalized * (radius * 0.5f);
            fxObj.transform.localScale = new Vector3(radius * 1.2f, 0.05f, radius * 1.2f);

            var col = fxObj.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);

            var rend = fxObj.GetComponent<Renderer>();
            if (rend != null)
            {
                Shader unlit = Shader.Find("Universal Render Pipeline/Unlit");
                if (unlit == null) unlit = Shader.Find("Sprites/Default");
                Material mat = new Material(unlit);
                mat.color = new Color(1f, 0.35f, 0.1f, 0.8f);
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", new Color(1f, 0.35f, 0.1f, 0.8f));
                rend.material = mat;
            }
            Object.Destroy(fxObj, 0.18f);
        }

        public static void SpawnShockwaveVisualEffect(Vector3 center, float radius)
        {
            GameObject fxObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            fxObj.name = "Shockwave_GroundRing";
            center.y = 0.05f;
            fxObj.transform.position = center;
            fxObj.transform.localScale = new Vector3(0.5f, 0.02f, 0.5f);

            var col = fxObj.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);

            var rend = fxObj.GetComponent<Renderer>();
            if (rend != null)
            {
                Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit")
                                  ?? Shader.Find("Sprites/Default")
                                  ?? Shader.Find("Legacy Shaders/Transparent/Diffuse");
                Material mat = new Material(unlitShader);
                Color ringColor = new Color(1.0f, 0.4f, 0.1f, 0.85f);
                mat.color = ringColor;
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", ringColor);

                mat.SetFloat("_Surface", 1f);
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.renderQueue = 3000;
                rend.material = mat;
            }

            var animator = fxObj.AddComponent<ShockwaveRingAnimator>();
            animator.Initialize(radius, 0.45f);

            // 안전장치: 컴포넌트 이상 발생 시에도 0.5초 후 반드시 삭제
            Object.Destroy(fxObj, 0.5f);
        }

        public static void SpawnVacuumPullEffect(Vector3 center, Vector3 forward, float radius)
        {
            GameObject fxObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            fxObj.name = "VacuumPullFX";
            center.y = 0.1f;
            fxObj.transform.position = center + forward * (radius * 0.4f);
            fxObj.transform.localScale = new Vector3(radius * 1.5f, 0.05f, radius * 1.5f);

            var col = fxObj.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);

            var rend = fxObj.GetComponent<Renderer>();
            if (rend != null)
            {
                Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit");
                if (unlitShader == null) unlitShader = Shader.Find("Sprites/Default");
                Material mat = new Material(unlitShader);
                Color c = new Color(0.6f, 0.1f, 0.9f, 0.6f);
                mat.color = c;
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
                rend.material = mat;
            }
            Object.Destroy(fxObj, 0.25f);
        }
    }

    /// <summary>
    /// 충격파 링 이펙트의 확산 및 페이드아웃 애니메이션 컴포넌트
    /// </summary>
    public class ShockwaveRingAnimator : MonoBehaviour
    {
        private float _duration = 0.4f;
        private float _elapsed = 0f;
        private Vector3 _startScale;
        private Vector3 _targetScale;
        private Material _mat;
        private Color _baseColor;

        public void Initialize(float targetRadius, float duration = 0.45f)
        {
            _duration = Mathf.Max(0.1f, duration);
            _startScale = new Vector3(0.5f, 0.02f, 0.5f);
            _targetScale = new Vector3(targetRadius * 2f, 0.02f, targetRadius * 2f);

            var rend = GetComponent<Renderer>();
            if (rend != null)
            {
                _mat = rend.material;
                _baseColor = _mat.color;
            }
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_elapsed / _duration);

            // Ease out: 충격파가 빠르게 퍼지며 점차 감속
            float scaleT = Mathf.Sin(t * Mathf.PI * 0.5f);
            transform.localScale = Vector3.Lerp(_startScale, _targetScale, scaleT);

            if (_mat != null)
            {
                float alpha = Mathf.Lerp(_baseColor.a, 0f, t * t);
                Color c = new Color(_baseColor.r, _baseColor.g, _baseColor.b, alpha);
                _mat.color = c;
                if (_mat.HasProperty("_BaseColor")) _mat.SetColor("_BaseColor", c);
            }

            if (_elapsed >= _duration)
            {
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            if (_mat != null)
            {
                Destroy(_mat);
                _mat = null;
            }
        }
    }

}
