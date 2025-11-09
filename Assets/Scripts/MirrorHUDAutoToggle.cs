using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using Mirror;

public class MirrorHUDAutoToggle : MonoBehaviour
{
    [Tooltip("Mirror HUD can be seen")]
    public string[] multiplayerScenes = { "Multiplayer" };

    [Header("Right-side anchoring and scaling")]
    [Range(0f, 1f)] public float screenWidthFraction = 0.3f;
    [Range(0f, 1f)] public float screenHeightFraction = 0.4f;
    public int margin = 10;

    NetworkManagerHUD hud;
    Rect hudRect;
    Vector2Int lastScreenSize;

    void Awake()
    {
        hud = GetComponent<NetworkManagerHUD>();
        SceneManager.sceneLoaded += OnSceneLoaded;
        OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        bool isMultiplayer = Array.Exists(multiplayerScenes, n => n == scene.name);
        if (hud) hud.enabled = isMultiplayer;
        if (isMultiplayer) UpdateHudLayout();
    }

    void LateUpdate()
    {
        if (!hud || !hud.enabled) return;

        if (Screen.width != lastScreenSize.x || Screen.height != lastScreenSize.y)
            UpdateHudLayout();
    }

    void UpdateHudLayout()
    {
        lastScreenSize = new Vector2Int(Screen.width, Screen.height);

        float width = Mathf.Max(150f, Screen.width * screenWidthFraction);
        float height = Mathf.Max(200f, Screen.height * screenHeightFraction);

        hud.offsetX = Mathf.RoundToInt(Screen.width - width - margin);
        hud.offsetY = margin;
    }
}
