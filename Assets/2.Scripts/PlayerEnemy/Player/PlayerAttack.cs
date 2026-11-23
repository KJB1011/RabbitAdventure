using System.Collections;
using UnityEngine;
using System;
using static Defines;

/// <summary>
/// Player의 기본기, 특수기의 범위 및 연출을 담당하는 스크립트
/// 기본기는 숙련도 강화에 따라 모양이 바뀐다 (기본 / 찌르기 / 3연타 / 초승달 베기)
/// </summary>
public class PlayerAttack : MonoBehaviour
{
    /// <summary>기본기 모양 (숙련도 강화로 바뀜)</summary>
    public enum BasicStyle { Normal, Thrust, TripleSlash, Crescent, SwordWave }

    float _rangeMultiplier = 1.0f;

    [SerializeField] float _baseAttackDistance = 1.2f;
    [SerializeField] float _baseSuperDistance = 2.5f;

    [Header("기본기")]
    [SerializeField] BoxCollider2D _attackArea;
    [SerializeField] Transform _attackAreaPos;
    [SerializeField] float _basicActiveTime = 0.1f;

    [Header("특수기")]
    [SerializeField] BoxCollider2D _attackArea2;
    [SerializeField] Transform _attackAreaPos2;
    [SerializeField] float _superRangeMultiplier = 4f;
    [SerializeField] float _superActiveTime = 0.1f;

    [Header("기본기 강화: 찌르기")]
    [SerializeField] Vector2 _thrustSize = new Vector2(2.8f, 0.8f);  // 좁고 길게
    [SerializeField] float _thrustDistance = 1.6f;
    [SerializeField] float _thrustDamageRate = 0.8f;

    [Header("기본기 강화: 3연타")]
    [SerializeField] float _tripleDamageRate = 0.5f;        // 1·2타
    [SerializeField] float _tripleGap = 0.12f;              // 1타 → 2타 간격 (스슥)
    [SerializeField] float _tripleFinishDelay = 0.3f;       // 2타 → 3타 간격 (-삭!)
    [SerializeField] float _tripleFinishScale = 1.6f;       // 3타 범위 배율
    [SerializeField] float _tripleFinishDamageRate = 1.3f;  // 3타

    [Header("기본기 강화: 초승달 베기")]
    [SerializeField] Vector2 _crescentSize = new Vector2(3.6f, 4.4f); // x = 정면 깊이, y = 좌우 폭
    [SerializeField] float _crescentDistance = 2.1f;                  // 판정 중심 (0.3 ~ 3.9 거리를 벤다 = 이펙트가 밀고 나간 끝까지)
    [SerializeField] float _crescentDamageRate = 2.5f;

    [Header("기본기 강화: 검기 베기")]
    [SerializeField] float _waveRange = 4f;          // 날아가는 거리
    [SerializeField] float _waveSpeed = 16f;
    [SerializeField] float _waveRadius = 0.6f;       // 검기 판정 반경
    [SerializeField] float _waveDamageRate = 0.7f;   // 70% (관통)

    Vector2 _defaultColSize;
    Vector2 _defaultColSize2;
    BasicStyle _basicStyle = BasicStyle.Normal;
    PlayerController _owner;

    // 적중한 콜라이더가 어떤 공격인지 반환 (아티팩트 발동 조건 구분용)
    public AttackType GetAttackType(Collider2D col)
    => col == _attackArea2 ? AttackType.Super : AttackType.Basic;

    void Awake()
    {
        _attackArea.enabled = false;
        _attackArea2.enabled = false;
        _defaultColSize = new Vector2(2f, 1.5f);
        _defaultColSize2 = new Vector2(5f, 4.5f);
        _owner = GetComponentInParent<PlayerController>();
    }
    public void SetAttackRange(float multiplier) => _rangeMultiplier = multiplier;

    /// <summary>마지막으로 휘두른 공격이 닿는 가장 먼 거리 (플레이어 중심부터 판정 앞쪽 끝까지). 칼끝 일격 판정용</summary>
    public float LastReach { get; private set; }
    public void SetBasicStyle(BasicStyle style) => _basicStyle = style;


