using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Swordman : PlayerController
{
    [Header("Combat Settings")]
    [SerializeField] private float attackRange = 1f;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private float attackDamage = 15f;
    [SerializeField] private Transform attackPoint;

    private PlayerHealth health;

    private Vector2 moveInput;

    private void Awake()
    {
        health = GetComponent<PlayerHealth>();
    }

    private void Start()
    {
        health = GetComponent<PlayerHealth>();

        m_CapsulleCollider = GetComponent<CapsuleCollider2D>();
        m_Anim = transform.Find("model").GetComponent<Animator>();
        m_rigidbody = GetComponent<Rigidbody2D>();

        m_rigidbody.gravityScale = 0f;
        m_rigidbody.linearVelocity = Vector2.zero;

        PlayerRegistry.Register(transform);
    }

    private void Update()
    {
        HandleInput();

        UpdateAnimation();
    }

    private void FixedUpdate()
    {
        m_rigidbody.linearVelocity = moveInput * MoveSpeed;
    }

    private void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.S))
        {
            IsSit = true;
            m_Anim.Play("Sit");
            moveInput = Vector2.zero;
            m_rigidbody.linearVelocity = Vector2.zero;
            return;
        }
        else if (Input.GetKeyUp(KeyCode.S))
        {
            IsSit = false;
            m_Anim.Play("Idle");
        }

        if (m_Anim.GetCurrentAnimatorStateInfo(0).IsName("Sit") ||
            m_Anim.GetCurrentAnimatorStateInfo(0).IsName("Die"))
        {
            moveInput = Vector2.zero;
            m_rigidbody.linearVelocity = Vector2.zero;
            return;
        }

        float moveX = 0f;
        float moveY = 0f;

        if (Input.GetKey(KeyCode.RightArrow))
            moveX = 1f;
        else if (Input.GetKey(KeyCode.LeftArrow))
            moveX = -1f;

        if (Input.GetKey(KeyCode.UpArrow))
            moveY = 1f;
        else if (Input.GetKey(KeyCode.DownArrow))
            moveY = -1f;

        moveInput = new Vector2(moveX, moveY).normalized;

        if (moveX > 0)
            Filp(false);
        else if (moveX < 0)
            Filp(true);

        if (Input.GetKeyDown(KeyCode.X) && !m_Anim.GetCurrentAnimatorStateInfo(0).IsName("Attack"))
        {
            m_Anim.Play("Attack");
            PerformAttack();
        }

        if (Input.GetKey(KeyCode.Alpha1))
        {
            m_Anim.Play("Die");
            moveInput = Vector2.zero;
            m_rigidbody.linearVelocity = Vector2.zero;
            return;
        }
    }

    private void UpdateAnimation()
    {
        if (m_Anim.GetCurrentAnimatorStateInfo(0).IsName("Attack") ||
            m_Anim.GetCurrentAnimatorStateInfo(0).IsName("Sit") ||
            m_Anim.GetCurrentAnimatorStateInfo(0).IsName("Die"))
        {
            return;
        }

        if (moveInput == Vector2.zero)
        {
            m_Anim.Play("Idle");
        }
        else
        {
            m_Anim.Play("Run");
        }
    }

    private void PerformAttack()
    {
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayer);

        foreach (Collider2D enemyCollider in hitEnemies)
        {
            IDamagable damagable = enemyCollider.GetComponent<IDamagable>();
            if (damagable != null)
            {
                damagable.Damage(attackDamage);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }

    private void OnDestroy()
    {
        PlayerRegistry.Unregister(transform);
    }
}
