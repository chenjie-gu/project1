using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

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
    
    [Header("UI Prefabs")]
    public GameObject gameOverCanvasPrefab;
    public GameObject restartButtonPrefab;
    
    [Header("Tutorial")]
    public GameObject tutorialCanvasPrefab;
    public GameObject tutorialKeyPrefab;
    public Transform tutorialKeySpawnPoint;
    
    private bool isGameOver = false;
    private TextMeshProUGUI gameOverTextMeshPro;
    private TextMeshProUGUI restartTextMeshPro;
    private UnityEngine.UI.Button restartButton;
    
    public static GameManager Instance { get; private set; }
    
    void Awake()
    {
        Debug.Log("GameManager: Awake called");
        
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            SceneManager.sceneLoaded += OnSceneLoaded;
            Debug.Log("GameManager: Singleton created and sceneLoaded event subscribed");
        }
        else
        {
            Debug.Log("GameManager: Duplicate GameManager destroyed");
            Destroy(gameObject);
        }
    }
    
    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log("GameManager: Scene loaded - " + scene.name);
        ResetGameState();
        EnsureRestartButtonExists();
        
        // Start tutorial only in Tutorial level
        if (scene.name == "Tutorial")
        {
            Debug.Log("GameManager: In Tutorial level, starting tutorial");
            StartTutorial();
        }
        else
        {
            Debug.Log("GameManager: In " + scene.name + ", no tutorial needed");
        }
    }
    
    void ResetGameState()
    {
        isGameOver = false;
        Time.timeScale = 1f;
        
        // Show restart button
        if (restartButton != null)
        {
            restartButton.gameObject.SetActive(true);
        }
        
        if (gameOverTextMeshPro != null)
        {
            gameOverTextMeshPro.gameObject.SetActive(false);
        }
        
        if (restartTextMeshPro != null)
        {
            restartTextMeshPro.gameObject.SetActive(false);
        }
        
        var player = FindObjectOfType<PlayerMovement>();
        if (player != null)
        {
            player.enabled = true;
        }
    }
    
    void FindUIComponents()
    {
        Debug.Log("GameManager: FindUIComponents called");
        
        var allTexts = FindObjectsOfType<TextMeshProUGUI>();
        Debug.Log($"GameManager: Found {allTexts.Length} TextMeshProUGUI components");
        
        if (allTexts.Length == 0 && gameOverCanvasPrefab != null)
        {
            Debug.Log("GameManager: No TextMeshProUGUI found, creating from prefab");
            var canvasInstance = Instantiate(gameOverCanvasPrefab);
            DontDestroyOnLoad(canvasInstance);
            
            var texts = canvasInstance.GetComponentsInChildren<TextMeshProUGUI>();
            Debug.Log($"GameManager: Created canvas with {texts.Length} TextMeshProUGUI components");
            
            if (texts.Length > 0)
            {
                gameOverTextMeshPro = texts[0];
                gameOverTextMeshPro.text = gameOverText;
                gameOverTextMeshPro.gameObject.SetActive(false);
                Debug.Log("GameManager: Game over text component set");
            }
            if (texts.Length > 1)
            {
                restartTextMeshPro = texts[1];
                restartTextMeshPro.text = restartText;
                restartTextMeshPro.gameObject.SetActive(false);
                Debug.Log("GameManager: Restart text component set");
            }
            else if (texts.Length == 1)
            {
                restartTextMeshPro = texts[0];
                restartTextMeshPro.text = restartText;
                restartTextMeshPro.gameObject.SetActive(false);
                Debug.Log("GameManager: Restart text component set (using same as game over)");
            }
        }
        else if (allTexts.Length > 0)
        {
            Debug.Log("GameManager: Using existing TextMeshProUGUI components");
            if (gameOverTextMeshPro == null)
            {
                gameOverTextMeshPro = allTexts[0];
                Debug.Log("GameManager: Game over text component assigned from existing");
            }
            if (restartTextMeshPro == null)
            {
                restartTextMeshPro = allTexts.Length > 1 ? allTexts[1] : allTexts[0];
                Debug.Log("GameManager: Restart text component assigned from existing");
            }
        }
        else
        {
            Debug.LogError("GameManager: No TextMeshProUGUI components found and no gameOverCanvasPrefab assigned!");
        }
    }
    
    void Start()
    {
        Debug.Log("GameManager: Start called");
        CreateRestartButton();
        
        // Manual tutorial trigger since OnSceneLoaded might not be called
        StartCoroutine(CheckForTutorialAfterDelay());
    }
    
    private System.Collections.IEnumerator CheckForTutorialAfterDelay()
    {
        // Wait a frame to ensure everything is loaded
        yield return new WaitForEndOfFrame();
        
        string currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        Debug.Log("GameManager: Manual scene check - " + currentSceneName);
        
        // Start tutorial only in Tutorial level
        if (currentSceneName == "Tutorial")
        {
            Debug.Log("GameManager: In Tutorial level, starting tutorial");
            StartTutorial();
        }
        else
        {
            Debug.Log("GameManager: In " + currentSceneName + ", no tutorial needed");
        }
    }
    
    void CreateRestartButton()
    {
        // Don't create if button already exists
        if (restartButton != null && restartButton.gameObject != null) return;
        
        // Always create a dedicated Canvas for the restart button
        var canvasGO = new GameObject("Restart Button Canvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100; // High sorting order to ensure it's on top
        canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
        canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        DontDestroyOnLoad(canvasGO);
        
        // Ensure EventSystem exists for UI input
        var eventSystem = FindObjectOfType<UnityEngine.EventSystems.EventSystem>();
        if (eventSystem == null)
        {
            var eventSystemGO = new GameObject("EventSystem");
            eventSystemGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            DontDestroyOnLoad(eventSystemGO);
            Debug.Log("Created EventSystem for UI input");
        }
        else
        {
            Debug.Log("EventSystem already exists");
        }
        
        // Create restart button
        var buttonGO = new GameObject("Restart Button");
        buttonGO.transform.SetParent(canvas.transform, false);
        
        restartButton = buttonGO.AddComponent<UnityEngine.UI.Button>();
        var image = buttonGO.AddComponent<UnityEngine.UI.Image>();
        image.color = new Color(0.2f, 0.2f, 0.2f, 0.8f); // Dark gray with transparency
        
        // Disable space key navigation to prevent accidental restarts
        var navigation = restartButton.navigation;
        navigation.mode = UnityEngine.UI.Navigation.Mode.None;
        restartButton.navigation = navigation;
        
        // Position button in top right corner
        var rectTransform = buttonGO.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(1, 1);
        rectTransform.anchorMax = new Vector2(1, 1);
        rectTransform.anchoredPosition = new Vector2(-100, -60); // Offset from top-right corner
        rectTransform.sizeDelta = new Vector2(100, 40); // Make it bigger for testing
        
        // Add text to button
        var textGO = new GameObject("Text");
        textGO.transform.SetParent(buttonGO.transform, false);
        var text = textGO.AddComponent<TextMeshProUGUI>();
        text.text = "Restart";
        text.fontSize = 16;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        
        var textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        
        // Add click listener
        restartButton.onClick.AddListener(() => {
            RestartGame();
        });
        
        
        // Make button persist across scenes
        DontDestroyOnLoad(buttonGO);
        
        // Ensure button is not focused by default to prevent space key activation
        UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(null);
        
        Debug.Log($"Restart button created successfully in scene: {SceneManager.GetActiveScene().name}");
    }
    
    void EnsureRestartButtonExists()
    {
        // Check if restart button still exists and is functional
        if (restartButton == null || restartButton.gameObject == null)
        {
            Debug.Log("Restart button missing, recreating...");
            restartButton = null; // Reset reference
            CreateRestartButton();
        }
        else
        {
            Debug.Log($"Restart button exists and is functional in scene: {SceneManager.GetActiveScene().name}");
        }
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
            Debug.Log("No next level scene name or index set!");
        }
    }
    
    public void GameOver()
    {
        Debug.Log("GameManager: GameOver called");
        
        if (isGameOver) 
        {
            Debug.Log("GameManager: Already game over, ignoring");
            return;
        }
        
        isGameOver = true;
        Debug.Log("GameManager: Game over state set to true");
        
        // Hide restart button during game over
        if (restartButton != null)
        {
            restartButton.gameObject.SetActive(false);
            Debug.Log("GameManager: Restart button hidden");
        }
        else
        {
            Debug.LogWarning("GameManager: Restart button is null");
        }
        
        if (gameOverTextMeshPro == null || restartTextMeshPro == null)
        {
            Debug.Log("GameManager: UI components missing, trying to find them");
            FindUIComponents();
        }
        
        if (gameOverTextMeshPro != null)
        {
            Debug.Log("GameManager: Showing game over text");
            gameOverTextMeshPro.transform.root.gameObject.SetActive(true);
            gameOverTextMeshPro.gameObject.SetActive(true);
        }
        else
        {
            Debug.LogError("GameManager: gameOverTextMeshPro is null! Cannot show game over text.");
        }
        
        if (restartTextMeshPro != null)
        {
            Debug.Log("GameManager: Showing restart text");
            restartTextMeshPro.gameObject.SetActive(true);
        }
        else
        {
            Debug.LogError("GameManager: restartTextMeshPro is null! Cannot show restart text.");
        }
        
        var player = FindObjectOfType<PlayerMovement>();
        if (player != null)
        {
            player.enabled = false;
            Debug.Log("GameManager: Player movement disabled");
        }
        
        Time.timeScale = 0f;
        Debug.Log("GameManager: Time scale set to 0");
    }
    
    public void RestartGame()
    {
        Debug.Log("RestartGame called");
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    
    public void StartNewGame()
    {
        Debug.Log("StartNewGame called");
        SceneManager.LoadScene("Level1");
    }
    
    [ContextMenu("Force Start Tutorial")]
    public void ForceStartTutorial()
    {
        Debug.Log("GameManager: Force starting tutorial");
        StartTutorial();
    }
    
    public bool IsGameOver()
    {
        return isGameOver;
    }
    
    private void StartTutorial()
    {
        Debug.Log("GameManager: StartTutorial called");
        
        if (tutorialCanvasPrefab != null)
        {
            Debug.Log("GameManager: Creating tutorial canvas from prefab");
            GameObject tutorialCanvas = Instantiate(tutorialCanvasPrefab);
            TutorialManager tutorialManager = tutorialCanvas.GetComponent<TutorialManager>();
            
            if (tutorialManager != null)
            {
                Debug.Log("GameManager: TutorialManager found, setting references");
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

