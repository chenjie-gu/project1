using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class StartMenu : MonoBehaviour
{
    [Header("UI References")]
    public Button playButton;
    
    void Start()
    {
        // Ensure EventSystem exists (required for UI buttons)
        if (EventSystem.current == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
        }
        
        // If no button is assigned, try to find it automatically
        if (playButton == null)
        {
            playButton = GameObject.Find("PlayButton")?.GetComponent<Button>();
            if (playButton == null)
            {
                // Try to find any button in the scene
                playButton = FindObjectOfType<Button>();
            }
        }
        
        // Set up the button click event
        if (playButton != null)
        {
            // Remove any existing listeners to avoid duplicates
            playButton.onClick.RemoveAllListeners();
            playButton.onClick.AddListener(OnPlayButtonClicked);
            
            // Ensure button is interactable
            playButton.interactable = true;
        }
        else
        {
            Debug.LogError("StartMenu: Play button not found! Make sure there's a button in the scene and assign it in the Inspector.");
        }
    }
    
    public void OnPlayButtonClicked()
    {
        // Check if Tutorial scene exists
        if (Application.CanStreamedLevelBeLoaded("Tutorial"))
        {
            SceneManager.LoadScene("Tutorial");
        }
        else
        {
            Debug.LogError("StartMenu: Tutorial scene not found! Make sure it's added to Build Settings.");
        }
    }
    
    // Alternative method that can be called directly from Unity Inspector
    public void LoadTutorial()
    {
        OnPlayButtonClicked();
    }
    
    void OnDestroy()
    {
        // Clean up listeners when script is destroyed
        if (playButton != null)
        {
            playButton.onClick.RemoveAllListeners();
        }
    }
}

