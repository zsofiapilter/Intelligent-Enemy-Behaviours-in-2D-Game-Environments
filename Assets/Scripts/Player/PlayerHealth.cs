using Unity.IO.LowLevel.Unsafe;
using UnityEngine;

public class PlayerHealth : MonoBehaviour, IDamagable
{
    [field: SerializeField] public float maxHealth { get; set; } = 30f;
    public float currentHealth { get; set; }

    public GameObject startPoint;
    public GameObject player;
    [SerializeField] private GameObject healthBarPrefab;

    private EnemyHealthBar healthBar;
    public Animator animator { get; set; }

    private void Start()
    {
        if (healthBarPrefab != null)
        {
            GameObject bar = Instantiate(healthBarPrefab);
            healthBar = bar.GetComponent<EnemyHealthBar>();
            healthBar.Setup(transform, maxHealth);
            healthBar.UpdateHealth(currentHealth);
        }
    }

    private void Awake()
    {
        currentHealth = maxHealth;
        animator = GetComponentInChildren<Animator>();
    }

    public void Damage(float damageAmount)
    {
        currentHealth -= damageAmount;

        if (animator != null)
            animator.SetTrigger("isHit");

        if (healthBar != null)
            healthBar.UpdateHealth(currentHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Die()
    {
        if (animator != null)
            animator.SetBool("isDead", true);

        if (healthBar != null)
        {
            Destroy(healthBar.gameObject);
            healthBar = null;
        }

        if (player != null && startPoint != null)
        {
            player.transform.position = startPoint.transform.position;
            currentHealth = maxHealth;

            if (healthBarPrefab != null)
            {
                GameObject bar = Instantiate(healthBarPrefab);
                healthBar = bar.GetComponent<EnemyHealthBar>();
                healthBar.Setup(transform, maxHealth);
                healthBar.UpdateHealth(currentHealth);
            }

            if (animator != null)
                animator.SetBool("isDead", false);
        }
    }
}
