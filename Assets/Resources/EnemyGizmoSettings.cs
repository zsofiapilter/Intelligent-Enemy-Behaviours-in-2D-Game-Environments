using UnityEngine;

[CreateAssetMenu(fileName = "EnemyGizmoSettings", menuName = "Debug/Enemy Gizmo Settings")]
public class EnemyGizmoSettings : ScriptableObject
{
    [Header("Switch (common)")]
    public bool drawAll = true;

    public bool drawUnselected = true;

    [Header("Runtime (Build)")]
    public bool runtimeEnabled = true;

    public float lineWidth = 0.03f;

    [Min(8), Tooltip("Sengment number for circles. Bigger number = smoother circle, but more performance cost.")]
    public int circleSegments = 48;

    [Tooltip("Steering vector scaling.")]
    public float vectorScale = 1f;

    [Header("Enemy Gizmos")]
    public bool showAggro = true;
    public bool showAttack = true;
    public bool showViewCircle = true;

    [Header("Gather Around (EnemyAttackGather)")]
    public bool showGatherRing = true;
    public bool showSlotTarget = true;
    public bool showSeparationRadius = false;
    public bool showAttackRange = false;
    public bool showSteeringVector = false;

    [Header("Colours")]
    public Color aggroColor = new Color(0.5f, 0.7f, 0.2f, 1f);
    public Color attackColor = new Color(0.3f, 0.3f, 0.9f, 1f);
    public Color viewCircleColor = new Color(0.2f, 0.8f, 0.2f, 0.8f);
    public Color ringColor = Color.cyan;
    public Color slotColor = Color.yellow;
    public Color separationColor = Color.magenta;
    public Color attackRangeColor = Color.red;
    public Color steeringColor = Color.green;

    [Header("Bresenham LOS Colours")]
    public Color bresPointColor = new Color(0.2f, 0.9f, 0.9f, 1f);
    public Color bresBlockedPointColor = new Color(0.9f, 0.2f, 0.2f, 1f);
    public Color bresLineColor = new Color(0.7f, 0.7f, 0.7f, 0.8f);
    public Color bresHitColor = new Color(1f, 0.4f, 0.1f, 1f);
    public Color bresHomeColor = new Color(0.6f, 0.4f, 1f, 1f);
    public Color bresViewColor = new Color(0.2f, 0.8f, 0.2f, 0.8f);


    private static EnemyGizmoSettings _instance;
    public static EnemyGizmoSettings Instance
    {
        get
        {
            if (_instance == null)
                _instance = Resources.Load<EnemyGizmoSettings>("EnemyGizmoSettings");
            return _instance;
        }
    }
}
