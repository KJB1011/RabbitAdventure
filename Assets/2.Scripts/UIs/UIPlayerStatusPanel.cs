using DG.Tweening;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 화면 왼쪽 아래 플레이어 상태 패널 (1P 기준, 화면 가로의 1/4 크기).
///
/// 접혔을 때: 이름 / 아티팩트 / 스킬 아이콘(쿨타임)
/// 펼쳤을 때: 이름 / HP·공격력·공격범위·치명타 / 아티팩트 / 스킬 아이콘(쿨타임)
/// Tab을 누를 때마다 펼침 ↔ 접힘. 아티팩트·스킬 아이콘에 마우스를 올리면 설명이 뜬다.
///
/// 멀티플레이를 고려해 플레이어 한 명당 패널 하나. 2P~4P는 이 패널을 복제해서 위치(앵커)만 바꾸면 된다.
/// </summary>
public class UIPlayerStatusPanel : MonoBehaviour
{
    [Header("대상 플레이어")]
    [SerializeField] PlayerController _player; // 비워두면 "Player" 태그로 자동으로 찾는다 (1P용)

    [Header("항상 보이는 부분")]
    [SerializeField] TextMeshProUGUI _nameText;
    [SerializeField] Transform _artifactRow;                 // 아티팩트 아이콘들이 들어갈 곳 (Horizontal Layout Group)
    [SerializeField] UITooltipTrigger _artifactSlotPrefab;   // 아티팩트 아이콘 한 칸 (Image + UITooltipTrigger)
    [Tooltip("순서: 기본공격, 대쉬, Super, Special. 각 스킬 아이콘 위에 덮는 쿨타임 그림자 (Image Type: Filled)")]
    [SerializeField] Image[] _skillCooldowns = new Image[4];
    [Tooltip("순서 동일. 스킬 아이콘 가운데의 남은 쿨타임 숫자 (Raycast Target 끄기).\n" +
             "컴포넌트 ⋮ 메뉴 → '쿨타임 숫자 자동 연결'로 한 번에 채울 수 있다")]
    [SerializeField] TextMeshProUGUI[] _skillCooldownTexts = new TextMeshProUGUI[4];

    [Header("펼쳐지는 부분 (스탯)")]
    [SerializeField] LayoutElement _statsWrapper;    // 높이를 0 ↔ 내용 높이로 바꿔서 펼치고 접는다 (Rect Mask 2D로 넘치는 부분을 가림)
    [SerializeField] RectTransform _statsContent;    // 스탯 글씨들이 들어 있는 곳 (Vertical Layout Group)
    [SerializeField] TextMeshProUGUI _hpText;
    [SerializeField] TextMeshProUGUI _atkText;
    [SerializeField] TextMeshProUGUI _rangeText;
    [SerializeField] TextMeshProUGUI _critText;

    [Header("펼치기 설정")]
    [SerializeField] Key _toggleKey = Key.Tab;
    [SerializeField] float _toggleTime = 0.25f;

    bool _isExpanded;
    Tween _toggleTween;
    readonly List<UITooltipTrigger> _artifactSlots = new();

    // 스탯이 바뀌었을 때만 글씨를 다시 만든다 (매 프레임 문자열을 만들면 메모리 쓰레기가 생김)
    int _lastHp = -1, _lastMaxHp = -1;
    float _lastAtk = -1f, _lastRange = -1f, _lastCrit = -1f;
    readonly int[] _lastCooldownSec = { -1, -1, -1, -1 }; // 쿨타임 숫자도 바뀔 때만 갱신

    void Start()
    {
        SetExpanded(false, instant: true);
        // 에디터에서는 "3"처럼 미리보기 숫자를 넣어 디자인을 맞춰도 되고, 시작할 때 숨긴다
        foreach (var text in _skillCooldownTexts)
            if (text != null) text.enabled = false;

        if (ArtifactManager.Instance != null)
            ArtifactManager.Instance.OnArtifactsChanged += RefreshArtifacts;
        RefreshArtifacts();
    }

