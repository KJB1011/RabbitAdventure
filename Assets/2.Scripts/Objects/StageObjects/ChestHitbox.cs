using UnityEngine;
using static Defines;

/// <summary>
/// Chest만을 위한 Hitbox
/// </summary>
public class ChestHitbox : MonoBehaviour
{
    [SerializeField] private ChestObject owner;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("PlayerAttack")) return;

        var player = other.GetComponentInParent<PlayerController>();
        if (player == null) return;

        int damage = player.DamageReturn();
        owner.TakeDamage(damage);

        VFXManager.Instance.Play("FinishHit", transform.position);
    }
}