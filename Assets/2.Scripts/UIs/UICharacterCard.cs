using DG.Tweening;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 캐릭터 선택 창의 카드 한 장 (초상화, 이름, 짧은 소개).
/// 지금 고른 캐릭터는 테두리가 금색, 아직 없는 칸은 "준비 중"으로 흐리게 보인다.
/// </summary>
[RequireComponent(typeof(Button))]
public class UICharacterCard : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] Image _background;
    [SerializeField] Image _portrait;
    [SerializeField] TMP_Text _nameText;
    [SerializeField] TMP_Text _descText;     // 캐릭터 소개 한두 줄 (스킬 정보는 게임 안에서 Tab 패널·툴팁으로)
    [SerializeField] GameObject _selectedMark;      // "선택 중" 표시

    [SerializeField] Color _normalColor = new Color(0.232f, 0.232f, 0.358f, 1f);
    [SerializeField] Color _selectedColor = new Color(0.45f, 0.36f, 0.16f, 1f);
    [SerializeField] Color _lockedColor = new Color(0.16f, 0.16f, 0.2f, 1f);

    CharacterSO _data;
    Action<CharacterSO> _onClick;
    Button _button;
    bool _locked;

    void Awake()
    {
        _button = GetComponent<Button>();
        _button.onClick.AddListener(() => { if (!_locked) _onClick?.Invoke(_data); });
    }

    public void Setup(CharacterSO data, bool isSelected, Action<CharacterSO> onClick)
    {
        _data = data;
        _onClick = onClick;
        _locked = false;
        if (_button != null) _button.interactable = true;

        if (_portrait != null)
        {
            _portrait.enabled = data.portrait != null;
            _portrait.sprite = data.portrait;
            _portrait.color = Color.white;
            _portrait.preserveAspect = true;
        }
        if (_nameText != null) _nameText.text = data.displayName;
        if (_descText != null) _descText.text = data.description;
        if (_background != null) _background.color = isSelected ? _selectedColor : _normalColor;
        if (_selectedMark != null) _selectedMark.SetActive(isSelected);
        transform.localScale = Vector3.one;
    }

    public void SetupLocked()
    {
        _data = null;
        _locked = true;
        if (_button != null) _button.interactable = false;
        if (_portrait != null) _portrait.enabled = false;
        if (_nameText != null) _nameText.text = "준비 중";
        if (_descText != null) _descText.text = "새로운 캐릭터가\n곧 찾아옵니다.";
        if (_background != null) _background.color = _lockedColor;
        if (_selectedMark != null) _selectedMark.SetActive(false);
        transform.localScale = Vector3.one;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_locked) return;
        transform.DOKill();
        transform.DOScale(1.04f, 0.12f).SetUpdate(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        transform.DOKill();
        transform.DOScale(1f, 0.12f).SetUpdate(true);
    }

    void OnDisable() => transform.DOKill();
}
