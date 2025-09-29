using UnityEngine;
using UnityEngine.UI;

public class GameOverUI : MonoBehaviour
{
    [Header("UI Elements")]
    public Text gameOverText;
    public Text restartText;
    public Button restartButton;
    
    void Start()
    {
        // Hide initially
        gameObject.SetActive(false);
        
        // Set up restart button if it exists
        if (restartButton != null)
        {
            restartButton.onClick.AddListener(RestartGame);
        }
        
        // Set default text if not assigned
        if (gameOverText != null && string.IsNullOrEmpty(gameOverText.text))
        {
            gameOverText.text = "Game Over!";
        }
        
        if (restartText != null && string.IsNullOrEmpty(restartText.text))
        {
            restartText.text = "Press R to Restart";
        }
    }
    
    void Update()
    {
        // Handle restart input
        if (Input.GetKeyDown(KeyCode.R))
        {
            RestartGame();
        }
    }
    
    public void RestartGame()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RestartGame();
        }
    }
}
