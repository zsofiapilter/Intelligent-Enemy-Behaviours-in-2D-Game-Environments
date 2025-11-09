using UnityEngine;
using Mirror;

public class MultiplayerController : NetworkBehaviour
{
    public float MoveSpeed = 6f;

    protected Animator m_Anim;
    protected Rigidbody2D m_rigidbody;

    [SyncVar] protected Vector2 moveDirection;
    [SyncVar(hook = nameof(OnFlipChanged))] protected bool isFacingLeft = false;

    protected virtual void Start()
    {
        m_Anim = GetComponentInChildren<Animator>();
        m_rigidbody = GetComponent<Rigidbody2D>();
        if (m_rigidbody != null)
            m_rigidbody.gravityScale = 0f;
    }

    protected virtual void Update()
    {
        if (!isLocalPlayer) return;

        HandleInput();
    }

    protected virtual void FixedUpdate()
    {
        if (!isServer) return;
        m_rigidbody.velocity = moveDirection * MoveSpeed;
    }

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        var cam = Camera.main?.GetComponent<camera_movement>();
        if (cam != null) cam.target = gameObject;
    }

    protected virtual void HandleInput()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector2 dir = new Vector2(h, v).normalized;

        bool faceLeft = (dir.x < 0f) || (dir.x == 0f && isFacingLeft);
        if (faceLeft != isFacingLeft) CmdSetFacing(faceLeft);

        if (dir != moveDirection) CmdSetMove(dir);
    }

    [Command]
    private void CmdSetMove(Vector2 dir)
    {
        moveDirection = dir;
    }

    [Command]
    private void CmdSetFacing(bool faceLeft)
    {
        isFacingLeft = faceLeft;
    }

    private void OnFlipChanged(bool oldValue, bool newValue)
    {
        transform.localScale = new Vector3(newValue ? 1 : -1, 1, 1);
    }
}
