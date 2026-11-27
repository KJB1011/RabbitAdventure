using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Defines;

/// <summary>
/// 스킬 숙련도를 관리한다.
/// 전투 중에 스킬을 쓸 때마다 숙련도가 오르고, 상점에서 스킬마다 한 번씩 구매해서 채울 수도 있다.
/// 가득 차면 강화 3개 중 하나를 고른다.
///   - 전투 중에 가득 참 → 전투가 끝나면 (IngameManager의 노드 전환 시작 시) 선택
///   - 상점에서 구매해서 가득 참 → 바로 선택
/// </summary>
public class SkillMasteryManager : MonoBehaviour
{
    public static SkillMasteryManager Instance { get; private set; }

    [Serializable]
    public class Track
    {
        [Tooltip("이 트랙을 쓰는 캐릭터 (CharacterSO.characterId). 비워 두면 토끼 검사(rabbit_sword)")]
        public string characterId;
        public SKILL skill;
        [Tooltip("가득 차는 데 필요한 숙련도 (전투 중 스킬 한 번 사용 = 1)")]
        public int required = 20;
        [Tooltip("가득 찼을 때 고를 수 있는 강화들 (3개)")]
        public SkillUpgradeSO[] upgrades;

        [NonSerialized] public int current;
        [NonSerialized] public bool purchased;        // 상점 구매는 스킬마다 한 번만
        [NonSerialized] public SkillUpgradeSO chosen; // 고른 강화 (고르기 전이면 null)

        public bool IsFull => current >= required;
        public bool IsPending => IsFull && chosen == null; // 가득 찼는데 아직 안 골랐다
    }

    [Tooltip("숙련도가 오르는 스킬들. 여기에 없는 스킬은 상점에서 '준비 중'으로 보인다")]
    [SerializeField] Track[] _tracks;

    [Header("상점 구매")]
    [Tooltip("상점에서 구매하면 숙련도를 필요량의 몇 %만큼 채울지 (1 = 한 번에 가득)")]
    [SerializeField, Range(0f, 1f)] float _purchaseFillRatio = 1f;

    [Header("선택 창")]
    [SerializeField] string _choiceTitleFormat = "{0} 강화를 하나 선택하세요";
    [Tooltip("강화 후보 중 몇 개를 보여줄지. 후보가 더 많으면 그중에서 무작위로 고른다")]
    [SerializeField] int _choiceCount = 3;

    /// <summary>숙련도가 바뀌거나 강화를 골랐을 때 (UI 갱신용)</summary>
    public event Action OnMasteryChanged;

    PlayerController _player;
    bool _isResolving; // 선택 창을 차례로 여는 중

    /// <summary>가득 찼는데 아직 강화를 고르지 않은 스킬이 있는지</summary>
    public bool HasPendingChoice
    {
        get
        {
            if (_tracks == null) return false;
            foreach (var track in _tracks)
                if (track.IsPending) return true;
            return false;
        }
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (_player != null) _player.OnSkillUsed -= HandleSkillUsed;
    }

    public void SetPlayer(PlayerController player)
    {
        if (_player != null) _player.OnSkillUsed -= HandleSkillUsed;
        _player = player;
        if (_player != null) _player.OnSkillUsed += HandleSkillUsed;
    }

    void HandleSkillUsed(SKILL skill) => AddPoints(skill, 1);

    // ── 숙련도 ───────────────────────────────────────────────

    /// <summary>
    /// 숙련도를 올린다. 기본적으로 전투 중에만 오른다. (이미 가득 찼거나 강화를 고른 스킬은 무시)
    /// </summary>
    /// <param name="ignoreBattleCheck">true면 전투 중이 아니어도 올린다 (상점 구매·디버그용)</param>
    public void AddPoints(SKILL skill, int amount, bool ignoreBattleCheck = false)
    {
        if (!ignoreBattleCheck && (IngameManager.Instance == null || !IngameManager.Instance._isBattle)) return;

        var track = GetTrack(skill);
        if (track == null || track.chosen != null || track.IsFull) return;

        track.current = Mathf.Min(track.current + amount, track.required);
        OnMasteryChanged?.Invoke();

        if (track.IsFull && _player != null && FloatingTextManager.Instance != null)
            FloatingTextManager.Instance.ShowItemEffect($"{SkillName(skill)} 숙련 완료!", new Color(1f, 0.85f, 0.3f), _player.transform.position);
    }

    /// <summary>0~1 (강화를 골랐으면 1)</summary>
    public float GetProgress(SKILL skill)
    {
        var track = GetTrack(skill);
        if (track == null) return 0f;
        if (track.chosen != null) return 1f;
        return track.required <= 0 ? 1f : (float)track.current / track.required;
    }

    public SkillUpgradeSO GetChosen(SKILL skill) => GetTrack(skill)?.chosen;

    /// <summary>가득 차는 데 필요한 숙련도 (숙련도가 없는 스킬은 0)</summary>
    public int GetRequired(SKILL skill) => GetTrack(skill)?.required ?? 0;

