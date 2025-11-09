using UnityEngine;

[CreateAssetMenu(fileName = "Chase-Direct To Player", menuName = "Enemy Logic/Chase Logic/Direct Chase")]
public class EnemyChaseDirectToPlayer : EnemyChaseSOBase
{
    [SerializeField] private float _movementSpeed = 2f;

    public override void DoAnimationTriggerEventLogic(Enemy.AnimationTriggerType triggerType)
    {
        base.DoAnimationTriggerEventLogic(triggerType);
    }

    public override void DoEnterLogic()
    {
        base.DoEnterLogic();
        if (enemy.animator != null)
        {
            enemy.animator.SetBool("isChasing", true);
        }
    }

    public override void DoExitLogic()
    {
        base.DoExitLogic();
        if (enemy.animator != null)
        {
            enemy.animator.SetBool("isChasing", false);
        }
    }

    public override void DoFrameUpdateLogic()
    {
        base.DoFrameUpdateLogic();

        if (playerTransform == null)
        {
            Transform playerObj = PlayerRegistry.GetClosestPlayer(transform.position);
            if (playerObj != null)
                playerTransform = playerObj.transform;
            else
                return;
        }

        if (enemy.IsAggroed)
        {
            Vector3 moveDirection = (playerTransform.position - enemy.transform.position).normalized;
            enemy.moveEnemy(moveDirection * _movementSpeed);

            if (enemy.IsWithinStrikingDistance)
            {
                enemy.enemyStateMachine.changeState(enemy.attackState);
            }
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
