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
        
        // Keep restart button visible - it should always be available
        if (restartButton != null)
        {
            restartButton.gameObject.SetActive(true);
        }
        
        var player = FindObjectOfType<PlayerMovement>();
        if (player != null)
        {
            player.enabled = true;
        }
    }
    
    void Start()
    {
        EnsureUICanvasExists();
        StartCoroutine(CheckForTutorialAfterDelay());
    }
    
    private System.Collections.IEnumerator CheckForTutorialAfterDelay()
    {
        yield return new WaitForEndOfFrame();
        
        string currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        
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
        
        Debug.Log($"GameManager: Creating UI canvas from prefab (Scene: {SceneManager.GetActiveScene().name})");
        uiCanvasInstance = Instantiate(uiCanvasPrefab);
        Debug.Log($"GameManager: UI canvas instantiated: {uiCanvasInstance != null}");
        
        // Find the restart button by name
        var allButtons = uiCanvasInstance.GetComponentsInChildren<UnityEngine.UI.Button>();
        Debug.Log($"GameManager: Found {allButtons.Length} Button components in UICanvas (Scene: {SceneManager.GetActiveScene().name})");
        
        // Find restart button by exact name
        foreach (var button in allButtons)
        {
            Debug.Log($"GameManager: Checking button: '{button.name}'");
            if (button.name == "RestartButton")
            {
                restartButton = button;
                Debug.Log($"GameManager: ✓ Found RestartButton: '{button.name}'");
                break;
            }
        }
        
        // Fallback: use first button if not found by name
        if (restartButton == null && allButtons.Length > 0)
        {
            restartButton = allButtons[0];
            Debug.Log($"GameManager: Using first button as restart button (fallback): {restartButton.name}");
        }
        
        if (restartButton == null)
        {
            Debug.LogError("GameManager: No Button component found in UICanvas prefab!");
            return;
        }
        
        // Find game over text elements by name
        var allTextComponents = uiCanvasInstance.GetComponentsInChildren<TextMeshProUGUI>();
        
        // Find text components by exact name
        foreach (var textComp in allTextComponents)
        {
            Debug.Log($"GameManager: Found text component: '{textComp.name}'");
            if (textComp.name == "GameOverText")
            {
                gameOverTextMeshPro = textComp;
                // Don't change text content, just hide the GameObject
                gameOverTextMeshPro.gameObject.SetActive(false);
                Debug.Log($"GameManager: ✓ Assigned GameOverText: '{gameOverTextMeshPro.name}'");
            }
            else if (textComp.name == "RestartText")
            {
                restartTextMeshPro = textComp;
                // Don't change text content, just hide the GameObject
                restartTextMeshPro.gameObject.SetActive(false);
                Debug.Log($"GameManager: ✓ Assigned RestartText: '{restartTextMeshPro.name}'");
            }
        }
        
        // Clear any existing listeners and add click listener
        restartButton.onClick.RemoveAllListeners();
        restartButton.onClick.AddListener(() => {
            Debug.Log($"GameManager: Restart button clicked in scene: {SceneManager.GetActiveScene().name}!");
            RestartGame();
        });
        
        Debug.Log($"GameManager: Click listener added to restart button: {restartButton.name}");
        
        // Make UI canvas persist across scenes
        DontDestroyOnLoad(uiCanvasInstance);
        
        // Force UI canvas to be visible
        uiCanvasInstance.SetActive(true);
        
        // Ensure restart button is visible (it should always be available)
        restartButton.gameObject.SetActive(true);
        
        Debug.Log($"UI canvas created from prefab successfully in scene: {SceneManager.GetActiveScene().name}");
        Debug.Log($"Restart button found: {restartButton.name}");
        Debug.Log($"Restart button interactable: {restartButton.interactable}");
        Debug.Log($"Restart button enabled: {restartButton.enabled}");
        Debug.Log($"Restart button gameObject active: {restartButton.gameObject.activeInHierarchy}");
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
            Debug.Log("No next level scene name or index set!");
        }
    }
    
    public void GameOver()
    {
        if (isGameOver) return;
        
        isGameOver = true;
        
        // Debug: Check what components we have
        Debug.Log($"GameManager: GameOver called - gameOverTextMeshPro: {gameOverTextMeshPro?.name}, restartTextMeshPro: {restartTextMeshPro?.name}");
        
        // Show game over UI
        if (gameOverTextMeshPro != null)
        {
            Debug.Log($"GameManager: Showing game over text: {gameOverTextMeshPro.name}");
            gameOverTextMeshPro.gameObject.SetActive(true);
        }
        else
        {
            Debug.LogWarning("GameManager: Game over text component not found!");
        }
        
        if (restartTextMeshPro != null)
        {
            Debug.Log($"GameManager: Showing restart text: {restartTextMeshPro.name}");
            restartTextMeshPro.gameObject.SetActive(true);
        }
        else
        {
            Debug.LogWarning("GameManager: Restart text component not found!");
        }
        
        // Show restart button
        if (restartButton != null)
        {
            Debug.Log($"GameManager: Showing restart button: {restartButton.name}");
            restartButton.gameObject.SetActive(true);
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
        
        Time.timeScale = 0f;
    }
    
    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    
    public void StartNewGame()
    {
        SceneManager.LoadScene("Level1");
    }
    
    public void ExitGame()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
    
    [ContextMenu("Force Start Tutorial")]
    public void ForceStartTutorial()
    {
        Debug.Log("GameManager: Force starting tutorial");
        StartTutorial();
    }
    
    [ContextMenu("Test Game Exit")]
    public void TestGameExit()
    {
        Debug.Log("GameManager: Testing game exit functionality...");
        ExitGame();
    }
    
    [ContextMenu("Test Game Over UI")]
    public void TestGameOverUI()
    {
        Debug.Log("GameManager: Testing game over UI...");
        
        if (uiCanvasInstance == null)
        {
            Debug.LogError("GameManager: UI Canvas instance is null!");
            return;
        }
        
        Debug.Log($"GameManager: UI Canvas active: {uiCanvasInstance.activeInHierarchy}");
        Debug.Log($"GameManager: Game over text found: {gameOverTextMeshPro != null}");
        Debug.Log($"GameManager: Restart text found: {restartTextMeshPro != null}");
        Debug.Log($"GameManager: Restart button found: {restartButton != null}");
        
        if (gameOverTextMeshPro != null)
        {
            Debug.Log($"GameManager: Game over text active: {gameOverTextMeshPro.gameObject.activeInHierarchy}");
            Debug.Log($"GameManager: Game over text content: {gameOverTextMeshPro.text}");
        }
        
        if (restartTextMeshPro != null)
        {
            Debug.Log($"GameManager: Restart text active: {restartTextMeshPro.gameObject.activeInHierarchy}");
            Debug.Log($"GameManager: Restart text content: {restartTextMeshPro.text}");
        }
        
        if (restartButton != null)
        {
            Debug.Log($"GameManager: Restart button active: {restartButton.gameObject.activeInHierarchy}");
        }
        
        // Manually trigger game over to test
        GameOver();
    }
    
    [ContextMenu("Show Restart Button")]
    public void ShowRestartButton()
    {
        if (restartButton != null)
        {
            restartButton.gameObject.SetActive(true);
        }
    }
    
    [ContextMenu("Fix Text Component Names")]
    public void FixTextComponentNames()
    {
        if (uiCanvasInstance == null) return;
        
        var allTextComponents = uiCanvasInstance.GetComponentsInChildren<TextMeshProUGUI>();
        
        for (int i = 0; i < allTextComponents.Length; i++)
        {
            var textComp = allTextComponents[i];
            string newName = "";
            
            if (i == 0)
            {
                newName = "GameOverText";
            }
            else if (i == 1)
            {
                newName = "RestartText";
            }
            else
            {
                newName = $"TextComponent{i}";
            }
            
            textComp.name = newName;
            
            // Set appropriate text content
            if (i == 0)
            {
                textComp.text = gameOverText;
            }
            else if (i == 1)
            {
                textComp.text = restartText;
            }
        }
    }
    
    [ContextMenu("Debug Restart Button Only")]
    public void DebugRestartButtonOnly()
    {
        Debug.Log("=== RESTART BUTTON DEBUG ===");
        
        if (restartButton == null)
        {
            Debug.LogError("GameManager: Restart button is null!");
            return;
        }
        
        var buttonGO = restartButton.gameObject;
        Debug.Log($"GameManager: Button GameObject: {buttonGO.name}");
        Debug.Log($"GameManager: Button active: {buttonGO.activeInHierarchy}");
        Debug.Log($"GameManager: Button enabled: {restartButton.enabled}");
        Debug.Log($"GameManager: Button interactable: {restartButton.interactable}");
        
        // Check Button's Image component
        var image = buttonGO.GetComponent<UnityEngine.UI.Image>();
        if (image == null)
        {
            Debug.LogError("GameManager: No Image component on button!");
        }
        else
        {
            Debug.Log($"GameManager: Image raycastTarget: {image.raycastTarget}");
            Debug.Log($"GameManager: Image enabled: {image.enabled}");
        }
        
        // Check click listeners
        Debug.Log($"GameManager: Button has {restartButton.onClick.GetPersistentEventCount()} persistent listeners");
        
        Debug.Log("=== END RESTART BUTTON DEBUG ===");
    }
    
    [ContextMenu("Test Final Level Issue")]
    public void TestFinalLevelIssue()
    {
        Debug.Log($"GameManager: Current scene: {SceneManager.GetActiveScene().name}");
        Debug.Log($"GameManager: Final level setting: {finalLevel}");
        
        string currentSceneName = SceneManager.GetActiveScene().name;
        if (currentSceneName.StartsWith("Level"))
        {
            if (int.TryParse(currentSceneName.Substring(5), out int currentLevel))
            {
                Debug.Log($"GameManager: Current level: {currentLevel}");
                Debug.Log($"GameManager: Is current level == final level? {currentLevel == finalLevel}");
                
                if (currentLevel == finalLevel)
                {
                    Debug.Log("GameManager: This is the final level - game would exit on completion");
                }
                else
                {
                    Debug.Log($"GameManager: This is not the final level - {finalLevel - currentLevel} levels remaining");
                }
            }
        }
        
        // Test if restart button works when we temporarily change final level
        Debug.Log("GameManager: Temporarily changing final level to 5...");
        int originalFinalLevel = finalLevel;
        finalLevel = 5;
        
        Debug.Log("GameManager: Try clicking the restart button now...");
        
        // Restore after 5 seconds
        StartCoroutine(RestoreFinalLevel(originalFinalLevel));
    }
    
    private System.Collections.IEnumerator RestoreFinalLevel(int originalValue)
    {
        yield return new WaitForSeconds(5f);
        finalLevel = originalValue;
        Debug.Log($"GameManager: Restored final level to {originalValue}");
    }
    
    [ContextMenu("Manually Trigger Restart")]
    public void ManuallyTriggerRestart()
    {
        Debug.Log("GameManager: Manually triggering restart...");
        RestartGame();
    }
    
    [ContextMenu("Fix Restart Button Only")]
    public void FixRestartButtonOnly()
    {
        Debug.Log("GameManager: Fixing restart button only...");
        
        // Re-find the restart button if it's null
        if (restartButton == null && uiCanvasInstance != null)
        {
            Debug.Log("GameManager: Restart button is null, re-finding it...");
            var allButtons = uiCanvasInstance.GetComponentsInChildren<UnityEngine.UI.Button>();
            foreach (var button in allButtons)
            {
                if (button.name == "RestartButton")
                {
                    restartButton = button;
                    Debug.Log($"GameManager: ✓ Re-found RestartButton: '{button.name}'");
                    break;
                }
            }
        }
        
        if (restartButton == null)
        {
            Debug.LogError("GameManager: Restart button is still null!");
            return;
        }
        
        var buttonGO = restartButton.gameObject;
        
        // Ensure button is active and enabled
        buttonGO.SetActive(true);
        restartButton.enabled = true;
        restartButton.interactable = true;
        
        // Ensure Image component has raycastTarget enabled
        var image = buttonGO.GetComponent<UnityEngine.UI.Image>();
        if (image != null)
        {
            image.raycastTarget = true;
            image.enabled = true;
        }
        
        // Re-add click listener
        restartButton.onClick.RemoveAllListeners();
        restartButton.onClick.AddListener(() => {
            Debug.Log($"GameManager: Restart button clicked (fixed)!");
            RestartGame();
        });
        
        Debug.Log("GameManager: Restart button fixed!");
    }
    
    [ContextMenu("Debug UI Interaction Issues")]
    public void DebugUIInteractionIssues()
    {
        Debug.Log("=== UI INTERACTION DEBUG ===");
        
        // Check EventSystem
        var eventSystem = UnityEngine.EventSystems.EventSystem.current;
        if (eventSystem == null)
        {
            Debug.LogError("GameManager: No EventSystem found! This will prevent UI interaction.");
        }
        else
        {
            Debug.Log($"GameManager: EventSystem found: {eventSystem.name}");
            Debug.Log($"GameManager: EventSystem active: {eventSystem.gameObject.activeInHierarchy}");
            Debug.Log($"GameManager: EventSystem enabled: {eventSystem.enabled}");
        }
        
        // Check UI Canvas
        if (uiCanvasInstance == null)
        {
            Debug.LogError("GameManager: UI Canvas instance is null!");
            return;
        }
        
        var canvas = uiCanvasInstance.GetComponent<Canvas>();
        if (canvas != null)
        {
            Debug.Log($"GameManager: Canvas render mode: {canvas.renderMode}");
            Debug.Log($"GameManager: Canvas sorting order: {canvas.sortingOrder}");
            Debug.Log($"GameManager: Canvas active: {canvas.gameObject.activeInHierarchy}");
            
            // Check GraphicRaycaster
            var raycaster = canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>();
            if (raycaster == null)
            {
                Debug.LogError("GameManager: No GraphicRaycaster on canvas! This will prevent UI interaction.");
            }
            else
            {
                Debug.Log($"GameManager: GraphicRaycaster enabled: {raycaster.enabled}");
            }
        }
        
        // Check Button
        if (restartButton == null)
        {
            Debug.LogError("GameManager: Restart button is null!");
            return;
        }
        
        var buttonGO = restartButton.gameObject;
        Debug.Log($"GameManager: Button GameObject: {buttonGO.name}");
        Debug.Log($"GameManager: Button active: {buttonGO.activeInHierarchy}");
        Debug.Log($"GameManager: Button enabled: {restartButton.enabled}");
        Debug.Log($"GameManager: Button interactable: {restartButton.interactable}");
        
        // Check Button's Image component (needed for raycast)
        var image = buttonGO.GetComponent<UnityEngine.UI.Image>();
        if (image == null)
        {
            Debug.LogError("GameManager: No Image component on button! This will prevent raycast detection.");
        }
        else
        {
            Debug.Log($"GameManager: Image component found, raycastTarget: {image.raycastTarget}");
            Debug.Log($"GameManager: Image enabled: {image.enabled}");
        }
        
        // Check if there are other UI elements blocking the button
        var allUIElements = uiCanvasInstance.GetComponentsInChildren<UnityEngine.UI.Graphic>();
        Debug.Log($"GameManager: Total UI elements in canvas: {allUIElements.Length}");
        
        foreach (var element in allUIElements)
        {
            if (element.raycastTarget && element.gameObject != buttonGO)
            {
                Debug.Log($"GameManager: Other raycast target found: {element.name} (Active: {element.gameObject.activeInHierarchy})");
            }
        }
        
        Debug.Log("=== END UI INTERACTION DEBUG ===");
    }
    
    [ContextMenu("Fix UI Interaction Issues")]
    public void FixUIInteractionIssues()
    {
        Debug.Log("GameManager: Attempting to fix UI interaction issues...");
        
        // Ensure EventSystem exists
        var eventSystem = UnityEngine.EventSystems.EventSystem.current;
        if (eventSystem == null)
        {
            Debug.LogWarning("GameManager: No EventSystem found, creating one...");
            var eventSystemGO = new GameObject("EventSystem");
            eventSystemGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            DontDestroyOnLoad(eventSystemGO);
        }
        
        // Fix UI Canvas
        if (uiCanvasInstance != null)
        {
            var canvas = uiCanvasInstance.GetComponent<Canvas>();
            if (canvas != null)
            {
                // Ensure proper canvas settings
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 100;
                
                // Ensure GraphicRaycaster exists
                var raycaster = canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>();
                if (raycaster == null)
                {
                    Debug.Log("GameManager: Adding GraphicRaycaster to canvas...");
                    canvas.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
                }
            }
        }
        
        // Fix Button
        if (restartButton != null)
        {
            var buttonGO = restartButton.gameObject;
            
            // Ensure button is active and enabled
            buttonGO.SetActive(true);
            restartButton.enabled = true;
            restartButton.interactable = true;
            
            // Ensure Image component exists and has raycastTarget enabled
            var image = buttonGO.GetComponent<UnityEngine.UI.Image>();
            if (image == null)
            {
                Debug.Log("GameManager: Adding Image component to button...");
                image = buttonGO.AddComponent<UnityEngine.UI.Image>();
            }
            image.raycastTarget = true;
            image.enabled = true;
            
            // Re-add click listener
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(() => {
                Debug.Log($"GameManager: Restart button clicked (fixed) in scene: {SceneManager.GetActiveScene().name}!");
                RestartGame();
            });
            
            Debug.Log("GameManager: UI interaction issues fixed!");
        }
        else
        {
            Debug.LogError("GameManager: Cannot fix button - restartButton is null!");
        }
    }
    
    [ContextMenu("Test Restart Button in Current Scene")]
    public void TestRestartButtonInCurrentScene()
    {
        string currentScene = SceneManager.GetActiveScene().name;
        Debug.Log($"GameManager: Testing restart button in scene: {currentScene}");
        
        if (restartButton == null)
        {
            Debug.LogError("GameManager: Restart button is null!");
            return;
        }
        
        Debug.Log($"GameManager: Restart button found: {restartButton.name}");
        Debug.Log($"GameManager: Restart button active: {restartButton.gameObject.activeInHierarchy}");
        Debug.Log($"GameManager: Restart button enabled: {restartButton.enabled}");
        Debug.Log($"GameManager: Restart button interactable: {restartButton.interactable}");
        
        // Test if we can manually trigger the restart
        Debug.Log("GameManager: Manually calling RestartGame()...");
        RestartGame();
    }
    
    [ContextMenu("Fix Restart Button Issues")]
    public void FixRestartButtonIssues()
    {
        Debug.Log("GameManager: Attempting to fix restart button issues...");
        
        if (restartButton == null)
        {
            Debug.LogError("GameManager: Restart button is null!");
            return;
        }
        
        // Ensure button is properly configured
        restartButton.gameObject.SetActive(true);
        restartButton.enabled = true;
        restartButton.interactable = true;
        
        // Re-add click listener
        restartButton.onClick.RemoveAllListeners();
        restartButton.onClick.AddListener(() => {
            Debug.Log($"GameManager: Restart button clicked (fixed) in scene: {SceneManager.GetActiveScene().name}!");
            RestartGame();
        });
        
        Debug.Log("GameManager: Restart button issues fixed!");
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
        }
    }
}

