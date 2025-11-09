using UnityEngine;

[CreateAssetMenu(fileName = "EnemyChaseRunaway", menuName = "Enemy Logic/Chase Logic/Runaway")]
public class EnemyChaseRunaway : EnemyChaseSOBase
{
    [SerializeField] private float _runawaySpeed = 2f;
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
        if (playerTransform == null)
        {
            Transform playerObj = PlayerRegistry.GetClosestPlayer(transform.position);
            if (playerObj != null)
                playerTransform = playerObj.transform;
            else
                return;
        }

        if (enemy.IsWithinStrikingDistance)
        {
            Vector3 runDir = (enemy.transform.position - playerTransform.position).normalized;
            enemy.moveEnemy(runDir * _runawaySpeed);
        }
        else
        {
            enemy.moveEnemy(Vector2.zero);
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
