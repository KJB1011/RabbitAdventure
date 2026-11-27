using DG.Tweening;
using UnityEngine;

/// <summary>
/// 총알 궤적: 두 점 사이를 잇는 가는 빛줄기가 잠깐 보였다가 사라진다.
/// 그림(가로 1유닛짜리 선)을 두 점 사이 길이만큼 늘리고 방향을 맞춘다.
/// VFXManager 풀에서 꺼낸 뒤 Show(from, to)를 부른다.
/// </summary>
public class TracerVFX : MonoBehaviour
{
    [SerializeField] SpriteRenderer _line;
    [SerializeField] float _thickness = 0.12f;
    [SerializeField] float _fadeTime = 0.12f;
    [SerializeField] Color _color = new Color(1f, 0.95f, 0.7f, 1f);

    public void Show(Vector2 from, Vector2 to) => Show(from, to, 1f, _color, 1f);

    /// <summary>굵기·색·남는 시간을 바꿔서 (차지샷처럼 강한 한 발)</summary>
    public void Show(Vector2 from, Vector2 to, float thicknessScale, Color color, float fadeScale)
    {
        Vector2 diff = to - from;
        float length = diff.magnitude;
        transform.position = (from + to) * 0.5f;
        transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg);
        transform.localScale = Vector3.one; // 풀에서 꺼낼 때 다른 크기가 남아 있지 않게

        if (_line == null) return;
        _line.transform.localScale = new Vector3(length, _thickness * thicknessScale, 1f);
        _line.DOKill();
        _line.color = color;
        _line.DOFade(0f, _fadeTime * fadeScale).SetEase(Ease.InQuad);
    }

    void OnDisable()
    {
        if (_line != null) _line.DOKill();
    }
}
