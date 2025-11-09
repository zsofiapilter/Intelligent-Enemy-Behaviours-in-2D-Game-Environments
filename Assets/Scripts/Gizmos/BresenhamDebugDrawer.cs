using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Enemy))]
[DisallowMultipleComponent]
public class BresenhamDebugDrawer : MonoBehaviour
{
    public Enemy enemy;
    public EnemyGizmoSettings settings;
    public EnemyChaseBresenhamLOS chaseSO;

    [Header("Live toggles (DebugPanel drives these)")]
    public bool showGridPoints = true;
    public bool showLine = true;
    public bool showFirstHit = true;
    public bool showHome = true;

    [Range(0.02f, 0.3f)] public float pointSize = 0.08f;

    static Material _glMat;

    void Awake()
    {
        if (!enemy) enemy = GetComponent<Enemy>();
        if (!settings) settings = EnemyGizmoSettings.Instance;
        EnsureMaterial();
    }

    void EnsureMaterial()
    {
        if (_glMat) return;
        var shader = Shader.Find("Hidden/Internal-Colored");
        if (!shader)
        {
            Debug.LogWarning("BresenhamDebugDrawer: 'Hidden/Internal-Colored' shader not found.");
            return;
        }
        _glMat = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
        _glMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        _glMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        _glMat.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
        _glMat.SetInt("_ZWrite", 0);
    }

    void OnDrawGizmos()
    {
        if (!settings || !settings.drawAll) return;
        if (!enemy) enemy = GetComponent<Enemy>();
        if (!enemy) return;

        if (!chaseSO)
        {
            var binder = enemy.GetComponent<EnemyChaseSOBinder>();
            if (binder) chaseSO = binder.bresenhamChaseSO;
        }
        if (!chaseSO) return;

        var player = PlayerRegistry.GetClosestPlayer(enemy.transform.position);
        if (!player) return;

        var from = enemy.transform.position;
        var to = player.position;

        var pts = chaseSO.SampleLinePoints(from, to);
        int firstBlockedIndex = -1;

        if (pts != null && pts.Count > 2)
        {
            for (int i = 1; i < pts.Count - 1; i++)
            {
                Vector3 wp = new Vector3(pts[i].x, pts[i].y, 0f);
                var hit = Physics2D.OverlapCircle(wp, 0.4f, chaseSO.ObstacleMask);
                if (hit != null) { firstBlockedIndex = i; break; }
            }
        }

        if (pts != null && pts.Count > 0)
        {
            if (showLine)
            {
                Gizmos.color = settings.bresLineColor;
                Gizmos.DrawLine(from, to);
            }

            if (showGridPoints)
            {
                for (int i = 0; i < pts.Count; i++)
                {
                    Vector3 wp = new Vector3(pts[i].x, pts[i].y, 0f);
                    bool isEndpoint = (i == 0 || i == pts.Count - 1);
                    var col = (!isEndpoint && firstBlockedIndex != -1 && i >= firstBlockedIndex)
                              ? settings.bresBlockedPointColor
                              : settings.bresPointColor;
                    GizmoSquare(wp, pointSize, col);
                }
            }

            if (firstBlockedIndex != -1 && showFirstHit)
            {
                Vector3 hitP = new Vector3(pts[firstBlockedIndex].x, pts[firstBlockedIndex].y, 0f);
                GizmoCross(hitP, pointSize * 2f, settings.bresHitColor);
            }
        }

        if (showHome && enemy.homePosition)
            GizmoDiamond(enemy.homePosition.position, pointSize * 2f, settings.bresHomeColor);
    }

    void OnRenderObject()
    {
        if (!settings || !settings.drawAll) return;
        if (!settings.runtimeEnabled) return;
        if (_glMat == null) { EnsureMaterial(); if (_glMat == null) return; }
        if (!chaseSO || !enemy) return;

        var player = PlayerRegistry.GetClosestPlayer(enemy.transform.position);
        if (!player) return;

        var from = enemy.transform.position;
        var to = player.position;

        var pts = chaseSO.SampleLinePoints(from, to);
        int firstBlockedIndex = -1;

        if (pts != null && pts.Count > 2)
        {
            for (int i = 1; i < pts.Count - 1; i++)
            {
                Vector3 wp = new Vector3(pts[i].x, pts[i].y, 0f);
                var hit = Physics2D.OverlapCircle(wp, 0.4f, chaseSO.ObstacleMask);
                if (hit != null) { firstBlockedIndex = i; break; }
            }
        }

        _glMat.SetPass(0);

        if (showLine)
            GLLine(from, to, settings.bresLineColor);

        if (showGridPoints && pts != null)
        {
            for (int i = 0; i < pts.Count; i++)
            {
                bool isEndpoint = (i == 0 || i == pts.Count - 1);
                var col = (!isEndpoint && firstBlockedIndex != -1 && i >= firstBlockedIndex)
                          ? settings.bresBlockedPointColor
                          : settings.bresPointColor;

                Vector3 wp = new Vector3(pts[i].x, pts[i].y, 0f);
                GLSquare(wp, pointSize, col);
            }
        }

        if (showFirstHit && firstBlockedIndex != -1)
        {
            Vector3 hitP = new Vector3(pts[firstBlockedIndex].x, pts[firstBlockedIndex].y, 0f);
            GLCross(hitP, pointSize * 2f, settings.bresHitColor);
        }

        if (showHome && enemy.homePosition)
            GLDiamond(enemy.homePosition.position, pointSize * 2f, settings.bresHomeColor);
    }

