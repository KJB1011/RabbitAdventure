using UnityEngine;
using UnityEngine.UI;
using static Defines;

/// <summary>
/// 플레이어를 따라다니며 스킬의 쿨다운을 알려주는 스크립트
/// </summary>
public class UIPlayerSkill : MonoBehaviour
{
    [SerializeField] PlayerController _targetPlayer; // 코드에서 자동으로 받아오게 수정
    [SerializeField] Transform _mainSkill;
    [SerializeField] Transform _subSkill;
    [SerializeField] Transform _superSkill;
    [SerializeField] Transform _specialSkill;

    Image _mainShadow;
    Image _subShadow;
    Image _superShadow;
    Image _specialShadow;

    Image _mainOutline;
    Image _subOutline;
    Image _superOutline;
    Image _specialOutline;

    // Child(0) - Outline, Child(1) - Icon, Child(2) - CooldownShadow

    void Start()
    {
        _mainShadow = _mainSkill.GetChild(2).GetComponent<Image>();
        _subShadow = _subSkill.GetChild(2).GetComponent<Image>();
        _superShadow = _superSkill.GetChild(2).GetComponent<Image>();
        _specialShadow = _specialSkill.GetChild(2).GetComponent<Image>();

        _mainOutline = _mainSkill.GetChild(0).GetComponent<Image>();
        _subOutline = _subSkill.GetChild(0).GetComponent<Image>();
        _superOutline = _superSkill.GetChild(0).GetComponent<Image>();
        _specialOutline = _specialSkill.GetChild(0).GetComponent<Image>();
    }
    public void SetCoolDown(SKILL type, float amount)
    {
        switch ((int)type)
        {
            case 0:
                _mainShadow.fillAmount = amount;
                _mainOutline.enabled = amount <= 0f;
                break;
            case 1:
                _subShadow.fillAmount = amount;
                _subOutline.enabled = amount <= 0f;
                break;
            case 2:
                _superShadow.fillAmount = amount;
                _superOutline.enabled = amount <= 0f;
                break;
            case 3:
                _specialShadow.fillAmount = amount;
                _specialOutline.enabled = amount <= 0f;
                break;
        }

    }
}
