using DG.Tweening;
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

    [Header("베기 선")]
    [SerializeField] RectTransform _slashLine;      // 베인 자국: 왼쪽 아래 → 오른쪽 위 방향의 긴 선 (Image, Pivot X = 0)
    [SerializeField] RectTransform _slashHead;      // 칼끝: 선을 따라 빠르게 날아가는 짧고 밝은 빛 (Image, Pivot X = 1)
    [SerializeField] float _slashDuration = 0.12f;  // 칼끝이 화면을 완전히 지나가는 시간 (짧을수록 빠름)
    Image _slashImage;                               // 선의 투명도 조절용 (_slashLine에서 자동으로 가져옴)

    [Header("파티클 (Canvas가 Screen Space - Camera여야 보임)")]
    [SerializeField] ParticleSystem _headSparks;     // ① 칼끝에서 뒤로 튀는 불꽃
    [SerializeField] ParticleSystem _lineGlitter;    // ② 베인 자국을 따라 피어오르는 빛
    [SerializeField] ParticleSystem _glassShards;    // ③ 화면이 깨질 때 튀는 유리 파편

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
        ResetSlash();
        gameObject.SetActive(true); // 연출용 캔버스 소환

        // 베는 순간: 묵직한 칼소리 + 날카로운 한 번 베기 소리를 겹쳐서 재생
        SoundManager.Instance.PlaySFX("SFX/Player_StrongSword");
        SoundManager.Instance.PlaySFX("SFX/Player_SwordOneSlash");
        Time.timeScale = 0f; // 연출동안 잠깐 게임 멈춰놓기

        // 칼로 베듯 선이 지나간 뒤
        yield return SlashRoutine();

        // 그 선을 따라 살짝 갈라지는 연출
        _topRect.anchoredPosition = TopDir * 10;
        _bottomRect.anchoredPosition = BottomDir * 10;

        // ② 베인 자국을 따라 빛이 피어오른다 (유리가 깨질 때까지)
        PlaceAlongSlash(_lineGlitter);
        PlayParticle(_lineGlitter);

        yield return new WaitForSecondsRealtime(_holdDuration);
        onDamage?.Invoke();
        yield return new WaitForSecondsRealtime(_closeDuration);

        // 갈라진 화면을 밖으로 밀어내는 연출
        SoundManager.Instance.PlaySFX("SFX/GlassBreaking");
        Time.timeScale = 1f;

        // ③ 갈라지는 경계선에서 유리 파편이 사방으로 튄다
        StopEmitting(_lineGlitter);
        PlaceAlongSlash(_glassShards); // 선이 사라지기 전에 위치를 읽어야 하므로 숨기기보다 먼저
        PlayParticle(_glassShards);

        // 베인 자국은 깨지는 순간 바로 사라진다 (그 자리에서 유리 파편이 튀어나옴)
        if (_slashLine != null) _slashLine.gameObject.SetActive(false);

        // 화면이 깨지는 순간 크게 흔든다
        // (다른 흔들림이 남아 있으면 ShakeCamera가 무시하므로, 혹시 모를 경우를 대비해 먼저 끝내고 시작)
        DOTween.Complete(IngameManager.CamShakeId);
        IngameManager.Instance.ShakeCamera(0.3f, 0.35f);

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
        ResetSlash();
        gameObject.SetActive(false);

        Destroy(screenshot);
        _screenshot = null;
    }

    // ── 베기 선 ───────────────────────────────────────────────
    // timeScale = 0 상태에서 진행되므로 unscaledDeltaTime으로 직접 시간을 잰다
    IEnumerator SlashRoutine()
    {
        if (_slashLine == null) yield break;

        _slashLine.gameObject.SetActive(true);
        SetSlashAlpha(1f);

        // 선의 시작점(왼쪽 아래 끝)과 진행 방향, 월드 기준 길이
        Vector3 start = _slashLine.position;
        Vector3 dir = _slashLine.right;
        float lineLength = _slashLine.rect.width * _slashLine.parent.lossyScale.x;

        // 칼끝은 선 끝을 지나 완전히 빠져나갈 때까지 이동한다
        float headLength = 0f;
        if (_slashHead != null)
        {
            _slashHead.gameObject.SetActive(true);
            _slashHead.rotation = _slashLine.rotation;
            headLength = _slashHead.rect.width * _slashHead.lossyScale.x;
        }
        float travel = lineLength + headLength;

        // ① 칼끝 불꽃: 선 시작점에서 출발 (World 공간이라 이미 튄 불꽃은 제자리에 남는다)
        if (_headSparks != null)
        {
            _headSparks.transform.SetPositionAndRotation(start, _slashLine.rotation);
            PlayParticle(_headSparks);
        }

        float elapsed = 0f;
        while (elapsed < _slashDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / _slashDuration);
            float dist = travel * t; // 일정한 속도로 이동 → "날아가는" 느낌

            // 칼끝: 선을 따라 이동 (Pivot이 오른쪽 끝이라, 밝은 끝이 앞장서고 꼬리가 뒤따른다)
            if (_slashHead != null)
                _slashHead.position = start + dir * dist;

            // 불꽃은 칼끝을 따라가되, 선 끝을 넘어가지 않게 (화면 밖에서 뿜어봐야 안 보임)
            if (_headSparks != null)
                _headSparks.transform.position = start + dir * Mathf.Min(dist, lineLength);

            // 베인 자국: 칼끝이 지나간 곳까지만 보이게
            _slashLine.localScale = new Vector3(Mathf.Clamp01(dist / lineLength), 1f, 1f);
            yield return null;
        }

        _slashLine.localScale = Vector3.one;
        if (_slashHead != null) _slashHead.gameObject.SetActive(false);
        StopEmitting(_headSparks); // 새 불꽃만 멈추고, 이미 튄 불꽃은 자연스럽게 사라지게 둔다
    }

    // 다음 사용을 위해 선을 숨기고 처음 상태로 되돌린다
    void ResetSlash()
    {
        if (_slashHead != null) _slashHead.gameObject.SetActive(false);

        ClearParticle(_headSparks);
        ClearParticle(_lineGlitter);
        ClearParticle(_glassShards);

        if (_slashLine == null) return;
        _slashLine.localScale = new Vector3(0f, 1f, 1f);
        SetSlashAlpha(1f);
        _slashLine.gameObject.SetActive(false);
    }

    void SetSlashAlpha(float alpha)
    {
        if (_slashLine == null) return;
        if (_slashImage == null) _slashImage = _slashLine.GetComponent<Image>();
        if (_slashImage == null) return;

        Color c = _slashImage.color;
        c.a = alpha;
        _slashImage.color = c;
    }

    // ── 파티클 ────────────────────────────────────────────────
    // 파티클을 베인 자국 위에 정확히 겹치게 놓는다 (선의 중앙, 같은 각도, 선 길이만큼 넓게)
    // Shape가 Edge라서 radius = 선 길이의 절반이면 선 전체에서 입자가 생긴다
    void PlaceAlongSlash(ParticleSystem ps)
    {
        if (ps == null || _slashLine == null) return;

        var corners = new Vector3[4];
        _slashLine.GetWorldCorners(corners); // 0: 왼쪽 아래, 1: 왼쪽 위, 2: 오른쪽 위, 3: 오른쪽 아래 (선 기준)
        Vector3 left = (corners[0] + corners[1]) * 0.5f;
        Vector3 right = (corners[2] + corners[3]) * 0.5f;

        ps.transform.SetPositionAndRotation((left + right) * 0.5f, _slashLine.rotation);
        var shape = ps.shape;
        shape.radius = Vector3.Distance(left, right) * 0.5f;
    }

    static void PlayParticle(ParticleSystem ps)
    {
        if (ps == null) return;
        ps.gameObject.SetActive(true);
        ps.Clear(true);
        ps.Play(true);
    }

    static void StopEmitting(ParticleSystem ps)
    {
        if (ps == null) return;
        ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }

    static void ClearParticle(ParticleSystem ps)
    {
        if (ps == null) return;
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    void OnDestroy()
    {
        if (_screenshot != null)
            Destroy(_screenshot);
    }
}