using UnityEngine;

public class MetricsPanel : MonoBehaviour
{
    [Header("Hook up")]
    public MetricsSpawner spawner;

    [Header("Panel")]
    public KeyCode toggleKey = KeyCode.F2;
    public bool openOnStart = true;

    [Header("Enemy Defaults (UI)")]
    [Range(1, 200)] public int enemyCount = 10;
    [Range(0f, 30f)] public float enemyRadius = 2.0f;

    [Header("Obstacle Defaults (UI)")]
    [Range(0, 500)] public int obstacleCount = 50;
    [Range(0f, 30f)] public float obstacleRadius = 6.0f;

    Rect win = new Rect(12, 12, 360, 260);
    bool open;

    void Awake()
    {
        open = openOnStart;
        if (!spawner) spawner = FindAnyObjectByType<MetricsSpawner>();
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey)) open = !open;
    }

    void OnGUI()
    {
        if (!open) return;
        win = GUILayout.Window(9102, win, DrawWin, "Metrics Spawner");
    }

    void DrawWin(int id)
    {
        if (!spawner)
        {
            GUILayout.Label("<i>No MetricsSpawner found. Assign one in the Inspector.</i>");
            GUI.DragWindow(new Rect(0, 0, 10000, 20));
            return;
        }

        GUILayout.Label("<b>Enemies</b>");
        RowSlider("Count", ref enemyCount, 1, 200);
        RowSlider("Radius", ref enemyRadius, 0f, 30f);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Spawn at Cursor"))
            spawner.SpawnEnemiesAtCursor(enemyCount, enemyRadius);
        if (GUILayout.Button("Spawn around Player"))
            spawner.SpawnEnemiesAroundPlayer(enemyCount, enemyRadius);
        GUILayout.EndHorizontal();

        if (GUILayout.Button("Despawn ENEMIES"))
            spawner.DespawnAllEnemies();

        GUILayout.Space(8);
        DrawLine();

        GUILayout.Label("<b>Obstacles</b>");
        RowSlider("Count", ref obstacleCount, 0, 500);
        RowSlider("Radius", ref obstacleRadius, 0f, 30f);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Spawn at Cursor"))
            spawner.SpawnObstaclesAtCursor(obstacleCount, obstacleRadius);
        if (GUILayout.Button("Spawn around Player"))
            spawner.SpawnObstaclesAroundPlayer(obstacleCount, obstacleRadius);
        GUILayout.EndHorizontal();

        if (GUILayout.Button("Despawn OBSTACLES"))
            spawner.DespawnAllObstacles();

        GUILayout.Space(6);
        if (GUILayout.Button("Despawn ALL"))
            spawner.DespawnAll();

        GUILayout.Space(6);
        GUILayout.Label("<i>Toggle with F2. Drag the title bar to move.</i>");
        GUI.DragWindow(new Rect(0, 0, 10000, 20));
    }

    void RowSlider(string label, ref int val, int min, int max)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label($"{label}: {val}", GUILayout.Width(140));
        val = Mathf.RoundToInt(GUILayout.HorizontalSlider(val, min, max, GUILayout.Width(160)));
        GUILayout.EndHorizontal();
    }

    void RowSlider(string label, ref float val, float min, float max)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label($"{label}: {val:0.0}", GUILayout.Width(140));
        val = GUILayout.HorizontalSlider(val, min, max, GUILayout.Width(160));
        GUILayout.EndHorizontal();
    }

    void DrawLine()
    {
        var r = GUILayoutUtility.GetRect(1, 2);
        if (Event.current.type == EventType.Repaint)
        {
            Color prev = GUI.color;
            GUI.color = new Color(1, 1, 1, 0.2f);
            GUI.DrawTexture(new Rect(r.xMin, r.yMin + 1, r.width, 1), Texture2D.whiteTexture);
            GUI.color = prev;
        }
    }
}
