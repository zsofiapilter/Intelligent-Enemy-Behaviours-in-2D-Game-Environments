using UnityEngine;
using Mirror;

public class Enemy : MonoBehaviour, IDamagable, IEnemyMoveable, ITriggerCheckable, IEnemyRanges
{
    #region Serialized Fields

    [SerializeField] private GameObject healthBarPrefab;

    [field: SerializeField] public float maxHealth { get; set; } = 100f;
    [SerializeField] private float aggroDistance = 5f;
    [SerializeField] private float attackDistance = 1.5f;
    [SerializeField] private float viewCircle = 8f;

    public float AggroDistance => aggroDistance;
    public float AttackDistance => attackDistance;
    public float ViewDistance => viewCircle;


    [SerializeField] private EnemyIdleSOBase enemyIdleSOBase;
    [SerializeField] private EnemyAttackSOBase enemyAttackSOBase;
    [SerializeField] private EnemyChaseSOBase enemyChaseSOBase;

    #endregion

    #region Public Properties

    public float currentHealth { get; set; }
    public Rigidbody2D rb { get; set; }
    public bool isFacingRight { get; set; } = true;
    public bool IsAggroed { get; set; }
    public bool IsWithinStrikingDistance { get; set; }
    public Animator animator { get; set; }
    public Transform homePosition { get; set; }
    public bool IsGrounded { get; set; }
    public bool CanMove { get; set; } = true;

    #endregion

    #region State Machine Variables

    public EnemyStateMachine enemyStateMachine { get; set; }
    public EnemyIdleState idleState { get; set; }
    public EnemyAttackState attackState { get; set; }
    public EnemyChaseState chaseState { get; set; }

    private Transform playerTransform;
    private Vector2 smoothedVelocity;

    #endregion

    #region Scriptable Object Instances

    public EnemyIdleSOBase enemyIdleBaseInstance { get; set; }
    public EnemyAttackSOBase enemyAttackBaseInstance { get; set; }
    public EnemyChaseSOBase enemyChaseBaseInstance { get; set; }

    #endregion

    #region Private Variables

    private EnemyHealthBar healthBar;

    #endregion

    #region Multiplayer variables
    private bool IsNetworkingActive()
       => NetworkClient.active || NetworkServer.active;

    private bool ShouldRunAI()
        => !IsNetworkingActive() || NetworkServer.active;
    #endregion

    private void Awake()
    {
        enemyIdleBaseInstance = Instantiate(enemyIdleSOBase);
        enemyAttackBaseInstance = Instantiate(enemyAttackSOBase);
        enemyChaseBaseInstance = Instantiate(enemyChaseSOBase);

        enemyStateMachine = new EnemyStateMachine();
        idleState = new EnemyIdleState(this, enemyStateMachine);
        attackState = new EnemyAttackState(this, enemyStateMachine);
        chaseState = new EnemyChaseState(this, enemyStateMachine);

        animator = GetComponent<Animator>();
    }

    private void Start()
    {
        currentHealth = maxHealth;

        if (healthBarPrefab != null)
        {
            if (NetworkServer.active || !NetworkClient.active)
            {
                GameObject bar = Instantiate(healthBarPrefab);

                if (NetworkServer.active)
                    NetworkServer.Spawn(bar);

                healthBar = bar.GetComponent<EnemyHealthBar>();

                if (healthBar != null)
                {
                    healthBar.Setup(transform, maxHealth);
                    healthBar.UpdateHealth(currentHealth);
                }
                else
                {
                    Debug.LogError("Missing EnemyHealthBar on HealthBar prefab.");
                }
            }
        }

        enemyIdleBaseInstance.Initialize(gameObject, this);
        enemyAttackBaseInstance.Initialize(gameObject, this);
        enemyChaseBaseInstance.Initialize(gameObject, this);

        rb = GetComponent<Rigidbody2D>();
        enemyStateMachine.Initialize(idleState);

        if (homePosition == null)
        {
            GameObject obj = GameObject.Find("Start");
            if (obj != null)
            {
                homePosition = obj.transform;
            }
            else
            {
                Debug.LogWarning("Cannot find ,Start' in hierarchy");
            }
        }
    }

    private void Update()
    {
        if (!ShouldRunAI()) return;
        if (enemyStateMachine?.CurrentEnemyState == null) return;

        playerTransform = SelectTarget();
        if (playerTransform == null) return;

        float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);
        setAggroStatus(distanceToPlayer < aggroDistance);
        setStrikingDistanceStatus(distanceToPlayer < attackDistance);

        enemyStateMachine.CurrentEnemyState.FrameUpdate();
    }

    private void FixedUpdate()
    {
        if (!ShouldRunAI()) return;
        if (enemyStateMachine?.CurrentEnemyState == null) return;

        enemyStateMachine.CurrentEnemyState.PhysicsUpdate();
    }

    #region Health / Death

    public void Damage(float damageAmount)
    {
        animator.SetBool("isHit", true);
        ResetIsHit();

        currentHealth -= damageAmount;

        if (healthBar != null)
            healthBar.UpdateHealth(currentHealth);

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    public void ResetIsHit()
    {
        animator.SetBool("isHit", false);
    }

    public void Die()
    {
        if (healthBar != null)
            Destroy(healthBar.gameObject);

        Destroy(gameObject);
    }

    #endregion

    #region Movement

    public virtual void moveEnemy(Vector2 velocity)
    {
        if (!CanMove)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        smoothedVelocity = Vector2.Lerp(smoothedVelocity, velocity, 0.2f);
        rb.linearVelocity = velocity;
        checkForLeftOrRightFacing(smoothedVelocity);
    }


    public void checkForLeftOrRightFacing(Vector2 velocity)
    {
        float threshold = 0.2f;

        if (Mathf.Abs(velocity.x) < threshold)
            return;

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

    #endregion

    #region Animation Triggers

    private void AnimationTriggerEvent(AnimationTriggerType triggerType)
    {
        enemyStateMachine.CurrentEnemyState.AnimationTriggerEvent(triggerType);
    }

    public enum AnimationTriggerType
    {
        EnemyDamaged,
        EnemyFlies,
        EnemyDies,
        EnemyAttack,
    }

    #endregion

    #region Distance Check

    public void setAggroStatus(bool isAggroed)
    {
        IsAggroed = isAggroed;
    }

    public void setStrikingDistanceStatus(bool isWithinStrikingDistance)
    {
        IsWithinStrikingDistance = isWithinStrikingDistance;
    }

    private Transform SelectTarget()
    {
        float viewR = (ViewDistance > 0f) ? ViewDistance : (AggroDistance > 0f) ? AggroDistance : 8f;

        Transform weakestInView = PlayerRegistry.GetWeakestNearbyPlayer(transform.position, viewR);
        if (weakestInView != null)
            return weakestInView;

        Transform closest = PlayerRegistry.GetClosestPlayer(transform.position);
        return closest;
    }

    public Transform GetPlayer()
    {
        if (playerTransform == null)
        {
            Transform playerObj = PlayerRegistry.GetClosestPlayer(transform.position);

            if (playerObj != null)
                playerTransform = playerObj.transform;
        }
        return playerTransform;
    }

    #endregion
}
