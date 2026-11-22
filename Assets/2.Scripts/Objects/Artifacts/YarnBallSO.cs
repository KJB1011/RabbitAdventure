using UnityEngine;
using static Defines;

[CreateAssetMenu(menuName = "Artifact/Yarn Ball", fileName = "YarnBall")]
public class YarnBallSO : ArtifactSO
{
    [SerializeField] int _hitsPerStack = 50;        // 이만큼 맞힐 때마다
    [SerializeField] float _rangePerStack = 0.05f;  // 공격범위 +5%
    [SerializeField] int _maxStacks = 6;            // 최대 +30%

    public override ArtifactEffect CreateEffect() => new Artifact_YarnBall(_hitsPerStack, _rangePerStack, _maxStacks);
}

/// <summary>
/// 털실 뭉치: 적을 맞힐수록 공격범위가 조금씩 늘어난다 (이번 게임 동안 유지, 성장형)
/// </summary>
public class Artifact_YarnBall : ArtifactEffect
{
    private readonly int _hitsPerStack;
    private readonly float _rangePerStack;
    private readonly int _maxStacks;

    private int _hitCount;
    private int _stacks;

    public Artifact_YarnBall(int hitsPerStack, float rangePerStack, int maxStacks)
    {
        _hitsPerStack = hitsPerStack;
        _rangePerStack = rangePerStack;
        _maxStacks = maxStacks;
    }

    protected override void OnActivate()
    {
        Player.OnAttackHit += HandleHit;
        Player.AddRange(_rangePerStack * _stacks); // 플레이어가 바뀌어 다시 연결될 때도 쌓인 만큼 유지
    }

    protected override void OnDeactivate()
    {
        Player.OnAttackHit -= HandleHit;
        Player.AddRange(-_rangePerStack * _stacks);
    }

    private void HandleHit(EnemyBase enemy, AttackType type)
    {
        if (_stacks >= _maxStacks) return;

        _hitCount++;
        if (_hitCount < _hitsPerStack) return;

        _hitCount = 0;
        _stacks++;
        Player.AddRange(_rangePerStack);
        FloatingTextManager.Instance.ShowItemEffect($"공격범위 +{_rangePerStack * 100f:0}%",
                                                    new Color(0.95f, 0.55f, 0.7f), Player.transform.position);
    }
}
