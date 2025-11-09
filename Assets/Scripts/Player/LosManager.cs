using UnityEngine;
using System.Collections.Generic;

public class LosManager : MonoBehaviour
{
    public static LosManager Instance;

    [Header("Grid Settings")]
    public GameObject dotPrefab;
    public Vector2Int gridSize = new Vector2Int(100, 100);
    public float radius = 3f;
    public float spacing = 1f;
    public LayerMask obstacleMask;
    public Transform player;

    [Header("Debug / Runtime Controls")]
    public bool drawDots = true;
    public bool showVisibleOnly = false;
    public bool showViewCircle = true;
    public KeyCode toggleDotsKey = KeyCode.F3;

    public int DotCount { get; private set; }
    public int LastVisibleCount { get; private set; }
    public int LastSkippedByDistance { get; private set; }
    public int LastBlockedByObstacles { get; private set; }

    private Vector2Int _startGridSize;
    private float _startRadius, _startSpacing;
    private bool _startDrawDots, _startShowVisibleOnly;

    private Vector2Int _appliedGridSize;
    private float _appliedSpacing;

    private Dictionary<Vector2Int, GameObject> dotMap = new Dictionary<Vector2Int, GameObject>();
    private HashSet<Vector2Int> visibleTiles = new HashSet<Vector2Int>();
    private Transform _dotsRoot;

    private void Awake()
    {
        if (Instance == null) Instance = this;

        if (player == null && GameObject.FindWithTag("Player"))
        {
            player = GameObject.FindWithTag("Player").transform;
        }

        if (_dotsRoot == null)
        {
            _dotsRoot = new GameObject("LOS_Dots").transform;
            _dotsRoot.SetParent(transform, false);
        }
        _startGridSize = gridSize;
        _startRadius = radius;
        _startSpacing = spacing;
        _startDrawDots = drawDots;
        _startShowVisibleOnly = showVisibleOnly;

        _appliedGridSize = gridSize;
        _appliedSpacing = spacing;
    }

    private void Start()
    {
        if (player == null && GameObject.FindWithTag("Player"))
        {
            player = GameObject.FindWithTag("Player").transform;
        }

        RebuildGrid();
    }

    private void Update()
    {
        if (player == null) return;

        if (Input.GetKeyDown(toggleDotsKey))
            SetDrawDots(!drawDots);

        if (_appliedSpacing != spacing || _appliedGridSize != gridSize)
        {
            RebuildGrid();
            _appliedSpacing = spacing;
            _appliedGridSize = gridSize;
        }

        UpdateVisibleTiles();
    }

    public void SetDrawDots(bool on)
    {
        drawDots = on;
        if (_dotsRoot) _dotsRoot.gameObject.SetActive(drawDots);
    }

    public void SetShowVisibleOnly(bool on)
    {
        showVisibleOnly = on;
    }

    public void SetShowViewCircle(bool on)
    {
        showViewCircle = on;
    }

    public void ResetToStart(bool rebuild = true)
    {
        gridSize = _startGridSize;
        radius = _startRadius;
        spacing = _startSpacing;
        drawDots = _startDrawDots;
        showVisibleOnly = _startShowVisibleOnly;

        _appliedGridSize = gridSize;
        _appliedSpacing = spacing;

        if (rebuild) RebuildGrid();
        SetDrawDots(drawDots);
    }


    public void RebuildGrid()
    {
        foreach (var kv in dotMap)
            if (kv.Value) Destroy(kv.Value);
        dotMap.Clear();
        DotCount = 0;

        for (int x = -gridSize.x / 2; x < gridSize.x / 2; x++)
        {
            for (int y = -gridSize.y / 2; y < gridSize.y / 2; y++)
            {
                var gridPos = new Vector2Int(x, y);
                var worldPos = new Vector3(x * spacing, y * spacing, 0f);

                var dot = Instantiate(dotPrefab, worldPos, Quaternion.identity, _dotsRoot);
                var sr = dot.GetComponent<SpriteRenderer>();
                if (sr) sr.color = Color.blue;

                dotMap[gridPos] = dot;
                DotCount++;
            }
        }

        if (_dotsRoot) _dotsRoot.gameObject.SetActive(drawDots);
    }

    private void UpdateVisibleTiles()
    {
        visibleTiles.Clear();

        int visibleCount = 0;
        int skippedDistance = 0;
        int blockedLinecasts = 0;

        foreach (var kvp in dotMap)
        {
            Vector2Int gridPos = kvp.Key;
            Vector3 worldPos = new Vector3(gridPos.x * spacing, gridPos.y * spacing, 0f);
            float dist = Vector3.Distance(worldPos, player.position);

            if (dist > radius)
            {
                skippedDistance++;
                if (drawDots)
                {
                    if (showVisibleOnly) kvp.Value.SetActive(false);
                    else
                    {
                        kvp.Value.SetActive(true);
                        SetDotColor(kvp.Value, Color.blue);
                    }
                }
                continue;
            }

            RaycastHit2D hit = Physics2D.Linecast(player.position, worldPos, obstacleMask);

            if (!hit)
            {
                visibleTiles.Add(gridPos);
                visibleCount++;

                if (drawDots)
                {
                    kvp.Value.SetActive(true);
                    SetDotColor(kvp.Value, Color.yellow);
                }
            }
            else
            {
                blockedLinecasts++;
                if (drawDots)
                {
                    if (showVisibleOnly) kvp.Value.SetActive(false);
                    else
                    {
                        kvp.Value.SetActive(true);
                        SetDotColor(kvp.Value, Color.blue);
                    }
                }
            }
        }

        LastVisibleCount = visibleCount;
        LastSkippedByDistance = skippedDistance;
        LastBlockedByObstacles = blockedLinecasts;
    }

    void SetDotColor(GameObject dot, Color c)
    {
        var sr = dot ? dot.GetComponent<SpriteRenderer>() : null;
        if (sr) sr.color = c;
    }

    public bool IsTileVisibleToPlayer(Vector3 worldPosition)
    {
        Vector2Int gridPos = new Vector2Int(
            Mathf.RoundToInt(worldPosition.x / spacing),
            Mathf.RoundToInt(worldPosition.y / spacing)
        );
        return visibleTiles.Contains(gridPos);
    }

    public List<Vector3> GetVisibleTileWorldPositions()
    {
        var worldPositions = new List<Vector3>(visibleTiles.Count);
        foreach (var gridPos in visibleTiles)
            worldPositions.Add(new Vector3(gridPos.x * spacing, gridPos.y * spacing, 0f));
        return worldPositions;
    }
}