    void GizmoSquare(Vector3 c, float s, Color col)
    {
        Gizmos.color = col;
        var h = s * 0.5f;
        var a = c + new Vector3(-h, -h, 0);
        var b = c + new Vector3(-h, h, 0);
        var d = c + new Vector3(h, -h, 0);
        var e = c + new Vector3(h, h, 0);
        Gizmos.DrawLine(a, b); Gizmos.DrawLine(b, e); Gizmos.DrawLine(e, d); Gizmos.DrawLine(d, a);
    }
    void GizmoCross(Vector3 c, float s, Color col)
    {
        Gizmos.color = col;
        var h = s * 0.5f;
        Gizmos.DrawLine(c + new Vector3(-h, -h), c + new Vector3(h, h));
        Gizmos.DrawLine(c + new Vector3(-h, h), c + new Vector3(h, -h));
    }
    void GizmoDiamond(Vector3 c, float s, Color col)
    {
        Gizmos.color = col;
        var h = s * 0.5f;
        var up = c + new Vector3(0, h);
        var right = c + new Vector3(h, 0);
        var down = c + new Vector3(0, -h);
        var left = c + new Vector3(-h, 0);
        Gizmos.DrawLine(up, right); Gizmos.DrawLine(right, down); Gizmos.DrawLine(down, left); Gizmos.DrawLine(left, up);
    }
    void GizmoCircle(Vector3 c, float r, Color col, int seg)
    {
        Gizmos.color = col;
        float step = Mathf.PI * 2f / seg;
        Vector3 prev = c + new Vector3(Mathf.Cos(0) * r, Mathf.Sin(0) * r);
        for (int i = 1; i <= seg; i++)
        {
            float a = i * step;
            Vector3 p = c + new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r);
            Gizmos.DrawLine(prev, p);
            prev = p;
        }
    }

    void GLLine(Vector3 a, Vector3 b, Color col)
    {
        GL.Begin(GL.LINES);
        GL.Color(col);
        GL.Vertex(a); GL.Vertex(b);
        GL.End();
    }
    void GLSquare(Vector3 c, float s, Color col)
    {
        var h = s * 0.5f;
        var a = c + new Vector3(-h, -h, 0);
        var b = c + new Vector3(-h, h, 0);
        var d = c + new Vector3(h, -h, 0);
        var e = c + new Vector3(h, h, 0);

        GL.Begin(GL.LINES);
        GL.Color(col);
        GL.Vertex(a); GL.Vertex(b);
        GL.Vertex(b); GL.Vertex(e);
        GL.Vertex(e); GL.Vertex(d);
        GL.Vertex(d); GL.Vertex(a);
        GL.End();
    }
    void GLCross(Vector3 c, float s, Color col)
    {
        var h = s * 0.5f;
        GL.Begin(GL.LINES);
        GL.Color(col);
        GL.Vertex(c + new Vector3(-h, -h, 0)); GL.Vertex(c + new Vector3(h, h, 0));
        GL.Vertex(c + new Vector3(-h, h, 0)); GL.Vertex(c + new Vector3(h, -h, 0));
        GL.End();
    }
    void GLDiamond(Vector3 c, float s, Color col)
    {
        var h = s * 0.5f;
        var up = c + new Vector3(0, h, 0);
        var right = c + new Vector3(h, 0, 0);
        var down = c + new Vector3(0, -h, 0);
        var left = c + new Vector3(-h, 0, 0);

        GL.Begin(GL.LINES);
        GL.Color(col);
        GL.Vertex(up); GL.Vertex(right);
        GL.Vertex(right); GL.Vertex(down);
        GL.Vertex(down); GL.Vertex(left);
        GL.Vertex(left); GL.Vertex(up);
        GL.End();
    }
    void GLCircle(Vector3 c, float r, Color col, int seg)
    {
        float step = Mathf.PI * 2f / seg;
        Vector3 prev = c + new Vector3(Mathf.Cos(0) * r, Mathf.Sin(0) * r, 0);
        GL.Begin(GL.LINES);
        GL.Color(col);
        for (int i = 1; i <= seg; i++)
        {
            float a = i * step;
            Vector3 p = c + new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, 0);
            GL.Vertex(prev); GL.Vertex(p);
            prev = p;
        }
        GL.End();
    }
}
