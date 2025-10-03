using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StartMenu : MonoBehaviour
{
    [Header("UI References")]
    public Button playButton;
    
    void Start()
    {
        // If no button is assigned, try to find it automatically
        if (playButton == null)
        {
            playButton = GameObject.Find("PlayButton")?.GetComponent<Button>();
        }
        
        // Set up the button click event
        if (playButton != null)
        {
            playButton.onClick.AddListener(OnPlayButtonClicked);
            Debug.Log("StartMenu: Play button connected successfully");
        }
        else
        {
            Debug.LogError("StartMenu: Play button not found! Make sure there's a button named 'PlayButton' in the scene.");
        }
    }
    
    public void OnPlayButtonClicked()
    {
        Debug.Log("StartMenu: Play button clicked - loading Tutorial scene");
        SceneManager.LoadScene("Tutorial");
    }
    
    // Alternative method that can be called directly from Unity Inspector
    public void LoadTutorial()
    {
        Debug.Log("StartMenu: LoadTutorial called - loading Tutorial scene");
        SceneManager.LoadScene("Tutorial");
    }
}
