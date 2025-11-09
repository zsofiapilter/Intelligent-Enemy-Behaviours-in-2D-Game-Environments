using Mirror;
using UnityEngine;

[RequireComponent(typeof(Enemy))]
public class EnemyBarBinder : NetworkBehaviour
{
    [Header("Health")]
    [SerializeField] float maxHealth = 100f;

    [Header("Bar")]
    [SerializeField] GameObject healthBarPrefab;
    private EnemyHealthBar bar;

    private Enemy enemy;

    [SyncVar(hook = nameof(OnHpChanged))]
    private float hp;

    public float MaxHp => maxHealth;
    public float CurrentHp => hp;

    private void Awake()
    {
        enemy = GetComponent<Enemy>();
    }

    public override void OnStartServer()
    {
        maxHealth = enemy != null ? enemy.maxHealth : maxHealth;
        hp = enemy != null ? enemy.currentHealth : maxHealth;
    }

    public override void OnStartClient()
    {
        EnsureBarExistsAndSetup();
        OnHpChanged(0f, hp);
    }

    private void EnsureBarExistsAndSetup()
    {
        if (bar == null)
        {
            bar = GetComponentInChildren<EnemyHealthBar>(true);
            if (bar == null && healthBarPrefab != null)
            {
                var go = Instantiate(healthBarPrefab);
                bar = go.GetComponent<EnemyHealthBar>();
            }
            if (bar != null)
                bar.Setup(transform, maxHealth);
        }
    }

    private void Update()
    {
        if (!isServer || enemy == null) return;

        if (Mathf.Abs(enemy.currentHealth - hp) > 0.001f)
            hp = enemy.currentHealth;
    }

    private void OnHpChanged(float oldVal, float newVal)
    {
        if (bar == null) EnsureBarExistsAndSetup();
        if (bar != null) bar.UpdateHealth(newVal);
    }
}
