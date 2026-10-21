using UnityEngine;

/// <summary>
/// 아티팩트 데이터 + 효과 생성.
/// 새 아티팩트는 이 클래스를 상속한 SO 하나만 추가하면 됩니다.
/// </summary>
public abstract class ArtifactSO : ScriptableObject
{
    public string artifactName;
    [TextArea] public string description;
    public Sprite icon;
    public int price;

    public abstract ArtifactEffect CreateEffect();
}