using UnityEngine;

[CreateAssetMenu(fileName = "EnemyAttackSingleStateProjectile", menuName = "Enemy Logic/Attack Logic/Straight Single Projectile")]
public class EnemyAttackSingleStateProjectile : EnemyAttackSOBase
{
    [SerializeField] private Rigidbody2D bulletPrefab;

    [Header("Timing")]
    [SerializeField] private float _timeBetweenAttacks = 1f;
    [SerializeField] private float _timeTillExit = 2f;

    [Header("Exit Condition")]
    [SerializeField] private float _distanceToCountExit = 3f;

    [Header("Projectile")]
    [SerializeField] private float _bulletSpeed = 5f;

    private float _timer;
    private float _exitTimer;

    public override void DoAnimationTriggerEventLogic(Enemy.AnimationTriggerType triggerType)
    {
        base.DoAnimationTriggerEventLogic(triggerType);
    }

    public override void DoEnterLogic()
    {
        base.DoEnterLogic();
    }

    public override void DoExitLogic()
    {
        base.DoExitLogic();
    }

    public override void DoFrameUpdateLogic()
    {
        base.DoFrameUpdateLogic();

        Transform player = enemy.GetPlayer();
        if (player == null)
            return;

        enemy.animator.SetBool("isAttacking", true);
        enemy.moveEnemy(Vector2.zero);

        _timer += Time.deltaTime;

        if (_timer >= _timeBetweenAttacks)
        {
            _timer = 0f;

            Vector2 dir = (player.position - enemy.transform.position).normalized;

            Rigidbody2D bullet = Instantiate(
                bulletPrefab, enemy.transform.position, Quaternion.identity);

            bullet.linearVelocity = dir * _bulletSpeed;
        }

        if (Vector2.Distance(enemy.transform.position, player.position) > _distanceToCountExit)
        {
            _exitTimer += Time.deltaTime;
            if (_exitTimer >= _timeTillExit)
            {
                enemy.enemyStateMachine.changeState(enemy.chaseState);
            }
        }
        else
        {
            _exitTimer = 0f;
        }
    }


    public override void DoPhysicsUpdateLogic()
    {
        base.DoPhysicsUpdateLogic();
    }

    public override void Initialize(GameObject gameObject, Enemy enemy)
    {
        base.Initialize(gameObject, enemy);
    }

    public override void ResetValues()
    {
        base.ResetValues();
    }
}
