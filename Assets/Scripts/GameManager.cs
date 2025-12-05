using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class GameManager : MonoBehaviour
{
    [Header("Game Over")]
    public string gameOverText = "Game Over!";
    public string restartText = "Press R to Restart";
    
    [Header("Level Progression")]
    public bool autoDetectNextLevel = true;
    public string nextLevelSceneName = "Level2";
    public int nextLevelSceneIndex = 1; // Alternative: use scene index instead of name
    public bool useSceneIndex = false; // Toggle between name and index
    public int finalLevel = 4; // Level at which the game ends
    
    [Header("UI Prefabs")]
    public GameObject uiCanvasPrefab; // Main UI canvas with restart button and foreground
    
    [Header("Tutorial")]
    public GameObject tutorialCanvasPrefab;
    public GameObject tutorialKeyPrefab;
    public Transform tutorialKeySpawnPoint;
    
    private bool isGameOver = false;
    private UnityEngine.UI.Button restartButton;
    private GameObject uiCanvasInstance; // Reference to the instantiated UI canvas
    private TextMeshProUGUI gameOverTextMeshPro; // Game over text display
    private TextMeshProUGUI restartTextMeshPro; // Restart instruction text
    
    public static GameManager Instance { get; private set; }
    
    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ResetGameState();
        EnsureUICanvasExists();
        
        if (scene.name == "Tutorial")
        {
            StartTutorial();
        }
    }
    
    void ResetGameState()
    {
        isGameOver = false;
        Time.timeScale = 1f;
        
        // Hide game over UI
        if (gameOverTextMeshPro != null)
        {
            gameOverTextMeshPro.gameObject.SetActive(false);
        }
        
        if (restartTextMeshPro != null)
        {
            restartTextMeshPro.gameObject.SetActive(false);
        }
        
        // Keep restart button visible at all times and ensure it's connected
        if (restartButton != null)
        {
            restartButton.gameObject.SetActive(true);
            restartButton.interactable = true;
            
            // Re-setup the onClick listener to ensure it works
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(() => {
                RestartGame();
            });
        }
        else
        {
            // If button is null, try to find it again
            EnsureUICanvasExists();
        }
        
        var player = FindObjectOfType<PlayerMovement>();
        if (player != null)
        {
            player.enabled = true;
        }
    }
    
    void Start()
    {
        // Create UI canvas
        EnsureUICanvasExists();
        
        // Manual tutorial trigger since OnSceneLoaded might not be called
        StartCoroutine(CheckForTutorialAfterDelay());
    }
    
    private System.Collections.IEnumerator CheckForTutorialAfterDelay()
    {
        // Wait a frame to ensure everything is loaded
        yield return new WaitForEndOfFrame();
        
        string currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        
        // Start tutorial only in Tutorial level
        if (currentSceneName == "Tutorial")
        {
            // Tutorial already started in OnSceneLoaded
        }
    }
    
    void EnsureUICanvasExists()
    {
        if (uiCanvasInstance == null)
        {
            CreateUICanvas();
        }
    }
    
    void CreateUICanvas()
    {
        if (uiCanvasPrefab == null)
        {
            Debug.LogError("GameManager: No uiCanvasPrefab assigned!");
            return;
        }
        
        uiCanvasInstance = Instantiate(uiCanvasPrefab);
        
        // Find the restart button by name
        var allButtons = uiCanvasInstance.GetComponentsInChildren<UnityEngine.UI.Button>();
        
        foreach (var button in allButtons)
        {
            if (button.name.ToLower().Contains("restart") || 
                button.name.ToLower().Contains("retry") ||
                button.name.ToLower().Contains("play"))
            {
                restartButton = button;
                break;
            }
        }
        
        // Fallback: use first button if not found by name
        if (restartButton == null && allButtons.Length > 0)
        {
            restartButton = allButtons[0];
        }
        
        if (restartButton == null)
        {
            Debug.LogError("GameManager: No Button component found in UICanvas prefab!");
            return;
        }
        
        // Find game over text elements by name
        var allTextComponents = uiCanvasInstance.GetComponentsInChildren<TextMeshProUGUI>();
        
        // Look for game over text by name
        foreach (var textComp in allTextComponents)
        {
            if (textComp.name.ToLower().Contains("gameover") || 
                textComp.name.ToLower().Contains("game_over") ||
                textComp.name.ToLower().Contains("game over"))
            {
                gameOverTextMeshPro = textComp;
                gameOverTextMeshPro.text = gameOverText;
                gameOverTextMeshPro.gameObject.SetActive(false); // Hide by default
                break;
            }
        }
        
        // Look for restart instruction text by name
        foreach (var textComp in allTextComponents)
        {
            if (textComp.name.ToLower().Contains("restart") || 
                textComp.name.ToLower().Contains("instruction") ||
                textComp.name.ToLower().Contains("press"))
            {
                restartTextMeshPro = textComp;
                restartTextMeshPro.text = restartText;
                restartTextMeshPro.gameObject.SetActive(false); // Hide by default
                break;
            }
        }
        
        // Fallback: if not found by name, use first/second components
        if (gameOverTextMeshPro == null && allTextComponents.Length > 0)
        {
            gameOverTextMeshPro = allTextComponents[0];
            gameOverTextMeshPro.text = gameOverText;
            gameOverTextMeshPro.gameObject.SetActive(false);
        }
        
        if (restartTextMeshPro == null && allTextComponents.Length > 1)
        {
            restartTextMeshPro = allTextComponents[1];
            restartTextMeshPro.text = restartText;
            restartTextMeshPro.gameObject.SetActive(false);
        }
        else if (restartTextMeshPro == null && allTextComponents.Length == 1)
        {
            restartTextMeshPro = allTextComponents[0];
        }
        
        // Clear any existing listeners and add click listener
        restartButton.onClick.RemoveAllListeners();
        restartButton.onClick.AddListener(() => {
            RestartGame();
        });
        
        // Ensure button is interactable and always visible
        restartButton.interactable = true;
        restartButton.gameObject.SetActive(true); // Always visible
        
        // Make UI canvas persist across scenes
        DontDestroyOnLoad(uiCanvasInstance);
        
        // Force UI canvas to be visible
        uiCanvasInstance.SetActive(true);
    }
    
    
    void Update()
    {
        if (isGameOver && Input.GetKeyDown(KeyCode.R))
        {
            RestartGame();
        }
        
        if (!isGameOver)
        {
            CheckLevelCompletion();
        }
    }
    
    
    void CheckLevelCompletion()
    {
        // Find all doors in the scene
        var doors = FindObjectsOfType<Door>();
        
        if (doors.Length == 0) return;
        
        // Check if all doors are open
        bool allDoorsOpen = true;
        foreach (var door in doors)
        {
            if (!door.IsOpen())
            {
                allDoorsOpen = false;
                break;
            }
        }
        
        // If all doors are open, load next level
        if (allDoorsOpen)
        {
            LoadNextLevel();
        }
    }
    
    void LoadNextLevel()
    {
        // Disable player movement
        var player = FindObjectOfType<PlayerMovement>();
        if (player != null)
        {
            player.enabled = false;
        }
        
        // Determine which scene to load
        string sceneToLoad = null;
        int sceneIndexToLoad = -1;
        
        if (autoDetectNextLevel)
        {
            // Auto-detect next level based on current scene
            var currentScene = SceneManager.GetActiveScene();
            var currentSceneName = currentScene.name;
            
            if (currentSceneName.StartsWith("Level"))
            {
                // Extract level number and increment
                if (int.TryParse(currentSceneName.Substring(5), out int currentLevel))
                {
                    // Check if this is the final level - if so, exit the game
                    if (currentLevel == finalLevel)
                    {
                        ExitGame();
                        return;
                    }
                    
                    int nextLevel = currentLevel + 1;
                    sceneToLoad = $"Level{nextLevel}";
                    sceneIndexToLoad = currentScene.buildIndex + 1;
                }
            }
        }
        
        // Use manual settings if auto-detect didn't work or is disabled
        if (string.IsNullOrEmpty(sceneToLoad))
        {
            sceneToLoad = nextLevelSceneName;
            sceneIndexToLoad = nextLevelSceneIndex;
        }
        
        // Load the next level
        if (useSceneIndex && sceneIndexToLoad >= 0)
        {
            try
            {
                SceneManager.LoadScene(sceneIndexToLoad);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to load scene at index {sceneIndexToLoad}: {e.Message}");
                Debug.LogError("Please add the scene to Build Settings: File -> Build Settings -> Add Open Scenes");
                RestartGame();
            }
        }
        else if (!string.IsNullOrEmpty(sceneToLoad))
        {
            try
            {
                SceneManager.LoadScene(sceneToLoad);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to load scene '{sceneToLoad}': {e.Message}");
                Debug.LogError("Please add the scene to Build Settings: File -> Build Settings -> Add Open Scenes");
                RestartGame();
            }
        }
        else
        {
            Debug.LogWarning("GameManager: No next level scene name or index set!");
        }
    }
    
    public void GameOver()
    {
        if (isGameOver) 
        {
            return;
        }
        
        isGameOver = true;
        
        // Show game over UI
        if (gameOverTextMeshPro != null)
        {
            gameOverTextMeshPro.gameObject.SetActive(true);
        }
        else
        {
            Debug.LogWarning("GameManager: Game over text component not found!");
        }
        
        if (restartTextMeshPro != null)
        {
            restartTextMeshPro.gameObject.SetActive(true);
        }
        else
        {
            Debug.LogWarning("GameManager: Restart text component not found!");
        }
        
        // Show restart button
        if (restartButton != null)
        {
            // Ensure button and its parent are active
            restartButton.gameObject.SetActive(true);
            restartButton.interactable = true;
            
            // Ensure parent canvas is active
            if (uiCanvasInstance != null)
            {
                uiCanvasInstance.SetActive(true);
            }
        }
        else
        {
            Debug.LogWarning("GameManager: Restart button not found!");
        }
        
        var player = FindObjectOfType<PlayerMovement>();
        if (player != null)
        {
            player.enabled = false;
        }
        
        // Delay pausing to allow sounds to start playing
        StartCoroutine(DelayedPause());
    }
    
    IEnumerator DelayedPause()
    {
        // Wait a short time to allow sounds to start playing
        yield return new WaitForSeconds(0.1f);
        Time.timeScale = 0f;
    }
    
    public void RestartGame()
    {
        // Ensure time scale is reset before loading scene
        Time.timeScale = 1f;
        
        // Load the current scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    
    public void StartNewGame()
    {
        SceneManager.LoadScene("Level1");
    }
    
    public void ExitGame()
    {
        // Exit the application
        #if UNITY_EDITOR
            // In editor, stop playing
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            // In build, quit application
            Application.Quit();
        #endif
    }
    
    public bool IsGameOver()
    {
        return isGameOver;
    }
    
    private void StartTutorial()
    {
        // Check if tutorial is already running to prevent duplicates
        if (FindObjectOfType<TutorialManager>() != null)
        {
            return;
        }
        
        if (tutorialCanvasPrefab != null)
        {
            GameObject tutorialCanvas = Instantiate(tutorialCanvasPrefab);
            TutorialManager tutorialManager = tutorialCanvas.GetComponent<TutorialManager>();
            
            if (tutorialManager != null)
            {
                // Set tutorial key prefab and spawn point
                tutorialManager.tutorialKeyPrefab = tutorialKeyPrefab;
                tutorialManager.keySpawnPoint = tutorialKeySpawnPoint;
            }
            else
            {
                Debug.LogError("GameManager: TutorialManager component not found on tutorial canvas prefab!");
            }
        }
        else
        {
            Debug.LogError("GameManager: tutorialCanvasPrefab is null! Please assign a tutorial canvas prefab.");
        }
    }
}

