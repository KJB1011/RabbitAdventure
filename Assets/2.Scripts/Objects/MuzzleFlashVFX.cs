using DG.Tweening;
using UnityEngine;

/// <summary>
/// 총구 섬광: 총구 앞에 불꽃 그림이 "팍" 하고 커졌다가 아주 짧게(0.07초) 사라진다.
/// VFXManager 풀에서 꺼낸 뒤 Show(총구 위치, 쏘는 방향)를 부른다.
/// (예전에 쓰던 LightHitVFX는 원이 너무 오래 남아서 총 전용으로 따로 만듦)
/// </summary>
public class MuzzleFlashVFX : MonoBehaviour
{
    [SerializeField] SpriteRenderer _flash;   // 피벗이 왼쪽 가운데인 불꽃 그림 (오른쪽을 향함)
    [SerializeField] float _duration = 0.07f;
    [SerializeField] float _startScale = 0.7f;
    [SerializeField] float _endScale = 1.15f;

    void OnEnable()
    {
        // 풀에서 꺼낸 직후 Show 전에 예전 모습이 한 프레임 보이지 않게
        if (_flash != null)
        {
            _flash.DOKill();
            var c = _flash.color; c.a = 0f; _flash.color = c;
        }
    }

    public void Show(Vector2 muzzle, Vector2 dir)
    {
        transform.position = muzzle;
        transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
        if (_flash == null) return;

        // 매번 조금씩 다르게 (위아래 뒤집기 + 크기)
        _flash.flipY = Random.value < 0.5f;
        float s = Random.Range(0.9f, 1.1f);

        _flash.DOKill();
        _flash.transform.DOKill();
        _flash.color = Color.white;
        _flash.transform.localScale = Vector3.one * (_startScale * s);
        _flash.transform.DOScale(_endScale * s, _duration).SetEase(Ease.OutQuad);
        _flash.DOFade(0f, _duration).SetEase(Ease.InQuad);
    }

    void OnDisable()
    {
        if (_flash == null) return;
        _flash.DOKill();
        _flash.transform.DOKill();
    }
}
