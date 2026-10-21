using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SpecialSkillEffect : MonoBehaviour
{
    [Header("스냅샷 패널")]
    [SerializeField] RectTransform _topRect;      // 위쪽으로 갈라질 패널
    [SerializeField] RawImage _topImage;      // 위쪽 이미지
    [SerializeField] RectTransform _bottomRect;   // 아래쪽으로 갈라질 패널
    [SerializeField] RawImage _bottomImage;   // 아래쪽 이미지

    [Header("연출 설정")]
    [SerializeField] float _splitDuration = 0.3f;   // 갈라지는 시간
    [SerializeField] float _holdDuration = 0.5f;   // 갈라진 채로 유지
    [SerializeField] float _closeDuration = 0.2f;   // 닫히는 시간
    [SerializeField] float _splitDistance = 600f;   // 갈라지는 거리 (px)

    // 이동 방향 - 위 패널은 좌상단, 아래 패널은 우하단
    static readonly Vector2 TopDir = new Vector2(-3f, 1f).normalized;
    static readonly Vector2 BottomDir = new Vector2(3f, -1f).normalized;

    Texture2D _screenshot;
    public void Close()
    {
        gameObject.SetActive(false);
    }

    public IEnumerator PlayRoutine(Texture2D screenshot, System.Action onDamage)
    {
        _screenshot = screenshot;
        _topImage.texture = screenshot;
        _bottomImage.texture = screenshot;

        _topRect.anchoredPosition = Vector2.zero;
        _bottomRect.anchoredPosition = Vector2.zero;
        gameObject.SetActive(true); // 연출용 캔버스 소환

        SoundManager.Instance.PlaySFX("SFX/StrongSword");
        Time.timeScale = 0f; // 연출동안 잠깐 게임 멈춰놓기

        // 살짝 갈라지는 연출
        _topRect.anchoredPosition = TopDir * 10;
        _bottomRect.anchoredPosition = BottomDir * 10;

        yield return new WaitForSecondsRealtime(_holdDuration);
        onDamage?.Invoke();
        yield return new WaitForSecondsRealtime(_closeDuration);

        // 갈라진 화면을 밖으로 밀어내는 연출
        SoundManager.Instance.PlaySFX("SFX/GlassBreaking");
        Time.timeScale = 1f;
        float elapsed = 0f;
        while (elapsed < _splitDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / _splitDuration));
            _topRect.anchoredPosition = TopDir * (_splitDistance * t);
            _bottomRect.anchoredPosition = BottomDir * (_splitDistance * t);
            yield return null;
        }

        // 벚꽃잎 VFX가 잘 보이게 시간 벌어주기
        yield return new WaitForSecondsRealtime(0.5f);

        // 초기화
        _topRect.anchoredPosition = Vector2.zero;
        _bottomRect.anchoredPosition = Vector2.zero;
        gameObject.SetActive(false);

        Destroy(screenshot);
        _screenshot = null;
    }

    void OnDestroy()
    {
        if (_screenshot != null)
            Destroy(_screenshot);
    }
}