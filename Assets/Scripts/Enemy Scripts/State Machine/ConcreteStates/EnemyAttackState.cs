using UnityEngine;

public class EnemyAttackState : EnemyState
{
    public EnemyAttackState(Enemy enemy, EnemyStateMachine enemyStateMachine) : base(enemy, enemyStateMachine)
    {
    }
    public override void AnimationTriggerEvent(Enemy.AnimationTriggerType triggerType)
    {
        base.AnimationTriggerEvent(triggerType);
        enemy.enemyAttackBaseInstance.DoAnimationTriggerEventLogic(triggerType);
    }

    public override void EnterState()
    {
        enemy.animator.SetBool("isAttacking", true);
        enemy.enemyAttackBaseInstance.DoEnterLogic();
        base.EnterState();
    }

    public override void ExitState()
    {
        enemy.animator.SetBool("isAttacking", false);
        enemy.enemyAttackBaseInstance.DoExitLogic();
        base.ExitState();
    }

    public override void FrameUpdate()
    {
        base.FrameUpdate();
        enemy.enemyAttackBaseInstance.DoFrameUpdateLogic();
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();
        enemy.enemyAttackBaseInstance.DoPhysicsUpdateLogic();
    }
}
