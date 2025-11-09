#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

public static class EnemyGizmosMenu
{
    private const string ResourcePath = "EnemyGizmoSettings";
    private const string ResourceAssetPath = "Assets/Resources/EnemyGizmoSettings.asset";

    private static EnemyGizmoSettings S
    {
        get
        {
            var s = EnemyGizmoSettings.Instance;
            if (s == null)
            {
                s = AssetDatabase.LoadAssetAtPath<EnemyGizmoSettings>(ResourceAssetPath);
            }
            return s;
        }
    }

    [MenuItem("Debug/Gizmos/Open Settings")]
    public static void OpenSettings()
    {
        var s = EnsureSettings();
        if (s) Selection.activeObject = s;
        EditorGUIUtility.PingObject(s);
    }

    [MenuItem("Debug/Gizmos/Create Settings Asset")]
    public static void CreateSettings()
    {
        if (File.Exists(ResourceAssetPath))
        {
            EditorUtility.DisplayDialog("EnemyGizmoSettings", "A Settings asset már létezik:\n" + ResourceAssetPath, "OK");
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<EnemyGizmoSettings>(ResourceAssetPath);
            return;
        }

        Directory.CreateDirectory("Assets/Resources");
        var s = ScriptableObject.CreateInstance<EnemyGizmoSettings>();
        AssetDatabase.CreateAsset(s, ResourceAssetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Selection.activeObject = s;
        EditorUtility.DisplayDialog("EnemyGizmoSettings", "Létrehozva:\n" + ResourceAssetPath, "OK");
    }

    private static EnemyGizmoSettings EnsureSettings()
    {
        var s = S;
        if (s == null)
        {
            if (!File.Exists(ResourceAssetPath))
                CreateSettings();
            s = AssetDatabase.LoadAssetAtPath<EnemyGizmoSettings>(ResourceAssetPath);
        }
        return s;
    }

    private static void Toggle(ref bool flag)
    {
        flag = !flag;
        EditorUtility.SetDirty(S);
        SceneView.RepaintAll();
    }

    // F1: Master switch
    [MenuItem("Debug/Gizmos/Toggle All _F1")]
    public static void ToggleAll()
    {
        var s = EnsureSettings(); if (!s) return;
        Toggle(ref s.drawAll);
    }

    // F2: Aggro circle
    [MenuItem("Debug/Gizmos/Toggle Aggro _F2")]
    public static void ToggleAggro()
    {
        var s = EnsureSettings(); if (!s) return;
        Toggle(ref s.showAggro);
    }

    // F3: Attack circle
    [MenuItem("Debug/Gizmos/Toggle Attack _F3")]
    public static void ToggleAttack()
    {
        var s = EnsureSettings(); if (!s) return;
        Toggle(ref s.showAttack);
    }

    // F3: Attack circle
    [MenuItem("Debug/Gizmos/Toggle View Circle ")]
    public static void ToggleViewCircle()
    {
        var s = EnsureSettings(); if (!s) return;
        Toggle(ref s.showViewCircle);
    }

    // F4: Gather Ring
    [MenuItem("Debug/Gizmos/Toggle Gather Ring _F4")]
    public static void ToggleGatherRing()
    {
        var s = EnsureSettings(); if (!s) return;
        Toggle(ref s.showGatherRing);
    }

    // F5: Slot Target
    [MenuItem("Debug/Gizmos/Toggle Slot Target _F5")]
    public static void ToggleSlotTarget()
    {
        var s = EnsureSettings(); if (!s) return;
        Toggle(ref s.showSlotTarget);
    }

    // F6: Separation Radius
    [MenuItem("Debug/Gizmos/Toggle Separation _F6")]
    public static void ToggleSeparation()
    {
        var s = EnsureSettings(); if (!s) return;
        Toggle(ref s.showSeparationRadius);
    }

    // F7: Attack Range
    [MenuItem("Debug/Gizmos/Toggle Attack Range _F7")]
    public static void ToggleAttackRange()
    {
        var s = EnsureSettings(); if (!s) return;
        Toggle(ref s.showAttackRange);
    }

    // F8: Steering Vector
    [MenuItem("Debug/Gizmos/Toggle Steering _F8")]
    public static void ToggleSteering()
    {
        var s = EnsureSettings(); if (!s) return;
        Toggle(ref s.showSteeringVector);
    }
}
#endif
