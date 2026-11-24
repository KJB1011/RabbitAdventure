using DG.Tweening;
using TMPro;
using UnityEngine;

/// <summary>
/// 연습방의 허수아비. 죽지 않고, 맞은 피해로 DPS(초당 피해)를 계산해서 머리 위에 보여준다.
/// 일정 시간(10초) 동안 맞지 않으면 기록을 초기화한다.
/// 적(EnemyBase)으로 취급되므로 기본기·특수기·필살기·강화 기술·아티팩트가 모두 실제 전투처럼 들어간다.
/// </summary>
public class TrainingDummy : EnemyController
{
    [Header("허수아비")]
    [SerializeField] TextMeshPro _dpsText;       // 머리 위 글씨 (월드 공간)
    [SerializeField] float _resetAfter = 10f;    // 이만큼 안 맞으면 초기화
    [SerializeField] float _minDuration = 1f;    // 첫 타 직후 DPS가 과하게 튀지 않도록 최소 측정 시간

    float _totalDamage;
    float _firstHitTime;
    float _lastHitTime;
    bool _isMeasuring;

    protected override void Awake()
    {
        base.Awake();
        InitEnemy(int.MaxValue); // 체력은 의미 없음 (깎지 않는다)
        ShowIdle();
    }

    void OnEnable() => ResetRecord();

    /// <summary>기록 초기화</summary>
    public void ResetRecord()
    {
        _isMeasuring = false;
        _totalDamage = 0f;
        ShowIdle();
    }

    // 체력을 깎지 않고, 피해만 기록한다 (EnemyBase.DamageTaken 대신)
    public override void DamageTaken(int damage)
    {
        if (damage <= 0) return;

        float now = Time.time;
        if (!_isMeasuring)
        {
            _isMeasuring = true;
            _totalDamage = 0f;
            _firstHitTime = now;
        }
        _totalDamage += damage;
        _lastHitTime = now;
        UpdateText();

        // 맞을 때마다 살짝 흔들흔들
        if (CharSprite != null)
        {
            var t = CharSprite.transform;
            t.DOKill(true);
            t.DOPunchRotation(new Vector3(0f, 0f, 8f), 0.25f, 8, 0.6f);
        }
    }

    void Update()
    {
        if (!_isMeasuring) return;

        if (Time.time - _lastHitTime > _resetAfter)
            ResetRecord();
        else
            UpdateText(); // 안 때리는 동안에도 시간이 흐르므로 DPS가 서서히 내려간다
    }

    void UpdateText()
    {
        if (_dpsText == null) return;
        float duration = Mathf.Max(_minDuration, Time.time - _firstHitTime);
        float dps = _totalDamage / duration;
        _dpsText.text = $"DPS {dps:N0}\n<size=70%>총 {_totalDamage:N0} · {duration:0.0}초</size>";
    }

    void ShowIdle()
    {
        if (_dpsText != null) _dpsText.text = "DPS -\n<size=70%>때려서 측정</size>";
    }

    // 허수아비는 쓰러지지 않는다
    protected override int ClampHp(int hp) => _totalHp;
}
