using DG.Tweening;
using UnityEngine;

/// <summary>
/// 그림 한 장으로 만드는 베기 이펙트 (찌르기, 초승달 베기).
/// 켜질 때마다 자식 그림을 "커지면서 + 살짝 돌면서 + 흐려지게" 움직인다.
/// 크기·회전은 자식에 주기 때문에, 부모(루트)의 크기는 공격 범위에 맞춰 자유롭게 바꿔도 된다.
/// (VFXManager의 풀에서 꺼낼 때마다 OnEnable이 불려서 처음부터 다시 재생된다)
/// </summary>
public class SpriteSlashVFX : MonoBehaviour
{
    [SerializeField] SpriteRenderer _sprite;

    [Header("크기 (자식 기준)")]
    [SerializeField] Vector3 _fromScale = new Vector3(0.3f, 1f, 1f);
    [SerializeField] Vector3 _toScale = Vector3.one;
    [SerializeField] float _scaleTime = 0.08f;

    [Header("회전 (휘두르는 느낌, 도)")]
    [SerializeField] float _fromAngle = 0f;
    [SerializeField] float _toAngle = 0f;

    [Header("이동 (앞으로 나아가는 느낌, 자식 기준 / +X = 공격 방향)")]
    [SerializeField] Vector3 _fromOffset = Vector3.zero;
    [SerializeField] Vector3 _toOffset = Vector3.zero;

    [Header("사라짐")]
    [SerializeField] Color _color = Color.white;
    [SerializeField] float _fadeDelay = 0.05f;
    [SerializeField] float _fadeTime = 0.2f;

    void OnEnable()
    {
        if (_sprite == null) return;
        var t = _sprite.transform;

        t.DOKill();
        _sprite.DOKill();

        t.localScale = _fromScale;
        t.localRotation = Quaternion.Euler(0f, 0f, _fromAngle);
        t.localPosition = _fromOffset;
        _sprite.color = _color;

        t.DOScale(_toScale, _scaleTime).SetEase(Ease.OutCubic);
        t.DOLocalRotate(new Vector3(0f, 0f, _toAngle), _scaleTime + _fadeDelay).SetEase(Ease.OutCubic);
        t.DOLocalMove(_toOffset, _scaleTime + _fadeDelay + _fadeTime * 0.5f).SetEase(Ease.OutCubic); // 사라지면서도 조금 더 나아간다
        _sprite.DOFade(0f, _fadeTime).SetDelay(_fadeDelay).SetEase(Ease.InQuad);
    }

    void OnDisable()
    {
        if (_sprite == null) return;
        _sprite.transform.DOKill();
        _sprite.DOKill();
    }
}
