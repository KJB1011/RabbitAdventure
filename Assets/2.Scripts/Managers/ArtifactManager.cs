using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어가 보유한 아티팩트를 관리합니다.
/// 아티팩트 획득/제거와 효과 활성화/비활성화를 담당하며,
/// 효과의 코루틴 실행 주체 역할도 합니다.
/// </summary>
public class ArtifactManager : MonoBehaviour
{
    public static ArtifactManager Instance { get; private set; }

    private readonly Dictionary<ArtifactSO, ArtifactEffect> _activeEffects = new();
    private PlayerController _player;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void SetPlayer(PlayerController player)
    {
        _player = player;

        foreach (var effect in _activeEffects.Values)
        {
            effect.Deactivate();
            effect.Activate(_player, this);
        }
    }

    /// <summary>
    /// 아티팩트를 획득합니다. (상점 구매)
    /// </summary>
    public bool Acquire(ArtifactSO data)
    {
        if (data == null || _activeEffects.ContainsKey(data))
        {
            Debug.Log($"[Artifact] 획득 불가 (없음 또는 보유 중): {data?.artifactName}");
            return false;
        }

        var effect = data.CreateEffect();
        effect.Activate(_player, this);
        _activeEffects[data] = effect;

        Debug.Log($"[Artifact] 획득: {data.artifactName}");
        return true;
    }

    /// <summary>
    /// 아티팩트를 제거합니다.
    /// </summary>
    public void Remove(ArtifactSO data)
    {
        if (!_activeEffects.TryGetValue(data, out var effect)) return;

        effect.Deactivate();
        _activeEffects.Remove(data);
        Debug.Log($"[Artifact] 제거: {data.artifactName}");
    }

    public bool HasArtifact(ArtifactSO data) => _activeEffects.ContainsKey(data);
}