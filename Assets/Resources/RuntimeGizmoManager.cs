using UnityEngine;

public class RuntimeGizmoManager : MonoBehaviour
{
    public static RuntimeGizmoManager Instance { get; private set; }
    public EnemyGizmoSettings settings;
    public KeyCode toggleKey = KeyCode.F2;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        if (settings == null) settings = EnemyGizmoSettings.Instance;
    }

    void Update()
    {
        if (settings == null) return;
        if (Input.GetKeyDown(toggleKey))
            settings.runtimeEnabled = !settings.runtimeEnabled;
    }
}
