using UnityEngine;
using System.Collections.Generic;

public class DebugPanel : MonoBehaviour
{
    [Header("Connect")]
    public Enemy targetEnemy;
    public EnemyGizmoSettings settings;
    public RinoSpawner spawner;
    public List<Enemy> targetEnemies = new List<Enemy>();

    [Header("Panel")]
    public KeyCode toggleKey = KeyCode.BackQuote;
    public bool startOpen = true;

    [Header("Tabs per scene (manual)")]
    public bool showBoidsTab = true;
    public bool showBresenhamTab = false;
    public bool showLineOfSightTab = false;
    public bool showMultiplayerTab = false;
    public bool showGOAPTab = false;

    List<AlgorithmTab> _visibleTabs;
    string[] _visibleTabNames;


    [Header("Window Size")]
    public float minWidth = 220f;
    public float minHeight = 170f;
    public Vector2 defaultOpenSize = new Vector2(400f, 260f);

    [Header("Style (Dark Theme)")]
    [Range(10, 28)] public int fontSize = 20;
    public Color windowBgColor = new Color(0.12f, 0.12f, 0.12f, 0.95f);
    public Color textColor = new Color(0.92f, 0.92f, 0.92f, 1f);
    public Color accentColor = new Color(0.35f, 0.65f, 1f, 1f);

    private bool _open;
    private Rect _window = new Rect(12, 12, 400, 560);
    private Vector2 _scroll;

    enum AlgorithmTab { Boids, Bresenham, LineOfSight, Multiplayer, GOAP }
    AlgorithmTab _tab = AlgorithmTab.Boids;

    enum BoidsGizmoScope { Selected, AllGather }
    BoidsGizmoScope _boidsScope = BoidsGizmoScope.AllGather;

    bool _foldGizmos = true;
    bool _foldGather = true;
    bool _foldSpawn = true;
    int _spawnCount = 3;
    float _spawnRadius = 1.5f;

    Texture2D _winBgTex;
    Texture2D _whiteTex;
    GUIStyle _winStyle, _rich, _line, _btn, _fold, _toggle, _toolbarBtn;
    Texture2D _whiteTexChip;

    const float EDGE = 6f;
    const float CORNER = 14f;
    enum ResizeDir { None, L, R, T, B, TL, TR, BL, BR }
    ResizeDir _resizeDir = ResizeDir.None;
    bool _resizing = false;
    Vector2 _resizeStartMouseScreen;
    Rect _resizeStartWindow;
    int _resizeControlId;
    public bool IsOpen => _open;

    void Awake()
    {
        _open = startOpen;

        _window.width = defaultOpenSize.x;
        _window.height = defaultOpenSize.y;

        if (settings == null) settings = EnemyGizmoSettings.Instance;
        if (targetEnemy == null) targetEnemy = FindFirstObjectByType<Enemy>();
        if (spawner == null) spawner = FindFirstObjectByType<RinoSpawner>();

        if (targetEnemies == null) targetEnemies = new List<Enemy>(5);
        if (targetEnemy == null) targetEnemy = FindFirstObjectByType<Enemy>();
        if (targetEnemy != null && !targetEnemies.Contains(targetEnemy)) targetEnemies.Add(targetEnemy);

        EnsureBoidsGatherVisualsOnAllEnemies();
        EnsureGoapGizmosOnAll();

        BuildVisibleTabs();
        if (_visibleTabs.Count == 0)
        {
            AutoDetectVisibleTabs();
        }
        EnsureValidSelectedTab();

    }

    void Update()
    {
        if (Time.timeScale == 0f) return;
        if (Input.GetKeyDown(toggleKey)) _open = !_open;
    }

    void OnGUI()
    {
        if (!_open) return;

        BuildStyles();

        _window = GUILayout.Window(9981, _window, DrawWindow, "Visualization", _winStyle);

        ClampWindowToScreen();
    }

    void OnDestroy()
    {
        if (_winBgTex != null) { Destroy(_winBgTex); _winBgTex = null; }
        if (_whiteTex != null) { Destroy(_whiteTex); _whiteTex = null; }
    }

    void ClampWindowToScreen()
    {
        float margin = 8f;

        _window.width = Mathf.Max(_window.width, minWidth);
        _window.height = Mathf.Max(_window.height, minHeight);

        _window.x = Mathf.Clamp(_window.x, margin - _window.width, Screen.width - margin);
        _window.y = Mathf.Clamp(_window.y, margin, Screen.height - margin);

        _window.width = Mathf.Min(_window.width, Screen.width - margin * 2);
        _window.height = Mathf.Min(_window.height, Screen.height - margin * 2);
    }