    void OnDestroy()
    {
        if (ArtifactManager.Instance != null)
            ArtifactManager.Instance.OnArtifactsChanged -= RefreshArtifacts;
    }

    void Update()
    {
        // 플레이어는 게임이 시작된 뒤에 생성되므로, 찾을 때까지 기다린다
        if (_player == null && !TryFindPlayer()) return;

        // Tab: 펼침 ↔ 접힘 (전투 중에는 열 수 없고, 열려 있었다면 접는다)
        bool inBattle = IngameManager.Instance != null && IngameManager.Instance._isBattle;
        if (inBattle)
        {
            if (_isExpanded) SetExpanded(false);
        }
        else if (Keyboard.current != null && Keyboard.current[_toggleKey].wasPressedThisFrame)
        {
            SetExpanded(!_isExpanded);
        }

        // 쿨타임 그림자 + 남은 시간 숫자
        SetCooldown(0, _player.MainSkillCooldownRatio, _player.MainSkillCooldownLeft);
        SetCooldown(1, _player.SubSkillCooldownRatio, _player.SubSkillCooldownLeft);
        SetCooldown(2, _player.SuperSkillCooldownRatio, _player.SuperSkillCooldownLeft);
        SetCooldown(3, _player.SpecialSkillCooldownRatio, _player.SpecialSkillCooldownLeft);

        if (_isExpanded)
            RefreshStats(force: false);
    }

    bool TryFindPlayer()
    {
        var go = GameObject.FindWithTag("Player");
        if (go == null) return false;

        _player = go.GetComponent<PlayerController>();
        if (_player == null) return false;

        if (_nameText != null && RememberManager.Instance != null)
            _nameText.text = RememberManager.Instance._nickname;
        return true;
    }

    void SetCooldown(int index, float ratio, float secondsLeft)
    {
        // 그림자: 숫자만 바꾸므로 매 프레임 해도 부담 없음
        if (index < _skillCooldowns.Length && _skillCooldowns[index] != null)
            _skillCooldowns[index].fillAmount = ratio; // 1 = 막 사용함(가득 가림), 0 = 사용 가능

        // 숫자: 소수점 없이 올림 (0.3초 남아도 "1" → 완전히 0이 되는 순간 사라짐)
        if (index >= _skillCooldownTexts.Length || _skillCooldownTexts[index] == null) return;

        int sec = Mathf.CeilToInt(secondsLeft);
        if (sec == _lastCooldownSec[index]) return; // 숫자가 바뀔 때만 갱신
        _lastCooldownSec[index] = sec;

        var text = _skillCooldownTexts[index];
        text.enabled = sec > 0;
        if (sec > 0)
            text.SetText("{0}", sec); // SetText에 숫자를 넘기면 새 문자열을 만들지 않는다 (메모리 쓰레기 없음)
    }

