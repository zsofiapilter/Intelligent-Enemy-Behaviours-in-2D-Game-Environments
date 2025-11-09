using UnityEngine;

public interface IEnemyMoveable
{
    Rigidbody2D rb { get; set; }
    bool isFacingRight { get; set; }
    void moveEnemy(Vector2 velocity);
    void checkForLeftOrRightFacing(Vector2 velocity);
}
