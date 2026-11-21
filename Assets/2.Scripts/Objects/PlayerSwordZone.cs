using DG.Tweening;
using System.Collections;
using UnityEngine;

/// <summary>
/// 특수기 강화(검 꽂기): 바닥에 꽂힌 검. 일정 시간 동안 주변 적에게 반복해서 피해를 준다.
/// Resources/VFX/PlantedSword 프리팹에 붙어 있고, 꽂을 때마다 새로 만들고 끝나면 지운다.
/// (VFXManager 풀은 3초 뒤 자동으로 거둬 가서, 4초 동안 남아야 하는 이 오브젝트에는 쓰지 않는다)
/// </summary>
public class PlayerSwordZone : MonoBehaviour
{
    [SerializeField] SpriteRenderer _sword;   // 꽂힌 검
    [SerializeField] SpriteRenderer _ring;    // 범위 표시 원 (반경 1짜리 그림을 크기로 맞춘다)
    [SerializeField] Color _ringColor = new Color(1f, 0.85f, 0.4f, 0.35f);

    PlayerSkillUpgrades _owner;   // 범위 피해(HitArea)를 대신 넣어 줄 플레이어 강화 컴포넌트
    float _radius, _duration, _interval, _damageRate, _extraRate;

    public void Begin(PlayerSkillUpgrades owner, float radius, float duration, float interval, float damageRate, float extraRate)
    {
        _owner = owner;
        _radius = radius;
        _duration = duration;
        _interval = interval;
        _damageRate = damageRate;
        _extraRate = extraRate;

        if (_ring != null)
        {
            _ring.transform.localScale = Vector3.one * radius;
            _ring.color = _ringColor;
        }
        if (_sword != null)
        {
            // 위에서 푹 꽂히는 연출
            var t = _sword.transform;
            Vector3 end = t.localPosition;
            t.localPosition = end + Vector3.up * 1.2f;
            t.DOLocalMove(end, 0.12f).SetEase(Ease.InQuad);
        }

        StartCoroutine(TickRoutine());
    }

    IEnumerator TickRoutine()
    {
        yield return new WaitForSeconds(0.12f); // 꽂히는 순간 첫 피해
        float elapsed = 0f;
        while (elapsed < _duration && _owner != null)
        {
            _owner.HitArea(transform.position, _radius, _damageRate, null, _extraRate);

            // 피해가 들어갈 때마다 원이 한 번 번쩍
            if (_ring != null)
            {
                _ring.DOKill();
                _ring.color = new Color(_ringColor.r, _ringColor.g, _ringColor.b, Mathf.Min(1f, _ringColor.a * 2.2f));
                _ring.DOColor(_ringColor, _interval * 0.8f);
            }

            yield return new WaitForSeconds(_interval);
            elapsed += _interval;
        }

        // 사라지기
        if (_ring != null) { _ring.DOKill(); _ring.DOFade(0f, 0.25f); }
        if (_sword != null) _sword.DOFade(0f, 0.25f);
        yield return new WaitForSeconds(0.3f);
        Destroy(gameObject);
    }

    void OnDestroy()
    {
        if (_ring != null) _ring.DOKill();
        if (_sword != null) { _sword.DOKill(); _sword.transform.DOKill(); }
    }
}
