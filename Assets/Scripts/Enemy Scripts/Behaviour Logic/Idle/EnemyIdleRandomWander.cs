using UnityEngine;

[CreateAssetMenu(fileName = "Idle-Random Wander", menuName = "Enemy Logic/Idle Logic/Random Wander")]
public class EnemyIdleRandomWander : EnemyIdleSOBase
{
    [SerializeField] public float RandomMovementRange = 0.5f;
    [SerializeField] public float RandomMovementSpeed = 1f;

    private Vector3 _targetPosition;
    private Vector3 _direction;

    public override void DoAnimationTriggerEventLogic(Enemy.AnimationTriggerType triggerType)
    {
        base.DoAnimationTriggerEventLogic(triggerType);
    }

    public override void DoEnterLogic()
    {
        base.DoEnterLogic();

        _targetPosition = GetRandomPointInCircle();
    }

    public override void DoExitLogic()
    {
        base.DoExitLogic();
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

        _direction = (_targetPosition - enemy.transform.position).normalized;
        enemy.moveEnemy(_direction * RandomMovementSpeed);

        if ((enemy.transform.position - _targetPosition).sqrMagnitude < 0.01f)
        {
            _targetPosition = GetRandomPointInCircle();
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

    private Vector3 GetRandomPointInCircle()
    {
        return enemy.transform.position + (Vector3)UnityEngine.Random.insideUnitCircle * RandomMovementRange;
    }
}
