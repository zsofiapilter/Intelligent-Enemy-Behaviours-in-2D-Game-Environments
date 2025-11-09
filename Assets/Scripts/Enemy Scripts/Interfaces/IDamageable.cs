using UnityEngine;

public interface IDamagable
{
    void Damage(float damageAmount);

    void Die();

    float maxHealth { get; set; }
    float currentHealth { get; set; }
}
