using System.Collections;
using UnityEngine;

/// <summary>
/// Enemy - 원형범위에 데미지를 넣는 공격
/// </summary>
public class AreaDamageZone : MonoBehaviour
{
    [Header("컴포넌트 참조")]
    [SerializeField] TelegraphIndicator _telegraph;

    [Header("연출")]
    [SerializeField] float _lingerDuration = 0.2f;

    System.Action _onComplete;

    public void Fire(Vector3 pos, Vector2 size, float telegraphDuration,
                     float angle = 0f, System.Action onComplete = null)
    {
        _onComplete = onComplete;
        gameObject.SetActive(true);
        StartCoroutine(FireRoutine(pos, size, telegraphDuration, angle));
        SoundManager.Instance.PlaySFX("SFX/Telegraph", SoundManager.Instance.SFXVolume, telegraphDuration);
    }

    private IEnumerator FireRoutine(Vector3 pos, Vector2 size,
                                     float telegraphDuration, float angle)
    {
        _telegraph.Show(pos, size, angle);
        yield return new WaitForSeconds(telegraphDuration);
        _telegraph.Hide();

        DealDamage(pos, size);
        VFXManager.Instance.Play("AreaHit", pos);
        SoundManager.Instance.PlaySFX("SFX/AreaDamage");

        yield return new WaitForSeconds(_lingerDuration);

        _onComplete?.Invoke();
    }

    private void DealDamage(Vector3 pos, Vector2 size)
    {
        float radius = size.x * 0.5f;

        Collider2D[] hits = Physics2D.OverlapCircleAll(pos, radius);

        foreach (var hit in hits)
        {
            var player = hit.GetComponentInParent<PlayerController>();
            if (player == null) continue;
            player.DamageTaken();
            break;
        }
    }

    // 진행 중인 공격을 즉시 중단 (시전자가 사망했을 때)
    public void Cancel()
    {
        StopAllCoroutines();
        _telegraph.Hide(); // 무한 루프 pulse tween Kill
        _onComplete = null;
        gameObject.SetActive(false);
    }
}