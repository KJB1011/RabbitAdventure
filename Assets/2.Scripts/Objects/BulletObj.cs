using UnityEngine;

/// <summary>
/// 탄막 자동소멸 스크립트
/// </summary>
public class BulletObj : MonoBehaviour
{
    [SerializeField] float _duration = 5f;
    void Start()
    {
        Destroy(gameObject, _duration);
    }

}