    /// <summary>숙련도 시스템이 있는 스킬인지 (없으면 상점에서 '준비 중')</summary>
    public bool HasTrack(SKILL skill) => GetTrack(skill) != null;

    // ── 상점 ─────────────────────────────────────────────────

    /// <summary>그 스킬을 아직 구매한 적이 없고, 숙련도가 덜 찼을 때만 살 수 있다</summary>
    public bool CanPurchase(SKILL skill)
    {
        var track = GetTrack(skill);
        return track != null && !track.purchased && track.chosen == null && !track.IsFull;
    }

    /// <summary>구매해서 숙련도를 채우고, 가득 찼으면 바로 강화를 고르게 한다</summary>
    public void Purchase(SKILL skill)
    {
        if (!CanPurchase(skill)) return;

        var track = GetTrack(skill);
        track.purchased = true;
        AddPoints(skill, Mathf.CeilToInt(track.required * _purchaseFillRatio), ignoreBattleCheck: true);
        TryResolveNow();
    }

    // ── 강화 선택 창 ─────────────────────────────────────────

    /// <summary>골라야 할 강화가 있으면 지금 바로 선택 창을 연다 (상점 구매·디버그용)</summary>
    public void TryResolveNow()
    {
        if (HasPendingChoice && !_isResolving)
            StartCoroutine(ResolvePendingChoices());
    }

    /// <summary>
    /// 골라야 할 강화를 하나씩 차례로 고르게 한다. 모두 고를 때까지 끝나지 않는다.
    /// (IngameManager가 전투 후 노드 전환을 시작할 때 이걸 기다린다)
    /// </summary>
    public IEnumerator ResolvePendingChoices()
    {
        if (_isResolving) yield break;
        _isResolving = true;

        foreach (var track in _tracks)
        {
            if (!track.IsPending) continue;

            // 다른 선택 창(상자 보상 등)이 열려 있으면 닫힐 때까지 기다린다
            yield return new WaitUntil(() => UIArtifactChoice.Instance == null || !UIArtifactChoice.Instance.IsOpen);

            bool closed = false;
            if (!OpenChoice(track, () => closed = true)) continue;
            yield return new WaitUntil(() => closed);
        }

        _isResolving = false;
    }

    bool OpenChoice(Track track, Action onClosed)
    {
        // 후보를 섞어서 앞에서부터 _choiceCount개만 보여준다 (4개 중 3개 → 판마다 선택지가 달라진다)
        var upgrades = new List<SkillUpgradeSO>();
        if (track.upgrades != null)
            foreach (var up in track.upgrades)
                if (up != null) upgrades.Add(up);

        for (int i = upgrades.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (upgrades[i], upgrades[j]) = (upgrades[j], upgrades[i]);
        }
        if (upgrades.Count > _choiceCount)
            upgrades.RemoveRange(_choiceCount, upgrades.Count - _choiceCount);

        var options = new List<UIArtifactChoice.Option>();
        foreach (var up in upgrades)
            options.Add(new UIArtifactChoice.Option(up.icon, up.upgradeName, up.description));

        if (options.Count == 0 || UIArtifactChoice.Instance == null)
        {
            Debug.LogWarning($"[SkillMastery] {track.skill}의 강화가 연결되지 않았거나 선택 창이 없습니다.");
            track.chosen = ScriptableObject.CreateInstance<SkillUpgradeSO>(); // 다시 열리지 않게
            return false;
        }

        string title = string.Format(_choiceTitleFormat, SkillName(track.skill));
        return UIArtifactChoice.Instance.TryOpenOptions(title, options, index =>
        {
            track.chosen = upgrades[index];
            if (_player != null) _player.ApplySkillUpgrade(upgrades[index].type);
            Debug.Log($"[SkillMastery] {track.skill} 강화: {upgrades[index].upgradeName}");
            OnMasteryChanged?.Invoke();
        }, onClosed);
    }

    const string DefaultCharacterId = "rabbit_sword";

    // 지금 캐릭터의 그 스킬 트랙 (그 캐릭터용 트랙이 없으면 null → 상점에서 "준비 중")
    Track GetTrack(SKILL skill)
    {
        if (_tracks == null) return null;
        string current = CurrentCharacterId;
        foreach (var track in _tracks)
        {
            string id = string.IsNullOrEmpty(track.characterId) ? DefaultCharacterId : track.characterId;
            if (track.skill == skill && id == current) return track;
        }
        return null;
    }

    static string CurrentCharacterId
    {
        get
        {
            var c = IngameManager.Instance != null ? IngameManager.Instance.CurrentCharacter : null;
            return c != null && !string.IsNullOrEmpty(c.characterId) ? c.characterId : DefaultCharacterId;
        }
    }

    public static string SkillName(SKILL skill) => skill switch
    {
        SKILL.MAIN => "기본기",
        SKILL.SUB => "회피기",
        SKILL.SUPER => "특수기",
        SKILL.SPECIAL => "필살기",
        _ => skill.ToString()
    };

}
