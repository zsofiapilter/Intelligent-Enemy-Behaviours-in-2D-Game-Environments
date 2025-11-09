using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    [Header("Menu Scene Settings")]
    [SerializeField] private string menuSceneName = "Menu";
    public GameObject menuPanel;

    private bool isMenuOpen = false;

    void Start()
    {
        ApplySceneMenuState(SceneManager.GetActiveScene());
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleMenu();
        }
    }

    private void ToggleMenu()
    {
        isMenuOpen = !isMenuOpen;
        if (menuPanel) menuPanel.SetActive(isMenuOpen);

        Time.timeScale = isMenuOpen ? 0f : 1f;

        if (isMenuOpen)
            DebugPanel.CloseAll();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        isMenuOpen = false;
        Time.timeScale = 1f;

        if (menuPanel) menuPanel.SetActive(false);
    }

    private void ApplySceneMenuState(Scene scene)
    {
        if (scene.name == menuSceneName)
        {
            isMenuOpen = true;
            if (menuPanel) menuPanel.SetActive(true);
            Time.timeScale = 0f;

            DebugPanel.CloseAll();
        }
        else
        {
            isMenuOpen = false;
            if (menuPanel) menuPanel.SetActive(false);
            Time.timeScale = 1f;
        }
    }

    public void SelectEnemy(string enemyName)
    {
        isMenuOpen = false;
        Time.timeScale = 1f;
        if (menuPanel) menuPanel.SetActive(false);

        GameData.selectedEnemy = enemyName;

        if (GameData.selectedEnemy == "Pig-los")
            SceneManager.LoadScene("Line of sight");
        else if (GameData.selectedEnemy == "Pig-bresenham")
            SceneManager.LoadScene("Bresenham");
        else if (GameData.selectedEnemy == "Turtle")
            SceneManager.LoadScene("Multiplayer");
        else if (GameData.selectedEnemy == "Rinos" || GameData.selectedEnemy == "Duck")
            SceneManager.LoadScene("Boids");
        else if (GameData.selectedEnemy == "Bee")
            SceneManager.LoadScene("GOAP");
    }

    public void ResumeGame()
    {
        isMenuOpen = false;
        if (menuPanel) menuPanel.SetActive(false);
        Time.timeScale = 1f;
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