    void DrawWindow(int id)
    {
        var e = Event.current;
        var titleBar = new Rect(0, 0, _window.width, 24f);
        GUI.DragWindow(titleBar);
        if (e.type == EventType.MouseDown && e.clickCount == 2 && titleBar.Contains(e.mousePosition))
        {
            _window.size = defaultOpenSize;
            e.Use();
        }


        BeginCard();
        GUILayout.BeginVertical();
        GUILayout.Label("<b>Guide for the game:</b>", _rich);
        GUILayout.Label("<b>Press SPACE to close/open the Visualization Panel</b>", _rich);
        GUILayout.Label("<b>Press ESC to open menu</b>", _rich);
        GUILayout.Label("<b>Use the arrows to move the player</b>", _rich);
        GUILayout.Label("<b>Press X to attack</b>", _rich);
        GUILayout.Label("<b>Scale the Visualization tab by grabbing the right bottom corner", _rich);
        GUILayout.EndVertical();
        EndCard();

        GUILayout.Space(6);

        GUILayout.BeginHorizontal();
        GUILayout.Label("<b>Algorithm:</b>", _rich, GUILayout.Width(200));
        using (new GUILayout.HorizontalScope())
        {
            if (_visibleTabs == null || _visibleTabs.Count == 0)
            {
                GUILayout.Label("<i>No tabs configured for this scene.</i>", _rich);
            }
            else if (_visibleTabs.Count == 1)
            {
                GUILayout.Label(_visibleTabNames[0], _rich);
                _tab = _visibleTabs[0];
            }
            else
            {
                for (int i = 0; i < _visibleTabs.Count; i++)
                {
                    var t = _visibleTabs[i];
                    bool on = (_tab == t);
                    var c = on ? accentColor : new Color(0.25f, 0.25f, 0.25f, 1f);
                    var prev = GUI.backgroundColor;
                    GUI.backgroundColor = c;
                    if (GUILayout.Toggle(on, _visibleTabNames[i], _toolbarBtn) && !on)
                    {
                        _tab = t;
                    }
                    GUI.backgroundColor = prev;
                }
            }
        }


        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
        EnsureValidSelectedTab();

        GUILayout.Space(4);

        _scroll = GUILayout.BeginScrollView(_scroll);

        if (settings == null)
        {
            GUILayout.Label("<i>No EnemyGizmoSettings asset.</i>", _rich);
#if UNITY_EDITOR
            if (GUILayout.Button("Create Settings (Resources/EnemyGizmoSettings.asset)", _btn))
            {
                var dir = "Assets/Resources";
                if (!System.IO.Directory.Exists(dir))
                    System.IO.Directory.CreateDirectory(dir);

                var s = ScriptableObject.CreateInstance<EnemyGizmoSettings>();
                UnityEditor.AssetDatabase.CreateAsset(s, "Assets/Resources/EnemyGizmoSettings.asset");
                UnityEditor.AssetDatabase.SaveAssets();
                UnityEditor.AssetDatabase.Refresh();
                settings = s;
            }
#endif
            GUILayout.EndScrollView();
            DoResizeHandles();
            return;
        }

        using (new GUILayout.VerticalScope(GUI.skin.box))
        {
            if (_tab != AlgorithmTab.GOAP)
            {
                GUILayout.Label("<b>Enemy basic</b>", _rich);
                DrawColorToggle(ref settings.showAggro, "Striking circle", settings.aggroColor);
                DrawColorToggle(ref settings.showAttack, "Attack circle", settings.attackColor);
            }
        }

        switch (_tab)
        {
            case AlgorithmTab.Boids:
                {
                    GUILayout.Space(6);
                    _foldSpawn = GUILayout.Toggle(_foldSpawn, (_foldSpawn ? "▼ " : "► ") + "Spawn", _fold);
                    if (_foldSpawn)
                    {
                        if (spawner == null) spawner = FindAnyObjectByType<RinoSpawner>();
                        if (spawner == null)
                            GUILayout.Label("<i>No RinoSpawner in scene.</i>", _rich);
                        else
                        {
                            GUILayout.BeginHorizontal();
                            GUILayout.Label($"Count: {_spawnCount}", _line, GUILayout.Width(140));
                            _spawnCount = Mathf.RoundToInt(GUILayout.HorizontalSlider(_spawnCount, 1, 20, GUILayout.Width(180)));
                            GUILayout.EndHorizontal();

                            GUILayout.BeginHorizontal();
                            GUILayout.Label($"Radius: {_spawnRadius:F1}", _line, GUILayout.Width(140));
                            _spawnRadius = GUILayout.HorizontalSlider(_spawnRadius, 0f, 5f, GUILayout.Width(180));
                            GUILayout.EndHorizontal();

                            GUILayout.Space(2);
                            if (GUILayout.Button("Spawn Rino where cursor", _btn)) spawner.SpawnAtCursor(_spawnCount, _spawnRadius);
                            if (GUILayout.Button("Spawn Rino around Player", _btn)) spawner.SpawnAroundPlayer(_spawnCount, _spawnRadius);
                            if (GUILayout.Button("Despawn ALL spawned", _btn)) spawner.DespawnAllSpawned();
                        }
                    }

                    GUILayout.Space(6);

                    _foldGather = GUILayout.Toggle(_foldGather, (_foldGather ? "▼ " : "► ") + "EnemyAttackGather (Boids)", _fold);
                    if (_foldGather) DrawGatherSection(_rich);

                    settings.runtimeEnabled = GUILayout.Toggle(settings.runtimeEnabled, "Runtime gizmos (build)", _toggle);

                    break;
                }

            case AlgorithmTab.Bresenham:
                {
                    if (!targetEnemy)
                    {
                        GUILayout.Label("<i>No target Enemy set.</i>", _rich);
                        break;
                    }

                    var go = targetEnemy.gameObject;

                    var binder = go.GetComponent<EnemyChaseSOBinder>();
                    if (!binder) binder = go.AddComponent<EnemyChaseSOBinder>();

                    var drawer = go.GetComponent<BresenhamDebugDrawer>();
                    if (!drawer) drawer = go.AddComponent<BresenhamDebugDrawer>();

                    drawer.enemy = targetEnemy;
                    drawer.settings = settings;
                    if (!drawer.chaseSO) drawer.chaseSO = binder.bresenhamChaseSO;

                    using (new GUILayout.VerticalScope(GUI.skin.box))
                    {
                        GUILayout.Label("<b>Bresenham LOS Visualization</b>", _rich);

                        if (!drawer.chaseSO)
                        {
                            GUILayout.Label("<i>Assign a Bresenham chase SO on the Enemy (EnemyChaseSOBinder.bresenhamChaseSO).</i>", _rich);
                        }

                        DrawColorToggle(ref settings.showViewCircle, "View circle", settings.viewCircleColor);
                        DrawToggleWithChip(ref drawer.showLine, "Straight line (enemy→player)", settings.bresLineColor);
                        DrawToggleWithChip(ref drawer.showGridPoints, "Grid sample points", settings.bresPointColor);
                        DrawToggleWithChip(ref drawer.showFirstHit, "First obstacle marker", settings.bresHitColor);
                        DrawToggleWithChip(ref drawer.showHome, "Home position", settings.bresHomeColor);

                        GUILayout.Space(4);
                        GUILayout.BeginHorizontal();
                        GUILayout.Label($"Point size: {drawer.pointSize:0.00}", _line, GUILayout.Width(200));
                        drawer.pointSize = GUILayout.HorizontalSlider(drawer.pointSize, 0.02f, 0.3f, GUILayout.Width(180));
                        GUILayout.EndHorizontal();

                        GUILayout.Space(6);
                        settings.runtimeEnabled = GUILayout.Toggle(settings.runtimeEnabled, "Enable runtime render in build", _toggle);
                    }
                    break;
                }


            case AlgorithmTab.LineOfSight:
                {
                    var los = LosManager.Instance ? LosManager.Instance : FindAnyObjectByType<LosManager>();
                    using (new GUILayout.VerticalScope(GUI.skin.box))
                    {
                        GUILayout.Label("<b>Line of Sight (Dots)</b>", _rich);

                        if (!los)
                        {
                            GUILayout.Label("<i>No LosManager in scene.</i>", _rich);
                            break;
                        }

                        foreach (var d in FindObjectsOfType<Enemy>())
                        {
                            if (!d) continue;
                            var drawer = d.GetComponent<LineOfSightDebugDrawer>();
                            if (!drawer) drawer = d.gameObject.AddComponent<LineOfSightDebugDrawer>();

                            var active = d.enemyChaseBaseInstance as EnemyChaseLineOfSight;
                            if (active != null) drawer.chaseSO = active;
                            else
                            {
                                var binder = d.GetComponent<EnemyChaseSOBinder>();
                                drawer.chaseSO = binder && binder.lineOfSightChaseSO
                                    ? binder.lineOfSightChaseSO as EnemyChaseLineOfSight
                                    : null;
                            }

                            drawer.enemy = d;
                            drawer.settings = settings;
                            drawer.showViewCircle = settings.showViewCircle;
                        }

                        DrawColorToggle(ref settings.showViewCircle, "View circle", settings.viewCircleColor);

                        bool draw = los.drawDots;
                        ToggleRowWithChip(ref draw, $"Draw dots (F3)", new Color(0.7f, 0.7f, 0.7f, 1f));
                        if (draw != los.drawDots) los.SetDrawDots(draw);

                        bool visOnly = los.showVisibleOnly;
                        ToggleRowWithChip(ref visOnly, "Show visible only", new Color(1f, 0.9f, 0.2f, 1f));
                        if (visOnly != los.showVisibleOnly) los.SetShowVisibleOnly(visOnly);

                        GUILayout.Space(6);
                        DrawDivider();

                        float r = los.radius;
                        if (SliderRow("View radius", ref r, 0.5f, 20f))
                            los.radius = r;

                        float sp = los.spacing;
                        if (SliderRow("Dot spacing", ref sp, 0.25f, 3f))
                            los.spacing = sp;

                        GUILayout.Space(8);
                        GUILayout.Label("<b>Enemy Vision</b>", _rich);

                        float enemyView = 5f;
                        bool haveAny = false;
                        foreach (var d in FindObjectsOfType<Enemy>())
                        {
                            var chase = d ? d.enemyChaseBaseInstance as EnemyChaseLineOfSight : null;
                            if (chase == null)
                            {
                                var binder = d ? d.GetComponent<EnemyChaseSOBinder>() : null;
                                if (binder && binder.lineOfSightChaseSO)
                                    chase = binder.lineOfSightChaseSO as EnemyChaseLineOfSight;
                            }
                            if (chase != null)
                            {
                                enemyView = Mathf.Max(0.01f, chase.ViewDistance);
                                haveAny = true;
                                break;
                            }
                        }

                        if (!haveAny)
                        {
                            GUILayout.Label("<i>No EnemyChaseLineOfSight found on active enemies.</i>", _rich);
                        }
                        else
                        {
                            if (SliderRow("View Distance", ref enemyView, 0.5f, 30f))
                            {
                                enemyView = Mathf.Max(0.01f, enemyView);
                                foreach (var d in FindObjectsOfType<Enemy>())
                                {
                                    var chase = d ? d.enemyChaseBaseInstance as EnemyChaseLineOfSight : null;
                                    if (chase == null)
                                    {
                                        var binder = d ? d.GetComponent<EnemyChaseSOBinder>() : null;
                                        if (binder && binder.lineOfSightChaseSO)
                                            chase = binder.lineOfSightChaseSO as EnemyChaseLineOfSight;
                                    }
                                    if (chase != null) chase.ViewDistance = enemyView;
                                }
                            }

                            GUILayout.BeginHorizontal();
                            if (GUILayout.Button("Reset Enemy View to 5", _btn))
                            {
                                foreach (var d in FindObjectsOfType<Enemy>())
                                {
                                    var chase = d ? d.enemyChaseBaseInstance as EnemyChaseLineOfSight : null;
                                    if (chase == null)
                                    {
                                        var binder = d ? d.GetComponent<EnemyChaseSOBinder>() : null;
                                        if (binder && binder.lineOfSightChaseSO)
                                            chase = binder.lineOfSightChaseSO as EnemyChaseLineOfSight;
                                    }
                                    if (chase != null) chase.ViewDistance = 5f;
                                }
                            }
                            GUILayout.EndHorizontal();
                        }

                        GUILayout.Space(6);
                        GUILayout.BeginHorizontal();
                        if (GUILayout.Button("Rebuild grid (manual)", _btn))
                            los.RebuildGrid();
                        if (GUILayout.Button("Reset to Start", _btn))
                            los.ResetToStart(true);
                        GUILayout.EndHorizontal();

                        GUILayout.Space(6);
                        DrawDivider();

                        GUILayout.Label($"<b>Stats</b>", _rich);
                        GUILayout.Label($"Visible: {los.LastVisibleCount} / {los.DotCount}", _line);
                        GUILayout.Label($"Skipped by distance: {los.LastSkippedByDistance}", _line);
                        GUILayout.Label($"Blocked by obstacles: {los.LastBlockedByObstacles}", _line);

                        GUILayout.Space(4);
                        GUILayout.Label("<i>Note:</i> the above parameters change automatically", _rich);
                    }
                    break;
                }
            case AlgorithmTab.GOAP:
                {
                    BeginCard();

                    DrawDivider();
                    GUILayout.Space(6);

                    GUILayout.Label("<b>EnemyGoap Basics</b>", _rich);
                    DrawColorToggle(ref settings.showAttack, "Attack circle", settings.attackColor);
                    DrawColorToggle(ref settings.showAggro, "Striking circle", settings.aggroColor);
                    DrawColorToggle(ref settings.showViewCircle, "View circle", settings.viewCircleColor);
                    settings.runtimeEnabled = GUILayout.Toggle(settings.runtimeEnabled, "Enable runtime gizmo render", _toggle);

                    GUILayout.Label("<b>GOAP (Agent State)</b>", _rich);

                    var agent = FindAnyObjectByType<GoapAgent>();
                    if (!agent)
                    {
                        GUILayout.Label("<i>No GoapAgent found.</i>", _rich);
                        EndCard();
                        break;
                    }

                    var s = agent.DebugWorldState;
                    GUILayout.Label($"LOS: {s.HasLOS}", _line);
                    GUILayout.Label($"Distance: {s.DistanceBand}", _line);
                    GUILayout.Label($"WeaponReady: {s.WeaponReady}", _line);
                    GUILayout.Label($"LowHP: {s.LowHP}", _line);

                    GUILayout.Space(6);
                    DrawDivider();
                    GUILayout.Space(6);

                    GUILayout.Label("<b>Plan</b>", _rich);
                    var plan = agent.DebugPlan;
                    int idx = agent.DebugPlanIndex;

                    if (plan == null || plan.Count == 0)
                    {
                        GUILayout.Label("<i>no plan</i>", _rich);
                    }
                    else
                    {
                        for (int i = 0; i < plan.Count; i++)
                        {
                            bool active = (i == idx);
                            string nm = plan[i] ? (string.IsNullOrWhiteSpace(plan[i].ActionName) ? plan[i].GetType().Name : plan[i].ActionName) : "(null)";
                            GUILayout.Label((active ? "→ " : "  ") + nm, _line);
                        }
                    }

                    GUILayout.Space(6);

                    EndCard();
                    break;
                }


            case AlgorithmTab.Multiplayer:

                DrawColorToggle(ref settings.showViewCircle, "View circle", settings.viewCircleColor);
                GUILayout.Space(4);

                GUILayout.Space(8);
                BeginCard();
                GUILayout.Label("<b>Per-enemy target view</b>", _rich);

                var allEnemies = FindObjectsOfType<Enemy>();
                if (allEnemies.Length == 0)
                {
                    GUILayout.Label("<i>No Enemy found.</i>", _rich);
                }
                else
                {
                    foreach (var k in allEnemies)
                    {
                        if (k == null) continue;

                        EnemyChaseBresenhamLOS chase = null;

                        chase = k.enemyChaseBaseInstance as EnemyChaseBresenhamLOS;

                        if (chase == null)
                        {
                            var binder = k.GetComponent<EnemyChaseSOBinder>();
                            if (binder && binder.bresenhamChaseSO) chase = binder.bresenhamChaseSO as EnemyChaseBresenhamLOS;
                        }

                        GUILayout.Space(4);
                        GUILayout.Label($"<b>{k.name}</b>", _rich);

                        if (chase == null)
                        {
                            GUILayout.Label("<i>No Bresenham chase assigned.</i>", _rich);
                            continue;
                        }

                        var inView = chase.DebugPlayersInViewLOS();

                        var target = chase.DebugPickTarget();

                        if (inView.Count == 0)
                        {
                            GUILayout.Label("<i>No players in view (with LOS).</i>", _line);
                        }
                        else
                        {
                            foreach (var t in inView)
                            {
                                var hp = t.GetComponent<MultiplayerHealth>();
                                float d = Vector3.Distance(k.transform.position, t.position);
                                bool isTarget = (t == target);

                                string tag = isTarget ? " → <b>TARGET</b>" : "";
                                string hpStr = (hp != null) ? $"{hp.currentHealth:0}/{hp.maxHealth:0}" : "n/a";

                                GUILayout.Label($"{t.name}  [HP {hpStr}] {tag}", _line);
                            }
                        }
                    }
                }
                EndCard();

                GUILayout.Space(8);
                GUILayout.Label("<i>Lists refresh every frame.</i>", _rich);
                break;
        }

        GUILayout.EndScrollView();

        GUI.DragWindow(new Rect(0, 0, 10000, 24));

        DoResizeHandles();
    }

