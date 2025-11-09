using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Enemy))]
public class EnemyPathFinding : MonoBehaviour
{
    [HideInInspector] public PathFinderManager pathfindermanager;
    [HideInInspector] public Transform target;
    public float waypointThreshold = 0.1f;
    public float moveSpeed = 3f;
    private Enemy enemy;
    private List<Vector2> currentPath = new();
    private int currentIndex = 0;

    private void Awake()
    {
        enemy = GetComponent<Enemy>();

        if (pathfindermanager == null)
        {
            pathfindermanager = FindAnyObjectByType<PathFinderManager>();
        }
    }

    private void Update()
    {
        if (target == null || pathfindermanager == null)
            return;

        if (currentPath.Count == 0)
        {
            var path = pathfindermanager.FindPath(transform.position, target.position);
            if (path != null && path.Count > 1)
            {
                currentPath = path;
                currentIndex = 1;
            }
        }

        FollowPath();
    }

    private void FollowPath()
    {
        if (!enemy.CanMove || currentIndex >= currentPath.Count) return;

        Vector2 currentWaypoint = currentPath[currentIndex];
        Vector2 dir = (currentWaypoint - (Vector2)transform.position).normalized;
        SafeMove(dir);

        if (Vector2.Distance(transform.position, currentWaypoint) < waypointThreshold)
        {
            currentIndex++;
            if (currentIndex >= currentPath.Count)
            {
                currentPath.Clear();
            }
        }
    }

    private void SafeMove(Vector2 direction)
    {
        if (!enemy.CanMove)
        {
            enemy.moveEnemy(Vector2.zero);
            return;
        }

        if (direction.sqrMagnitude < 0.01f)
        {
            enemy.moveEnemy(Vector2.zero);
            return;
        }

        direction.Normalize();
        Vector2 moveVec = direction * moveSpeed;
        float checkDistance = moveVec.magnitude * Time.deltaTime + 0.05f;
        Vector2 origin = (Vector2)transform.position;

        LayerMask mask = pathfindermanager.obstacleMask;

        if (!Physics2D.Raycast(origin, direction, checkDistance, mask))
        {
            enemy.moveEnemy(moveVec);
            return;
        }

        Vector2 xMove = new Vector2(moveVec.x, 0f);
        if (Mathf.Abs(xMove.x) > 0.01f &&
            !Physics2D.Raycast(origin, xMove.normalized, xMove.magnitude * Time.deltaTime + 0.05f, mask))
        {
            enemy.moveEnemy(xMove);
            return;
        }

        Vector2 yMove = new Vector2(0f, moveVec.y);
        if (Mathf.Abs(yMove.y) > 0.01f &&
            !Physics2D.Raycast(origin, yMove.normalized, yMove.magnitude * Time.deltaTime + 0.05f, mask))
        {
            enemy.moveEnemy(yMove);
            return;
        }

        enemy.moveEnemy(Vector2.zero);
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        currentPath.Clear();
    }
}
