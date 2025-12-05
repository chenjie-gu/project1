using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class TutorialUI : MonoBehaviour
{
    [Header("Tutorial Components")]
    public TextMeshProUGUI tutorialText;
    public GameObject tutorialPanel;
    
    private TutorialManager tutorialManager;
    
    void Start()
    {
        // Add TutorialManager component if it doesn't exist
        tutorialManager = GetComponent<TutorialManager>();
        if (tutorialManager == null)
        {
            tutorialManager = gameObject.AddComponent<TutorialManager>();
        }
        
        // Set references in TutorialManager
        tutorialManager.tutorialText = tutorialText;
        tutorialManager.tutorialPanel = tutorialPanel;
        
        // Check if references are set
        if (tutorialText == null)
        {
            Debug.LogError("TutorialUI: tutorialText is null! Please assign TextMeshProUGUI component.");
        }
        
        if (tutorialPanel == null)
        {
            Debug.LogError("TutorialUI: tutorialPanel is null! Please assign Panel GameObject.");
        }
    }
}
