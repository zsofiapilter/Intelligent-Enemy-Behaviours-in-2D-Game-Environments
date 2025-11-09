using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class ProjectileDamage : MonoBehaviour
{
    [SerializeField] private float damage = 15f;
    [SerializeField] private float lifeTime = 5f;

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;

        Destroy(gameObject, lifeTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        IDamagable dmg = other.GetComponent<IDamagable>();
        if (dmg != null)
        {
            dmg.Damage(damage);
            Destroy(gameObject);
        }
        else if (!other.isTrigger)
        {
            Destroy(gameObject);
        }
    }
}
