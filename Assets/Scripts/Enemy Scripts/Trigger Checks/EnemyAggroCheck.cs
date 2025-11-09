using UnityEngine;

public class EnemyAggroCheck : MonoBehaviour
{
    public GameObject player { get; set; }

    private Enemy _enemy;

    private void Awake()
    {
        Transform playerObj = PlayerRegistry.GetClosestPlayer(transform.position);

        _enemy = GetComponentInParent<Enemy>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject == player)
        {
            _enemy.setAggroStatus(true);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject == player)
        {
            _enemy.setAggroStatus(false);
        }
    }
}
