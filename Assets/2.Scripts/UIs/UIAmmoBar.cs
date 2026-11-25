using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 토끼 총잡이의 남은 총알 표시.
/// 플레이어를 따라다니는 스킬칸 바로 바깥쪽에 탄환을 세로로 늘어놓는다.
///   - 남은 탄환 : 황갈색
///   - 쓴 탄환   : 어두운 회색 (위에서부터 하나씩)
///   - 재장전 중 : 아래에서부터 흐린 황갈색으로 하나씩 차오른다
/// 탄창 크기가 바뀌면(쌍권총 12발) 같은 높이 안에 촘촘하게 다시 늘어놓는다.
/// 오브젝트·이미지는 코드에서 만든다 (RabbitGunnerSkills가 UIAmmoBar.Create로 생성).
/// </summary>
public class UIAmmoBar : MonoBehaviour
{
    static readonly Color FullColor = new Color(0.85f, 0.66f, 0.38f);      // 황갈색
    static readonly Color EmptyColor = new Color(0.27f, 0.27f, 0.29f);     // 어두운 회색
    static readonly Color ReloadColor = new Color(0.85f, 0.66f, 0.38f, 0.45f); // 재장전으로 차오르는 중

    const float ColumnHeight = 190f; // 스킬칸 4개 높이와 비슷하게 (월드 캔버스 단위)
    const float BulletWidth = 26f;
    const float BulletHeight = 14f;  // 탄환 두께 (12발일 때는 칸이 좁아서 자동으로 조금 줄어든다)
    const float SideOffset = 44f;    // 스킬칸 가운데에서 탄환 줄 가운데까지

    RectTransform _rect;
    Transform _player;
    Sprite _sprite;
    readonly List<Image> _bullets = new();

    /// <summary>스킬칸(UIPlayerSkill) 아래에 탄환 줄을 만든다</summary>
    public static UIAmmoBar Create(Transform skillColumn, Transform player)
    {
        var go = new GameObject("AmmoBar", typeof(RectTransform));
        go.layer = skillColumn.gameObject.layer;
        var rect = (RectTransform)go.transform;
        rect.SetParent(skillColumn, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(BulletWidth, ColumnHeight);
        rect.anchoredPosition = new Vector2(-SideOffset, 0f);

        var bar = go.AddComponent<UIAmmoBar>();
        bar._rect = rect;
        bar._player = player;
        bar._sprite = Resources.Load<Sprite>("UI/AmmoBullet");
        return bar;
    }

    /// <summary>
    /// 탄환 표시 갱신
    /// </summary>
    /// <param name="bullets">남은 총알</param>
    /// <param name="size">탄창 크기</param>
    /// <param name="reloadProgress">재장전 중이면 0~1, 아니면 음수</param>
    public void Set(int bullets, int size, float reloadProgress = -1f)
    {
        if (_bullets.Count != size) Rebuild(size);

        // 재장전 중이면 아래에서부터 진행도만큼 흐리게 차오름
        int reloaded = reloadProgress >= 0f ? Mathf.FloorToInt(reloadProgress * size) : 0;

        for (int i = 0; i < size; i++)
        {
            int fromBottom = size - 1 - i;   // i=0이 맨 위 → 위에서부터 비워진다
            Color c;
            if (fromBottom < bullets) c = FullColor;
            else if (fromBottom < reloaded) c = ReloadColor;
            else c = EmptyColor;
            _bullets[i].color = c;
        }
    }

    void Rebuild(int size)
    {
        foreach (var b in _bullets) if (b != null) Destroy(b.gameObject);
        _bullets.Clear();

        float slot = ColumnHeight / Mathf.Max(1, size);
        float h = Mathf.Min(BulletHeight, slot * 0.8f);
        for (int i = 0; i < size; i++)
        {
            var go = new GameObject("Bullet" + i, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.layer = gameObject.layer;
            var rt = (RectTransform)go.transform;
            rt.SetParent(_rect, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(BulletWidth, h);
            rt.anchoredPosition = new Vector2(0f, -slot * (i + 0.5f));

            var img = go.GetComponent<Image>();
            img.sprite = _sprite;
            img.raycastTarget = false;
            _bullets.Add(img);
        }
    }

    // 스킬칸은 플레이어가 바라보는 반대쪽에 붙는다 → 탄환 줄은 스킬칸의 더 바깥쪽에
    void LateUpdate()
    {
        if (_player == null || _rect == null || _rect.parent == null) return;
        float side = _rect.parent.position.x < _player.position.x ? -1f : 1f; // -1: 스킬칸이 왼쪽 (오른쪽을 보는 중)
        _rect.anchoredPosition = new Vector2(SideOffset * side, 0f);
        _rect.localScale = new Vector3(-side, 1f, 1f); // 탄두가 바라보는 쪽을 향하게
    }
}
