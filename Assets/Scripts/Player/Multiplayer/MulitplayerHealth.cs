using Mirror;
using UnityEngine;

public class MultiplayerHealth : NetworkBehaviour, IDamagable
{
    [field: SerializeField] public float maxHealth { get; set; } = 30f;

    [SyncVar(hook = nameof(OnHealthChanged))]
    private float _currentHealth;

    public float currentHealth
    {
        get => _currentHealth;
        set => _currentHealth = value;
    }

    public GameObject startPoint;
    public GameObject player;
    [SerializeField] private GameObject healthBarPrefab;

    private EnemyHealthBar healthBar;
    public Animator animator { get; private set; }

    private void Awake()
    {
        animator = GetComponentInChildren<Animator>();
    }

    public override void OnStartServer()
    {
        _currentHealth = maxHealth;
    }

    public override void OnStartClient()
    {
        if (healthBarPrefab != null && healthBar == null)
        {
            GameObject bar = Instantiate(healthBarPrefab);
            healthBar = bar.GetComponent<EnemyHealthBar>();
            healthBar.Setup(transform, maxHealth);
        }
        OnHealthChanged(0f, _currentHealth);
    }

    [Server]
    public void Damage(float damageAmount)
    {
        if (_currentHealth <= 0f) return;

        _currentHealth = Mathf.Max(0f, _currentHealth - damageAmount);

        RpcHitFX();

        if (_currentHealth <= 0f)
        {
            Die();
        }
    }

    [ClientRpc]
    private void RpcHitFX()
    {
        if (animator != null)
            animator.SetTrigger("isHit");
    }

    private void OnHealthChanged(float oldValue, float newValue)
    {
        if (healthBar != null)
            healthBar.UpdateHealth(newValue);
    }

    [Server]
    public void Die()
    {
        if (player != null && startPoint != null)
        {
            player.transform.position = startPoint.transform.position;
        }
        if (animator != null)
            animator.SetBool("isDead", true);

        _currentHealth = maxHealth;
        RpcDeathFX();
    }

    [ClientRpc]
    private void RpcDeathFX()
    {
        if (animator != null)
        {
            animator.SetBool("isDead", true);
            animator.SetBool("isDead", false);
        }
    }
}
