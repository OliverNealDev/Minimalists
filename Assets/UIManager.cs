// In Scripts/UI/UIManager.cs
using UnityEngine;
using UnityEngine.SceneManagement; // Needed to reload the scene or go to the main menu

public class UIManager : MonoBehaviour
{
    private GameManager gameManager;
    
    // Assign these panels in the Unity Inspector
    [SerializeField] private GameObject pauseMenuPanel;
    [SerializeField] private GameObject winScreenPanel;
    [SerializeField] private GameObject loseScreenPanel;

    void Start()
    {
        gameManager = FindFirstObjectByType<GameManager>();
        
        // Ensure all panels are hidden when the game starts
        HideAllPanels();
    }

    public void HideAllPanels()
    {
        pauseMenuPanel.SetActive(false);
        winScreenPanel.SetActive(false);
        loseScreenPanel.SetActive(false);
    }

    public void ShowPausePanel(bool show)
    {
        pauseMenuPanel.SetActive(show);
    }

    public void ShowWinPanel()
    {
        HideAllPanels();
        winScreenPanel.SetActive(true);
    }

    public void ShowLosePanel()
    {
        HideAllPanels();
        loseScreenPanel.SetActive(true);
    }

    // These are example methods you would link to your UI buttons
    public void ResumeGame()
    {
        ShowPausePanel(false);
        gameManager.currentState = GameManager.GameState.Playing;
        Time.timeScale = 1f; // Resume the game
    }

    public void NextLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    public void RestartLevel()
    {
        Time.timeScale = 1f; // Always reset time scale when changing scenes
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu"); // The name of your main menu scene
    }
}