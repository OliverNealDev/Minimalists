using UnityEngine;
using UnityEngine.Rendering;

public class MainMenuManager : MonoBehaviour
{
    // Assign these panels in the Unity Inspector
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject levelSelectPanel;
    [SerializeField] private GameObject optionsPanel;
    [SerializeField] private GameObject statisticsPanel;
    [SerializeField] private Volume volume;

    void Start()
    {
        ShowMainMenu();
    }

    public void HideAllPanels()
    {
        mainMenuPanel.SetActive(false);
        levelSelectPanel.SetActive(false);
        optionsPanel.SetActive(false);
        statisticsPanel.SetActive(false);
    }

    public void ShowMainMenu()
    {
        HideAllPanels();
        mainMenuPanel.SetActive(true);
    }

    public void ShowLevelSelect()
    {
        HideAllPanels();
        levelSelectPanel.SetActive(true);
    }

    public void ShowOptions()
    {
        HideAllPanels();
        optionsPanel.SetActive(true);
    }
    
    public void ShowStatistics()
    {
        HideAllPanels();
        statisticsPanel.SetActive(true);
    }
    
    public void LoadLevel(int levelIndex)
    {
        // Load the specified level by its index
        
        UnityEngine.SceneManagement.SceneManager.LoadScene("Level_" + levelIndex);
    }
}
