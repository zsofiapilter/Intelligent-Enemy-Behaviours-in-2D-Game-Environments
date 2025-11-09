using UnityEngine;

[CreateAssetMenu(fileName = "EnemyAttackForce", menuName = "Enemy Logic/Attack Logic/Force Attack")]
public class EnemyAttackForce : EnemyAttackSOBase
{
    #region Serialized Fields (Inspector)

    [SerializeField] private float _timeBetweenAttacks = 1f;

    [Header("Combat Settings")]
    [SerializeField] private float _attackRange  = 1.5f;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private float _attackDamage = 15f;
    [SerializeField] private Transform _attackPoint;

    #endregion


    #region Private Runtime Variables

    private PlayerController player;
    private float _timer;
    private bool _playerInRange = false;

    #endregion


    #region Initialization

    public override void Initialize(GameObject gameObject, Enemy enemy)
    {
        base.Initialize(gameObject, enemy);

        player = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerController>();

        _attackPoint = gameObject.transform.Find("AttackPoint");
        if (_attackPoint == null)
            Debug.LogWarning("Enemy is missing AttackPoint transform!");
    }

    #endregion


    #region State Entry / Exit

    public override void DoEnterLogic()
    {
        base.DoEnterLogic();
        _playerInRange = false;
        enemy.CanMove   = false;
    }

    public override void ResetValues()
    {
        base.ResetValues();
        _playerInRange = false;
        enemy.CanMove   = true;
    }

    #endregion


    #region Frame and Physics Updates

    public override void DoFrameUpdateLogic()
    {
        base.DoFrameUpdateLogic();

        Transform player = enemy.GetPlayer();
        if (player == null)
            return;

        float distanceToPlayer = Vector2.Distance(enemy.transform.position, player.position);

        _playerInRange = distanceToPlayer <= _attackRange;

        if (!_playerInRange)
        {
            enemy.enemyStateMachine.changeState(enemy.chaseState);
            return;
        }

        enemy.animator.SetBool("isAttacking", true);
        enemy.moveEnemy(Vector2.zero);

        if (_timer >= _timeBetweenAttacks)
        {
            _timer = 0f;
            AttackPlayer();
        }

        _timer += Time.deltaTime;
    }

    public override void DoPhysicsUpdateLogic()
    {
        base.DoPhysicsUpdateLogic();
    }

    #endregion


    #region Helper Methods

    private void AttackPlayer()
    {
        if (_attackPoint == null) return;

        Collider2D[] hitPlayers = Physics2D.OverlapCircleAll(
            _attackPoint.position,
            _attackRange,
            LayerMask.GetMask("Player"));

        foreach (Collider2D playerCollider in hitPlayers)
        {
            IDamagable damagable = playerCollider.GetComponent<IDamagable>();
            if (damagable != null)
                damagable.Damage(_attackDamage);
        }
    }

    #endregion
}
