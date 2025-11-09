using UnityEngine;

public class EnemyIdleSOBase : ScriptableObject
{
    protected Enemy enemy;
    protected Transform transform;
    protected GameObject gameObject;

    protected Transform playerTransform;

    public virtual void Initialize(GameObject gameObject, Enemy enemy)
    {
        this.gameObject = gameObject;
        transform = gameObject.transform;
        this.enemy = enemy;

        Transform playerObj = PlayerRegistry.GetClosestPlayer(transform.position);
        if (playerObj != null)
            playerTransform = playerObj.transform;

    }

    public virtual void DoEnterLogic()
    {

    }
    public virtual void DoExitLogic()
    {
        ResetValues();
    }
    public virtual void DoFrameUpdateLogic()
    {
        if (enemy.IsAggroed)
        {
            enemy.enemyStateMachine.changeState(enemy.chaseState);
        }
    }
    public virtual void DoPhysicsUpdateLogic()
    {

    }

    public virtual void DoAnimationTriggerEventLogic(Enemy.AnimationTriggerType triggerType)
    {

    }

    public virtual void ResetValues()
    {

    }
}
