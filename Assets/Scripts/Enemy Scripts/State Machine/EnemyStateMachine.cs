using UnityEngine;

public class EnemyStateMachine 
{
    public EnemyState CurrentEnemyState { get; set; }
    public void Initialize(EnemyState startingEnemyState)
    {
        CurrentEnemyState = startingEnemyState;
        CurrentEnemyState.EnterState();
    }

    public void changeState(EnemyState newState)
    {
        CurrentEnemyState.ExitState();
        CurrentEnemyState = newState;
        CurrentEnemyState.EnterState();
    }
}
