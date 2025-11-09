using UnityEngine;
using Mirror;

[CreateAssetMenu(fileName = "MirrorMultiplayerAttackForce", menuName = "Enemy Logic/Attack Logic/Mirror Force Attack")]
public class MirrorMultiplayerAttackForce : EnemyAttackSOBase
{
    [SerializeField] private float _timeBetweenAttacks = 1f;

    private float _timer;
    private bool _playerInRange = false;

    [Header("Combat Settings")]
    [SerializeField] private float _attackRange = 1.5f;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private float _attackDamage = 15f;
    [SerializeField] private Transform _attackPoint;

    [Header("Targeting")]
    [SerializeField] private float _sameRangeEpsilon = 3f;
    [SerializeField] private float _aggroRadius = 6f;

    public override void Initialize(GameObject gameObject, Enemy enemy)
    {
        base.Initialize(gameObject, enemy);
        _attackPoint = gameObject.transform.Find("AttackPoint");

        if (_attackPoint == null)
        {
            Debug.LogWarning("Enemy is missing AttackPoint transform!");
        }
    }

    public override void DoEnterLogic()
    {
        base.DoEnterLogic();
        _playerInRange = false;
        _timer = 0f;
    }

    public override void DoFrameUpdateLogic()
    {
        base.DoFrameUpdateLogic();

        if (!NetworkServer.active) return;

        float viewR = GetViewRadiusOrFallback();
        Transform weakestInView = PlayerRegistry.GetWeakestNearbyPlayer(enemy.transform.position, viewR);

        Transform target = null;
        if (weakestInView != null)
        {
            target = weakestInView;
        }
        else
        {
            Transform closest = PlayerRegistry.GetClosestPlayer(enemy.transform.position);
            if (closest == null) return;
            target = closest;
        }

        float dist = Vector2.Distance(enemy.transform.position, target.position);
        _playerInRange = dist <= _attackRange;

        if (!_playerInRange)
        {
            enemy.enemyStateMachine.changeState(enemy.chaseState);
            return;
        }

        enemy.animator.SetBool("isAttacking", true);
        enemy.CanMove = false;
        enemy.moveEnemy(Vector2.zero);

        if (_timer >= _timeBetweenAttacks)
        {
            _timer = 0f;
            AttackPlayer();
        }

        _timer += Time.deltaTime;
    }

    private float GetViewRadiusOrFallback()
    {
        if (enemy != null && enemy.ViewDistance > 0f)
            return enemy.ViewDistance;

        if (_aggroRadius > 0f)
            return _aggroRadius;

        return 8f;
    }


    private void AttackPlayer()
    {
        if (!NetworkServer.active) return;
        if (_attackPoint == null) return;

        Collider2D[] hits = Physics2D.OverlapCircleAll(_attackPoint.position, _attackRange, playerLayer);

        foreach (var col in hits)
        {
            var dmg = col.GetComponent<IDamagable>() ?? col.GetComponentInParent<IDamagable>() ?? col.GetComponentInChildren<IDamagable>();

            if (dmg != null)
            {
                dmg.Damage(_attackDamage);
            }
        }
    }


    public override void DoPhysicsUpdateLogic()
    {
        base.DoPhysicsUpdateLogic();
    }

    public override void ResetValues()
    {
        base.ResetValues();
        _playerInRange = false;
    }

    public override void DoExitLogic()
    {
        base.DoExitLogic();
        enemy.CanMove = true;
    }
}
