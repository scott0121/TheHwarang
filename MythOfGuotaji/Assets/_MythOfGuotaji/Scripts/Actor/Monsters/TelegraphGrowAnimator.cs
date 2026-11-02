using MythOfGuotaji.Data;
using UnityEngine;



public class TelegraphGrowAnimator : MonoBehaviour

{

    private Renderer _renderer;

    private Material _instanceMat;

    private int _colorId;

    private Color _targetColor;

    private Vector3 _baseScale;

    private float _duration;

    private float _elapsed;



    public void Begin(Renderer renderer, float duration, Color targetColor)

    {

        _renderer = renderer;

        _duration = Mathf.Max(0.01f, duration);

        _targetColor = targetColor;

        _baseScale = transform.localScale;



        // 공유 머티리얼의 알파를 바꾸면 다른 텔레그래프에도 영향이 가므로 인스턴스를 따로 만든다

        _instanceMat = new Material(renderer.sharedMaterial);

        renderer.sharedMaterial = _instanceMat;

        _colorId = _instanceMat.HasProperty("_BaseColor") ? Shader.PropertyToID("_BaseColor") : Shader.PropertyToID("_Color");



        Apply(0f);

    }



    private void Update()

    {

        _elapsed += Time.deltaTime;

        float t = Mathf.Clamp01(_elapsed / _duration);

        Apply(t);

        if (t >= 1f) enabled = false;

    }



    private void Apply(float t)

    {

        transform.localScale = _baseScale * t;

        Color c = _targetColor;

        c.a = Mathf.Lerp(0f, _targetColor.a, t);

        _instanceMat.SetColor(_colorId, c);

    }



    private void OnDestroy()

    {

        if (_instanceMat != null) Destroy(_instanceMat);

    }

}