    // ── 에디터 도우미 ─────────────────────────────────────────
    // Inspector에서 컴포넌트 이름 옆 ⋮(또는 우클릭) 메뉴에 나타난다. 실행하지 않아도 에디터에서 바로 쓸 수 있다.
    // 각 쿨타임 그림자의 부모(= 스킬 아이콘) 아래에 있는 TMP를 찾아 순서대로 칸을 채운다.
    [ContextMenu("쿨타임 숫자 자동 연결")]
    void AutoAssignCooldownTexts()
    {
        if (_skillCooldownTexts == null || _skillCooldownTexts.Length != _skillCooldowns.Length)
            _skillCooldownTexts = new TextMeshProUGUI[_skillCooldowns.Length];

        for (int i = 0; i < _skillCooldowns.Length; i++)
        {
            if (_skillCooldowns[i] == null) continue;
            _skillCooldownTexts[i] = _skillCooldowns[i].transform.parent.GetComponentInChildren<TextMeshProUGUI>(true);
            if (_skillCooldownTexts[i] == null)
                Debug.LogWarning($"[StatusPanel] {_skillCooldowns[i].transform.parent.name} 아래에 TMP가 없습니다", this);
        }

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this); // 바뀐 값을 씬에 저장되게 표시 (안 하면 Ctrl+S 때 빠질 수 있음)
#endif
    }

    // ── 펼치기 / 접기 ─────────────────────────────────────────

    public void SetExpanded(bool expand, bool instant = false)
    {
        _isExpanded = expand;
        if (_statsWrapper == null || _statsContent == null) return;

        float targetHeight = 0f;
        if (expand)
        {
            RefreshStats(force: true);
            // 글씨를 채운 뒤 내용의 실제 높이를 바로 계산해서 목표 높이로 쓴다
            LayoutRebuilder.ForceRebuildLayoutImmediate(_statsContent);
            targetHeight = LayoutUtility.GetPreferredHeight(_statsContent);
        }

        _toggleTween?.Kill();
        if (instant)
        {
            _statsWrapper.preferredHeight = targetHeight;
            return;
        }

        // 높이를 부드럽게 바꾼다 → 패널 전체가 위로 "튀어나오듯" 늘어난다
        // (게임이 멈춘 상태에서도 열 수 있게 SetUpdate(true))
        _toggleTween = DOTween.To(() => _statsWrapper.preferredHeight,
                                  h => _statsWrapper.preferredHeight = h,
                                  targetHeight, _toggleTime)
                              .SetEase(expand ? Ease.OutBack : Ease.InCubic)
                              .SetUpdate(true);
    }

    // ── 스탯 ─────────────────────────────────────────────────

    void RefreshStats(bool force)
    {
        if (_player == null) return;

        if (force || _player.CurrentHp != _lastHp || _player.MaxHp != _lastMaxHp)
        {
            _lastHp = _player.CurrentHp;
            _lastMaxHp = _player.MaxHp;
            if (_hpText != null) _hpText.text = $"HP : {_lastHp} / {_lastMaxHp}";
        }
        if (force || !Mathf.Approximately(_player.Atk, _lastAtk))
        {
            _lastAtk = _player.Atk;
            if (_atkText != null) _atkText.text = $"공격력 : {_lastAtk:F0}";
        }
        if (force || !Mathf.Approximately(_player.RangeMultiplier, _lastRange))
        {
            _lastRange = _player.RangeMultiplier;
            if (_rangeText != null) _rangeText.text = $"공격범위 : {_lastRange:F1}";
        }
        if (force || !Mathf.Approximately(_player.CritRate, _lastCrit))
        {
            _lastCrit = _player.CritRate;
            if (_critText != null) _critText.text = $"크리티컬 확률 : {_lastCrit:F0}%";
        }
    }

    // ── 아티팩트 ─────────────────────────────────────────────

    // 보유 아티팩트가 바뀔 때만 호출된다 (ArtifactManager의 알림)
    void RefreshArtifacts()
    {
        if (_artifactRow == null || _artifactSlotPrefab == null) return;

        IReadOnlyList<ArtifactSO> owned = ArtifactManager.Instance != null
            ? ArtifactManager.Instance.OwnedArtifacts
            : null;
        int count = owned != null ? Mathf.Min(owned.Count, ArtifactManager.Instance.MaxArtifacts) : 0; // 칸 수까지만 표시

        // 칸이 모자라면 새로 만든다 (만들어 둔 칸은 지우지 않고 재사용)
        while (_artifactSlots.Count < count)
            _artifactSlots.Add(Instantiate(_artifactSlotPrefab, _artifactRow));

        for (int i = 0; i < _artifactSlots.Count; i++)
        {
            var slot = _artifactSlots[i];
            bool used = i < count;
            slot.gameObject.SetActive(used);
            if (!used) continue;

            ArtifactSO data = owned[i];
            if (slot.TryGetComponent(out Image icon))
                icon.sprite = data.icon;
            slot.Set(data.artifactName, data.description);
        }
    }
}
