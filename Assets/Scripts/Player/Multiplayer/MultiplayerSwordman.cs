using UnityEngine;
using Mirror;

public class MultiplayerSwordman : MultiplayerController
{
    [Header("Combat Settings")]
    [SerializeField] private float attackRange = 1f;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private float attackDamage = 15f;
    [SerializeField] private Transform attackPoint;

    protected override void HandleInput()
    {
        float moveX = 0f, moveY = 0f;

        if (Input.GetKey(KeyCode.RightArrow)) moveX = 1f;
        if (Input.GetKey(KeyCode.LeftArrow)) moveX = -1f;
        if (Input.GetKey(KeyCode.UpArrow)) moveY = 1f;
        if (Input.GetKey(KeyCode.DownArrow)) moveY = -1f;
        if (Input.GetKey(KeyCode.S)) m_Anim.Play("Sit");

        Vector2 input = new Vector2(moveX, moveY).normalized;

        CmdMove(input);

        if (Input.GetKeyDown(KeyCode.X))
        {
            CmdAttack();
        }
    }

    [Command]
    private void CmdMove(Vector2 direction)
    {
        moveDirection = direction;

        if (direction.x > 0) isFacingLeft = false;
        else if (direction.x < 0) isFacingLeft = true;

        RpcUpdateAnimation(direction);
    }

    [ClientRpc]
    private void RpcUpdateAnimation(Vector2 dir)
    {
        if (m_Anim == null) return;

        if (dir == Vector2.zero)
            m_Anim.Play("Idle");
        else
            m_Anim.Play("Run");
    }

    [Command]
    private void CmdAttack()
    {
        RpcPlayAttackAnimation();

        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayer);

        foreach (var enemy in hitEnemies)
        {
            IDamagable dmg = enemy.GetComponent<IDamagable>();
            if (dmg != null)
                dmg.Damage(attackDamage);
        }
    }

    [ClientRpc]
    private void RpcPlayAttackAnimation()
    {
        if (m_Anim != null)
            m_Anim.Play("Attack");
    }

    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        PlayerRegistry.Register(transform);
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
        PlayerRegistry.Unregister(transform);
    }

}
