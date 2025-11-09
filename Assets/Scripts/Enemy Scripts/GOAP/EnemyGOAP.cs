using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(GoapAgent))]
public class EnemyGoap : MonoBehaviour, IDamagable, IEnemyMoveable, IEnemyRanges
{
    [Header("Health")]
    [field: SerializeField] public float maxHealth { get; set; } = 100f;
    public float currentHealth { get; set; }

    [Header("Ranges")]
    [SerializeField] private float aggroDistance = 5f;
    [SerializeField] private float attackDistance = 1.5f;
    [SerializeField] private float viewCircle = 8f;
    public float AggroDistance => aggroDistance;
    public float AttackDistance => attackDistance;
    public float ViewDistance => viewCircle;

    [Header("UI")]
    [SerializeField] private GameObject healthBarPrefab;
    private EnemyHealthBar healthBar;

    [Header("State Flags")]
    public bool isFacingRight { get; set; } = true;
    public bool IsAggroed { get; private set; }
    public bool IsWithinStrikingDistance { get; private set; }
    public bool CanMove { get; set; } = true;

    public Animator animator { get; private set; }
    public Rigidbody2D rb { get; set; }
    private Vector2 smoothedVelocity;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        currentHealth = maxHealth;

        if (healthBarPrefab != null)
        {
            GameObject bar = Instantiate(healthBarPrefab);
            healthBar = bar.GetComponent<EnemyHealthBar>();
            if (healthBar)
            {
                healthBar.Setup(transform, maxHealth);
                healthBar.UpdateHealth(currentHealth);
            }
        }
    }

    private void Update()
    {
        var agent = GetComponent<GoapAgent>();
        if (agent && agent.CurrentTarget)
        {
            float d = Vector2.Distance(transform.position, agent.CurrentTarget.position);
            IsAggroed = d < aggroDistance;
            IsWithinStrikingDistance = d < attackDistance;
        }
        else
        {
            IsAggroed = false;
            IsWithinStrikingDistance = false;
        }
    }

    public void moveEnemy(Vector2 velocity)
    {
        if (!CanMove)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        smoothedVelocity = Vector2.Lerp(smoothedVelocity, velocity, 0.2f);
        rb.linearVelocity = velocity;
        checkForLeftOrRightFacing(rb.linearVelocity);
    }

    public void checkForLeftOrRightFacing(Vector2 velocity)
    {
        if (Mathf.Abs(velocity.x) < 0.2f) return;

        if (velocity.x < 0f && isFacingRight)
        {
            transform.rotation = Quaternion.Euler(0, 180, 0);
            isFacingRight = false;
        }
        else if (velocity.x > 0f && !isFacingRight)
        {
            transform.rotation = Quaternion.Euler(0, 0, 0);
            isFacingRight = true;
        }
    }

    public void Damage(float damageAmount)
    {
        animator.SetBool("isHit", true);
        ResetIsHit();

        currentHealth = Mathf.Max(0f, currentHealth - damageAmount);
        if (healthBar) healthBar.UpdateHealth(currentHealth);

        if (currentHealth <= 0f)
            Die();
    }

    public void ResetIsHit() => animator.SetBool("isHit", false);

    public void Die()
    {
        if (healthBar) Destroy(healthBar.gameObject);
        Destroy(gameObject);
    }
}
