using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 부채꼴 공격 전조 표시 (할퀴기 등).
/// 부채꼴 모양 이미지는 실행 중에 코드로 만들어 각도별로 재사용한다 (따로 그림 파일이 필요 없음).
/// 꼭짓점이 오브젝트 위치, 방향은 오른쪽(+X)이 기준이며 Scale = 반지름.
/// </summary>
public class SectorTelegraph : MonoBehaviour
{
    const int TexSize = 128;   // 부채꼴 이미지 해상도
    static readonly Dictionary<int, Sprite> _spriteCache = new(); // 각도 → 이미지

    static readonly Color StartColor = new Color(1f, 0.3f, 0.3f, 0.25f);
    static readonly Color FlashColor = new Color(1f, 0.3f, 0.3f, 0.6f);
    const float PulseDuration = 0.25f;

    SpriteRenderer _renderer;
    Tween _pulseTween;

    /// <summary>표시용 오브젝트 생성. 머티리얼·그리는 순서는 기존 경고선(template)과 같게 맞춘다</summary>
    public static SectorTelegraph Create(Transform parent, SpriteRenderer template)
    {
        var go = new GameObject("SectorTelegraph");
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<SectorTelegraph>();
        t._renderer = go.AddComponent<SpriteRenderer>();
        if (template != null)
        {
            t._renderer.sharedMaterial = template.sharedMaterial;
            t._renderer.sortingLayerID = template.sortingLayerID;
            t._renderer.sortingOrder = template.sortingOrder;
        }
        go.SetActive(false);
        return t;
    }

    /// <param name="apex">부채꼴의 꼭짓점 (공격하는 쪽 위치)</param>
    /// <param name="angle">부채꼴 전체 각도 (도)</param>
    public void Show(Vector2 apex, Vector2 direction, float radius, float angle)
    {
        _renderer.sprite = GetSprite(angle);
        transform.position = apex;
        transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        transform.localScale = new Vector3(radius, radius, 1f);
        gameObject.SetActive(true);

        _renderer.color = StartColor;
        _pulseTween?.Kill();
        _pulseTween = _renderer.DOColor(FlashColor, PulseDuration).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
    }

    public void Hide()
    {
        _pulseTween?.Kill();
        gameObject.SetActive(false);
    }

    void OnDestroy() => _pulseTween?.Kill();

    // 반지름 1(유닛)짜리 부채꼴 이미지. 중심이 꼭짓점, 오른쪽을 향함. 테두리는 조금 더 진하게
    static Sprite GetSprite(float angle)
    {
        int key = Mathf.RoundToInt(angle);
        if (_spriteCache.TryGetValue(key, out var cached) && cached != null) return cached;

        var tex = new Texture2D(TexSize, TexSize, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var pixels = new Color32[TexSize * TexSize];
        float half = key * 0.5f;
        float r = TexSize * 0.5f;

        for (int y = 0; y < TexSize; y++)
            for (int x = 0; x < TexSize; x++)
            {
                float dx = x + 0.5f - r, dy = y + 0.5f - r;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Abs(Mathf.Atan2(dy, dx) * Mathf.Rad2Deg);
                byte alpha = 0;
                if (dist <= r - 1 && a <= half)
                {
                    bool rim = dist >= r - 4 || a >= half - 3f / Mathf.Max(dist, 1f) * Mathf.Rad2Deg; // 바깥 호 + 양쪽 변 (약 3픽셀)
                    alpha = rim ? (byte)255 : (byte)150;
                }
                pixels[y * TexSize + x] = new Color32(255, 255, 255, alpha);
            }

        tex.SetPixels32(pixels);
        tex.Apply();
        var sprite = Sprite.Create(tex, new Rect(0, 0, TexSize, TexSize), new Vector2(0.5f, 0.5f), r);
        _spriteCache[key] = sprite;
        return sprite;
    }
}