    /// <summary>
    /// 노드 전환 시 진행 중인 공격을 즉시 중단합니다.
    /// </summary>
    public void ForceStop()
    {
        StopAllCoroutines();
        _attackArea.enabled = false;
        _attackArea2.enabled = false;
        if (_owner != null) _owner.SetBasicHitRate(1f);
    }
    // ── 기본기 ─────────────────────────────────────────────
    public void AttackStart(Action onComplete)
    {
        StartCoroutine(CoBasicAttack(onComplete));
    }
    IEnumerator CoBasicAttack(Action onComplete)
    {
        float m = _rangeMultiplier;

        switch (_basicStyle)
        {
            // 소리는 같은 칼 소리라도 음높이(pitch)를 바꿔서 기술마다 다르게 들리게 한다
            case BasicStyle.Thrust:
                yield return Swing(_thrustDistance * m, _thrustSize * m, _thrustDamageRate,
                                   "ThrustVFX", m, "SFX/Player_Sword", 1.35f);   // 높고 날카롭게
                break;

            case BasicStyle.TripleSlash:
                // 스슥 (1·2타: 기본 범위, 낮은 피해. 두 번째가 조금 더 높게)
                yield return Swing(_baseAttackDistance * m, _defaultColSize * m, _tripleDamageRate,
                                   "SwordVFX", m, "SFX/Player_Sword", 1.1f);
                yield return new WaitForSeconds(_tripleGap);
                yield return Swing(_baseAttackDistance * m, _defaultColSize * m, _tripleDamageRate,
                                   "SwordVFX", m, "SFX/Player_Sword", 1.22f);
                // -삭! (3타: 넓은 범위, 높은 피해. 커진 만큼 살짝 앞에서 벤다)
                yield return new WaitForSeconds(_tripleFinishDelay);
                float s = _tripleFinishScale;
                yield return Swing(_baseAttackDistance * m * (1f + (s - 1f) * 0.6f), _defaultColSize * m * s,
                                   _tripleFinishDamageRate, "SwordVFX", m * s, "SFX/Player_SwordOneSlash", 1f);
                break;

            case BasicStyle.Crescent:
                yield return Swing(_crescentDistance * m, _crescentSize * m, _crescentDamageRate,
                                   "CrescentVFX", m, "SFX/Player_StrongSword", 0.85f); // 묵직하게
                break;

            case BasicStyle.SwordWave:
                // 근접 판정 없이 검기만 날린다 (판정은 PlayerProjectile이 직접)
                SoundManager.Instance.PlaySFXPitch("SFX/Player_SwordOneSlash", 1.25f);
                var wave = VFXManager.Instance.Play("SwordWaveVFX", transform.position + transform.right * (0.6f * m), transform.rotation);
                if (wave != null && wave.TryGetComponent(out PlayerProjectile proj))
                {
                    wave.transform.localScale = Vector3.one * m;
                    proj.Launch(_owner, transform.right, _waveSpeed, _waveRange * m, _waveRadius * m, _waveDamageRate);
                }
                yield return new WaitForSeconds(_basicActiveTime);
                break;

            default:
                yield return Swing(_baseAttackDistance * m, _defaultColSize * m, 1f,
                                   "SwordVFX", m, "SFX/Player_Sword", UnityEngine.Random.Range(0.95f, 1.05f)); // 매번 살짝 다르게
                break;
        }

        onComplete?.Invoke();
    }

    // 한 번 베기: 판정을 잠깐 켜고 이펙트·소리를 낸다. 켜져 있는 동안의 데미지 배율을 플레이어에게 알려준다
    IEnumerator Swing(float distance, Vector2 size, float damageRate, string vfxId, float vfxScale, string sfx, float pitch)
    {
        if (_owner != null) _owner.SetBasicHitRate(damageRate);

        // 판정(Area1_col)은 위치가 고정이고 offset으로 앞으로 밀어 둔 구조라, 거리도 offset으로 맞춘다
        // (이펙트 위치용 _attackAreaPos와 같은 거리 → 이펙트와 판정이 겹친다)
        _attackAreaPos.position = transform.position + transform.right * distance;
        _attackArea.offset = new Vector2(distance, 0f);
        _attackArea.size = size;
        LastReach = distance + size.x * 0.5f;
        _attackArea.enabled = true; // 끄고 다시 켜면 같은 적도 다시 맞는다 (3연타)

        SoundManager.Instance.PlaySFXPitch(sfx, pitch);
        var vfx = VFXManager.Instance.Play(vfxId, (Vector2)_attackAreaPos.position, transform.rotation);
        if (vfx != null) vfx.transform.localScale = Vector3.one * vfxScale;

        yield return new WaitForSeconds(_basicActiveTime);
        _attackArea.enabled = false;
        if (_owner != null) _owner.SetBasicHitRate(1f);
    }

    // ── 특수기 ─────────────────────────────────────────────
    /// <param name="scale">범위 배율 (특수기 강화: 모아베기 단계, 받아치기 반격)</param>
    public void SuperAttackStart(Action onComplete, float scale = 1f)
    {
        StartCoroutine(CoSuperAttack(onComplete, scale));
    }

    IEnumerator CoSuperAttack(Action onComplete, float scale)
    {
        _attackArea2.enabled = true;

        float multiplier = _rangeMultiplier * scale;
        _attackAreaPos2.position = transform.position + transform.right * (_baseSuperDistance * multiplier);
        _attackArea2.offset = new Vector2(_baseSuperDistance * multiplier, 0f); // 범위가 커지면 판정도 앞으로
        _attackArea2.size = _defaultColSize2 * multiplier;
        LastReach = _baseSuperDistance * multiplier + _attackArea2.size.x * 0.5f;

        // 범위가 클수록(모아베기 3단계 등) 낮고 묵직하게
        SoundManager.Instance.PlaySFXPitch("SFX/Player_StrongSword", Mathf.Clamp(1f / Mathf.Pow(scale, 0.4f), 0.75f, 1.2f));
        var vfx = VFXManager.Instance.Play("SwordVFX2", (Vector2)_attackAreaPos2.position, transform.rotation);
        if (vfx != null) vfx.transform.localScale = Vector3.one * _superRangeMultiplier * scale;

        yield return new WaitForSeconds(_superActiveTime);
        _attackArea2.enabled = false;
        onComplete?.Invoke();
    }
}
