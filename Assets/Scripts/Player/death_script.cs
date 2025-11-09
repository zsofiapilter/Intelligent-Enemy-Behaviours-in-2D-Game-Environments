using UnityEngine;

public class death_script : MonoBehaviour
{
    public GameObject startPoint;
    public GameObject player;

    void Start()
    {
    }

    void Update()
    {
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            player.transform.position = startPoint.transform.position;
        }
    }
}