    IEnumerable<Enemy> GetBoidsScopeEnemies()
    {
        var all = FindObjectsByType<Enemy>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );
        foreach (var e in all)
            if (e && e.enemyAttackBaseInstance is EnemyAttackGather)
                yield return e;
    }

    void ForEachGatherInScope(System.Action<EnemyAttackGather> action)
    {
        foreach (var e in GetBoidsScopeEnemies())
        {
            var g = e.enemyAttackBaseInstance as EnemyAttackGather;
            if (g != null) action(g);
        }
    }

    void DrawGatherSection(GUIStyle rich)
    {
        EnemyAttackGather sample = null;
        foreach (var e in GetBoidsScopeEnemies())
        {
            sample = e.enemyAttackBaseInstance as EnemyAttackGather;
            if (sample != null) break;
        }

        if (sample == null)
        {
            GUILayout.Label("<i>No EnemyAttackGather found in scope.</i>", rich);
            return;
        }

        BeginCard();

        GUILayout.Label("<b>Gather (Boids)</b>", _rich);
        GUILayout.Space(6);

        ToggleRowWithChip(ref settings.showGatherRing, "Rings around player", settings.ringColor);
        ToggleRowWithChip(ref settings.showSlotTarget, "Slot target", settings.slotColor);
        ToggleRowWithChip(ref settings.showSeparationRadius, "Separation radius", settings.separationColor);
        ToggleRowWithChip(ref settings.showAttackRange, "Attack range", settings.attackRangeColor);
        ToggleRowWithChip(ref settings.showSteeringVector, "Steering vector", settings.steeringColor);

        GUILayout.Space(8);
        DrawDivider();

        GUILayout.Space(4);
        using (new GUILayout.VerticalScope())
        {
            if (GUILayout.Button("Reset Parameters to Start", _btn, GUILayout.Width(350)))
                ForEachGatherInScope(g => g.ResetToDefaults(true));

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Apply: Reinit ring to Desired", _btn, GUILayout.Width(350)))
                ForEachGatherInScope(g => g.RuntimeReinit(true));
        }

        bool anyChanged = false;

        float ring = sample.DesiredRingRadius;
        if (SliderRow("Ring radius", ref ring, 0.5f, 12f))
        {
            ForEachGatherInScope(g =>
            {
                g.DesiredRingRadius = ring;
                g.MinRingRadius = Mathf.Clamp(ring - 0.10f, 0.05f, ring);
                SoftRebuild(g);
            });
            anyChanged = true;
        }

        float sep = sample.SeparationRadius;
        if (SliderRow("Separation radius", ref sep, 0.05f, 5f))
        {
            ForEachGatherInScope(g =>
            {
                g.SeparationRadius = sep;
                SoftRebuild(g);
            });
            anyChanged = true;
        }

        float atk = sample.AttackRangeBonus;
        if (SliderRow("Attack range bonus", ref atk, 0f, 4f))
        {
            ForEachGatherInScope(g =>
            {
                g.AttackRangeBonus = atk;
                SoftRebuild(g);
            });
            anyChanged = true;
        }

        float vec = settings.vectorScale;
        if (SliderRow("Steering vector scale", ref vec, 0.1f, 5f))
        {
            settings.vectorScale = vec;
            anyChanged = true;
        }

        GUILayout.Space(8);

        if (anyChanged)
            GUILayout.Label("<i>Changes applied to all enemies in scope.</i>", _rich);

        EndCard();
    }

    void BeginCard()
    {
        GUILayout.BeginVertical(GUI.skin.box);
    }

    void EndCard()
    {
        GUILayout.EndVertical();
    }

    void SoftRebuild(EnemyAttackGather g)
    {
        ForEachGatherInScope(g => g.ApplyDebugParamChange(true));
    }


    void DrawDivider()
    {
        var r = GUILayoutUtility.GetRect(1, 3);
        if (_whiteTex == null)
        {
            _whiteTex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            _whiteTex.hideFlags = HideFlags.HideAndDontSave;
            _whiteTex.SetPixel(0, 0, Color.white);
            _whiteTex.Apply();
        }
        var prev = GUI.color;
        GUI.color = new Color(1, 1, 1, 0.15f);
        GUI.DrawTexture(new Rect(r.x, r.y + 1, r.width, 1f), _whiteTex);
        GUI.color = prev;
    }

    void ToggleRowWithChip(ref bool val, string label, Color chip)
    {
        GUILayout.BeginHorizontal();
        val = GUILayout.Toggle(val, label, _toggle);
        GUILayout.FlexibleSpace();

        Rect rr = GUILayoutUtility.GetRect(16, 16, GUILayout.Width(16), GUILayout.Height(16));
        if (_whiteTex == null)
        {
            _whiteTex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            _whiteTex.hideFlags = HideFlags.HideAndDontSave;
            _whiteTex.SetPixel(0, 0, Color.white);
            _whiteTex.Apply();
        }
        var prev = GUI.color;
        GUI.color = chip;
        GUI.DrawTexture(rr, _whiteTex);
        GUI.color = prev;

        GUILayout.EndHorizontal();
    }

    bool SliderRow(string label, ref float value, float min, float max, float labelWidth = 300, float sliderWidth = 150f)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label($"{label}: {value:0.00}", _line, GUILayout.Width(labelWidth));
        float newVal = GUILayout.HorizontalSlider(value, min, max, GUILayout.Width(sliderWidth));
        GUILayout.EndHorizontal();

        if (Mathf.Abs(newVal - value) > 1e-4f)
        {
            value = newVal;
            return true;
        }
        return false;
    }

    void DrawColorToggle(ref bool value, string label, Color color)
    {
        GUILayout.BeginHorizontal();
        value = GUILayout.Toggle(value, label, _toggle);
        GUILayout.Space(6);

        Rect colorRect = GUILayoutUtility.GetRect(18, 18, GUILayout.Width(18), GUILayout.Height(18));

        if (_whiteTex == null)
        {
            _whiteTex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            _whiteTex.hideFlags = HideFlags.HideAndDontSave;
            _whiteTex.SetPixel(0, 0, Color.white);
            _whiteTex.Apply();
        }
        GUI.DrawTexture(colorRect, _whiteTex);
        var prev = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(colorRect, _whiteTex);
        GUI.color = prev;

        GUILayout.EndHorizontal();
    }

    void BuildStyles()
    {
        if (_winBgTex == null)
        {
            _winBgTex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            _winBgTex.hideFlags = HideFlags.HideAndDontSave;
        }
        _winBgTex.SetPixel(0, 0, windowBgColor);
        _winBgTex.Apply();

        if (_whiteTex == null)
        {
            _whiteTex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            _whiteTex.hideFlags = HideFlags.HideAndDontSave;
            _whiteTex.SetPixel(0, 0, Color.white);
            _whiteTex.Apply();
        }

        if (_winStyle == null) _winStyle = new GUIStyle(GUI.skin.window);
        _winStyle.normal.background = _winBgTex;
        _winStyle.active.background = _winBgTex;
        _winStyle.focused.background = _winBgTex;
        _winStyle.onNormal.background = _winBgTex;
        _winStyle.padding = new RectOffset(12, 12, 28, 12);
        _winStyle.border = new RectOffset(6, 6, 24, 6);

        if (_rich == null) _rich = new GUIStyle(GUI.skin.label);
        _rich.richText = true; _rich.fontSize = fontSize; _rich.wordWrap = true;
        _rich.normal.textColor = textColor;

        if (_line == null) _line = new GUIStyle(GUI.skin.label);
        _line.richText = true; _line.fontSize = fontSize; _line.normal.textColor = textColor;
        _line.wordWrap = false; _line.alignment = TextAnchor.MiddleLeft; _line.clipping = TextClipping.Clip;

        if (_btn == null) _btn = new GUIStyle(GUI.skin.button);
        _btn.fontSize = fontSize; _btn.alignment = TextAnchor.MiddleCenter;

        if (_fold == null) _fold = new GUIStyle(GUI.skin.button);
        _fold.fontSize = fontSize; _fold.alignment = TextAnchor.MiddleLeft; _fold.richText = true;

        if (_toggle == null) _toggle = new GUIStyle(GUI.skin.toggle);
        _toggle.fontSize = fontSize; _toggle.alignment = TextAnchor.MiddleLeft; _toggle.richText = true;
        _toggle.normal.textColor = textColor; _toggle.onNormal.textColor = textColor;
        _toggle.hover.textColor = textColor; _toggle.onHover.textColor = textColor;
        _toggle.active.textColor = textColor; _toggle.onActive.textColor = textColor;
        _toggle.focused.textColor = textColor; _toggle.onFocused.textColor = textColor;

        if (_toolbarBtn == null) _toolbarBtn = new GUIStyle(GUI.skin.button);
        _toolbarBtn.fontSize = fontSize;
        _toolbarBtn.padding = new RectOffset(8, 8, 4, 4);
        _toolbarBtn.margin = new RectOffset(2, 2, 2, 2);
        _toolbarBtn.alignment = TextAnchor.MiddleCenter;
        _toolbarBtn.richText = true;
    }

    void DoResizeHandles()
    {
        Event e = Event.current;
        Vector2 localMouse = e.mousePosition;
        Vector2 mouseScreen = GUIUtility.GUIToScreenPoint(localMouse);

        Rect r = new Rect(0, 0, _window.width, _window.height);

        Rect left = new Rect(r.xMin, r.yMin + CORNER, EDGE, r.height - CORNER * 2);
        Rect right = new Rect(r.xMax - EDGE, r.yMin + CORNER, EDGE, r.height - CORNER * 2);
        Rect top = new Rect(r.xMin + CORNER, r.yMin, r.width - CORNER * 2, EDGE);
        Rect bottom = new Rect(r.xMin + CORNER, r.yMax - EDGE, r.width - CORNER * 2, EDGE);

        Rect tl = new Rect(r.xMin, r.yMin, CORNER, CORNER);
        Rect tr = new Rect(r.xMax - CORNER, r.yMin, CORNER, CORNER);
        Rect bl = new Rect(r.xMin, r.yMax - CORNER, CORNER, CORNER);
        Rect br = new Rect(r.xMax - CORNER, r.yMax - CORNER, CORNER, CORNER);

        int id = _resizeControlId != 0 ? _resizeControlId : (_resizeControlId = GUIUtility.GetControlID(FocusType.Passive));

        ResizeDir hoverDir = HitWhich(localMouse, tl, tr, bl, br, left, right, top, bottom);

        switch (e.type)
        {
            case EventType.MouseDown:
                if (e.button == 0 && hoverDir != ResizeDir.None)
                {
                    _resizing = true;
                    _resizeDir = hoverDir;
                    _resizeStartMouseScreen = mouseScreen;
                    _resizeStartWindow = _window;
                    GUIUtility.hotControl = id;
                    e.Use();
                }
                break;

            case EventType.MouseDrag:
                if (_resizing && GUIUtility.hotControl == id)
                {
                    Vector2 delta = mouseScreen - _resizeStartMouseScreen;
                    Rect w = _resizeStartWindow;

                    if (_resizeDir == ResizeDir.L || _resizeDir == ResizeDir.TL || _resizeDir == ResizeDir.BL)
                    {
                        w.xMin += delta.x;
                        if (w.width < minWidth) w.xMin = w.xMax - minWidth;
                    }
                    if (_resizeDir == ResizeDir.R || _resizeDir == ResizeDir.TR || _resizeDir == ResizeDir.BR)
                    {
                        w.xMax += delta.x;
                        if (w.width < minWidth) w.xMax = w.xMin + minWidth;
                    }
                    if (_resizeDir == ResizeDir.T || _resizeDir == ResizeDir.TL || _resizeDir == ResizeDir.TR)
                    {
                        w.yMin += delta.y;
                        if (w.height < minHeight) w.yMin = w.yMax - minHeight;
                    }
                    if (_resizeDir == ResizeDir.B || _resizeDir == ResizeDir.BL || _resizeDir == ResizeDir.BR)
                    {
                        w.yMax += delta.y;
                        if (w.height < minHeight) w.yMax = w.yMin + minHeight;
                    }

                    _window = w;
                    e.Use();
                }
                break;

            case EventType.MouseUp:
                if (_resizing && e.button == 0 && GUIUtility.hotControl == id)
                {
                    _resizing = false;
                    _resizeDir = ResizeDir.None;
                    GUIUtility.hotControl = 0;
                    e.Use();
                }
                break;

            case EventType.Repaint:
                DrawCornerGrip(br);
                break;
        }
    }

    ResizeDir HitWhich(Vector2 p, Rect tl, Rect tr, Rect bl, Rect br, Rect l, Rect r, Rect t, Rect b)
    {
        if (br.Contains(p)) return ResizeDir.BR;
        if (bl.Contains(p)) return ResizeDir.BL;
        if (tr.Contains(p)) return ResizeDir.TR;
        if (tl.Contains(p)) return ResizeDir.TL;
        if (l.Contains(p)) return ResizeDir.L;
        if (r.Contains(p)) return ResizeDir.R;
        if (t.Contains(p)) return ResizeDir.T;
        if (b.Contains(p)) return ResizeDir.B;
        return ResizeDir.None;
    }

    void DrawCornerGrip(Rect br)
    {
        var c = new Color(1, 1, 1, 0.25f);
        HandlesLikeLine(new Vector2(br.xMax - 10, br.yMax - 2), new Vector2(br.xMax - 2, br.yMax - 2), c);
        HandlesLikeLine(new Vector2(br.xMax - 10, br.yMax - 6), new Vector2(br.xMax - 2, br.yMax - 6), c);
        HandlesLikeLine(new Vector2(br.xMax - 10, br.yMax - 10), new Vector2(br.xMax - 2, br.yMax - 10), c);
    }

    void HandlesLikeLine(Vector2 a, Vector2 b, Color col)
    {
        var prev = GUI.color;
        GUI.color = col;
        if (_whiteTex == null)
        {
            _whiteTex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            _whiteTex.hideFlags = HideFlags.HideAndDontSave;
            _whiteTex.SetPixel(0, 0, Color.white);
            _whiteTex.Apply();
        }
        var r = Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
        GUI.DrawTexture(new Rect(r.xMin, r.yMin, r.width, 1f), _whiteTex);
        GUI.color = prev;
    }

    void DrawColorChip(string label, Color color)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(label, _line);
        GUILayout.FlexibleSpace();
        Rect r = GUILayoutUtility.GetRect(18, 18, GUILayout.Width(18), GUILayout.Height(18));
        if (_whiteTex == null)
        {
            _whiteTex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            _whiteTex.hideFlags = HideFlags.HideAndDontSave;
            _whiteTex.SetPixel(0, 0, Color.white);
            _whiteTex.Apply();
        }
        var prev = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(r, _whiteTex);
        GUI.color = prev;
        GUILayout.EndHorizontal();
    }

    void DrawToggleWithChip(ref bool val, string label, Color col)
    {
        GUILayout.BeginHorizontal();
        val = GUILayout.Toggle(val, label, _toggle);
        GUILayout.Space(6);
        if (_whiteTexChip == null)
        {
            _whiteTexChip = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            _whiteTexChip.hideFlags = HideFlags.HideAndDontSave;
            _whiteTexChip.SetPixel(0, 0, Color.white);
            _whiteTexChip.Apply();
        }
        Rect r = GUILayoutUtility.GetRect(18, 18, GUILayout.Width(18), GUILayout.Height(18));
        var prev = GUI.color;
        GUI.color = col;
        GUI.DrawTexture(r, _whiteTexChip);
        GUI.color = prev;
        GUILayout.EndHorizontal();
    }

    List<Enemy> ValidTargets()
    {
        if (targetEnemies == null) return new List<Enemy>();
        for (int i = targetEnemies.Count - 1; i >= 0; i--)
            if (targetEnemies[i] == null) targetEnemies.RemoveAt(i);

        var set = new HashSet<Enemy>(targetEnemies);
        return new List<Enemy>(set);
    }

    void EnsureBoidsGatherVisualsOnAllEnemies()
    {
        if (settings == null) return;

        settings.drawAll = true;
        settings.runtimeEnabled = true;

        settings.showGatherRing = true;
        settings.showSlotTarget = true;
        settings.showSeparationRadius = true;
        settings.showAttackRange = true;
        settings.showSteeringVector = true;

        foreach (var enemy in FindObjectsOfType<Enemy>())
        {
            if (enemy == null) continue;

            var gather = enemy.enemyAttackBaseInstance as EnemyAttackGather;
            if (gather == null) continue;

            gather.RuntimeReinit(true);
        }
    }

    void EnsureGoapGizmosOnAll()
    {
        var s = settings ? settings : EnemyGizmoSettings.Instance;
        foreach (var eg in FindObjectsOfType<EnemyGoap>())
        {
            var g = eg.GetComponent<EnemyGoapGizmos>();
            if (!g) g = eg.gameObject.AddComponent<EnemyGoapGizmos>();
            g.enemyGoap = eg;
            g.settings = s;
        }
    }


    void BuildVisibleTabs()
    {
        _visibleTabs = new List<AlgorithmTab>(4);
        if (showBoidsTab) _visibleTabs.Add(AlgorithmTab.Boids);
        if (showBresenhamTab) _visibleTabs.Add(AlgorithmTab.Bresenham);
        if (showLineOfSightTab) _visibleTabs.Add(AlgorithmTab.LineOfSight);
        if (showMultiplayerTab) _visibleTabs.Add(AlgorithmTab.Multiplayer);
        if (showGOAPTab) _visibleTabs.Add(AlgorithmTab.GOAP);

        _visibleTabNames = GetNames(_visibleTabs);
    }

    void AutoDetectVisibleTabs()
    {
        _visibleTabs = new List<AlgorithmTab>(4);

        foreach (var e in FindObjectsOfType<Enemy>())
        {
            if (e && e.enemyAttackBaseInstance is EnemyAttackGather)
            {
                if (!_visibleTabs.Contains(AlgorithmTab.Boids))
                    _visibleTabs.Add(AlgorithmTab.Boids);
                break;
            }
        }

        if (FindAnyObjectByType<BresenhamDebugDrawer>() ||
            FindAnyObjectByType<EnemyChaseSOBinder>())
        {
            if (!_visibleTabs.Contains(AlgorithmTab.Bresenham))
                _visibleTabs.Add(AlgorithmTab.Bresenham);
        }

        var nm = FindAnyObjectByType<Mirror.NetworkManager>();
        if (nm) _visibleTabs.Add(AlgorithmTab.Multiplayer);

        _visibleTabNames = GetNames(_visibleTabs);
    }

    string[] GetNames(List<AlgorithmTab> tabs)
    {
        var list = new List<string>(tabs.Count);
        foreach (var t in tabs) list.Add(TabName(t));
        return list.ToArray();
    }

    string TabName(AlgorithmTab t)
    {
        switch (t)
        {
            case AlgorithmTab.Boids: return "Boids";
            case AlgorithmTab.Bresenham: return "Bresenham";
            case AlgorithmTab.LineOfSight: return "Line of Sight";
            case AlgorithmTab.Multiplayer: return "Multiplayer";
            default: return t.ToString();
        }
    }

    void EnsureValidSelectedTab()
    {
        if (_visibleTabs == null || _visibleTabs.Count == 0)
        {
            _tab = AlgorithmTab.Boids;
            return;
        }
        if (!_visibleTabs.Contains(_tab))
        {
            _tab = _visibleTabs[0];
        }
    }

    public void SetOpen(bool open)
    {
        _open = open;
    }

    public static void CloseAll()
    {
        foreach (var dp in FindObjectsOfType<DebugPanel>())
            dp.SetOpen(false);
    }
}
