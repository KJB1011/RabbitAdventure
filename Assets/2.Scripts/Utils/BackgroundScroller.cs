using DG.Tweening;
using UnityEngine;

/// <summary>
/// 배경(BGShader)의 흐르는 속도를 잠깐 빠르게 만든다. (노드 이동 연출용)
///
/// BGShader는 "시간 × 속도(_BGSpeed)"로 배경을 흘려보낸다.
/// 이때 _BGSpeed를 직접 바꾸면 (시간 × 바뀐 속도)가 되어 배경 위치가 순간적으로 튀므로,
/// 속도는 그대로 두고 "추가로 더 흘러간 거리(_BGOffset)"를 매 프레임 쌓아서 넘긴다.
/// → BGShader에 _BGOffset 속성을 더해 주는 작업이 필요하다.
/// </summary>
public class BackgroundScroller : MonoBehaviour
{
    [SerializeField] Renderer[] _layers;            // 배경 레이어들 (BGShader 머티리얼을 쓰는 렌더러)
    [SerializeField] float _boostMultiplier = 40f;  // 빨라질 때 원래 속도의 몇 배를 "추가로" 흐를지

    static readonly int SpeedId = Shader.PropertyToID("_BGSpeed");
    static readonly int OffsetId = Shader.PropertyToID("_BGOffset");

    float[] _baseSpeeds;   // 레이어별 원래 속도 (앞 배경은 빠르고 뒤 배경은 느림 → 그 비율을 유지)
    float[] _offsets;      // 레이어별로 추가로 흘러간 거리
    float _boost;          // 0 = 평소, 1 = 최고 속도
    Tween _boostTween;
    MaterialPropertyBlock _block;

    void Awake()
    {
        _block = new MaterialPropertyBlock();
        _baseSpeeds = new float[_layers.Length];
        _offsets = new float[_layers.Length];

        for (int i = 0; i < _layers.Length; i++)
        {
            var mat = _layers[i] != null ? _layers[i].sharedMaterial : null;
            if (mat != null && mat.HasProperty(SpeedId))
                _baseSpeeds[i] = mat.GetFloat(SpeedId);
        }
    }

    /// <summary>배경을 빠르게 흐르게 한다 (time초에 걸쳐 가속).</summary>
    public void StartBoost(float time = 0.3f) => TweenBoost(1f, time, Ease.InQuad);

    /// <summary>원래 속도로 되돌린다 (time초에 걸쳐 감속).</summary>
    public void StopBoost(float time = 0.6f) => TweenBoost(0f, time, Ease.OutQuad);

    void TweenBoost(float target, float time, Ease ease)
    {
        _boostTween?.Kill();
        _boostTween = DOTween.To(() => _boost, v => _boost = v, target, time).SetEase(ease);
    }

    void Update()
    {
        if (_boost <= 0f) return; // 평소에는 아무것도 하지 않는다 (셰이더가 알아서 흐름)

        for (int i = 0; i < _layers.Length; i++)
        {
            if (_layers[i] == null) continue;

            // 레이어마다 자기 원래 속도에 비례해서 더 흐르게 → 앞뒤 배경의 깊이감(패럴랙스) 유지
            _offsets[i] += _baseSpeeds[i] * _boostMultiplier * _boost * Time.deltaTime;

            // 머티리얼 에셋은 건드리지 않고 이 렌더러에만 값을 넘긴다
            _layers[i].GetPropertyBlock(_block);
            _block.SetFloat(OffsetId, _offsets[i]);
            _layers[i].SetPropertyBlock(_block);
        }
    }
}
