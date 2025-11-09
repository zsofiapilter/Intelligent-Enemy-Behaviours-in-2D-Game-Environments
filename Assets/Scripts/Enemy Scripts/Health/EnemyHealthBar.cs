using UnityEngine;
using UnityEngine.UI;

public class EnemyHealthBar : MonoBehaviour
{
    [SerializeField] private Image fillImage;
    [SerializeField] private Vector3 worldOffset = new Vector3(0, 1.5f, 0);
    [SerializeField] private bool useScreenSpaceOverlay = false;

    private Transform target;
    private float maxHealth = 1f;

    public void Setup(Transform targetTransform, float maxHp)
    {
        target = targetTransform;
        maxHealth = Mathf.Max(0.0001f, maxHp);
        if (fillImage != null) fillImage.fillAmount = 1f;
    }

    public void UpdateHealth(float currentHealth)
    {
        if (fillImage == null) return;
        float t = Mathf.Clamp01(maxHealth > 0f ? currentHealth / maxHealth : 0f);
        fillImage.fillAmount = t;
    }

    private void LateUpdate()
    {
        if (target == null) { Destroy(gameObject); return; }

        if (useScreenSpaceOverlay)
        {
            Vector3 screen = Camera.main ? Camera.main.WorldToScreenPoint(target.position + worldOffset)
                                         : target.position;
            transform.position = screen;
        }
        else
        {
            transform.position = target.position + worldOffset;
            transform.rotation = Quaternion.identity;
        }
    }
}
