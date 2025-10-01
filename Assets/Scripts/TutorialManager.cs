using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class TutorialManager : MonoBehaviour
{
    [Header("Tutorial Steps")]
    public TextMeshProUGUI tutorialText;
    public GameObject tutorialPanel;
    
    [Header("Animation Settings")]
    public float fadeInDuration = 1f;
    public float fadeOutDuration = 0.7f;
    public float stepTransitionDelay = 1f; // Delay between steps
    
    [Header("Tutorial Key")]
    public GameObject tutorialKeyPrefab;
    public Transform keySpawnPoint;
    
    private int currentStep = 0;
    private bool[] stepCompleted = new bool[3];
    private GameObject tutorialKey;
    
    // Track individual key presses for step 0
    private bool aKeyPressed = false;
    private bool dKeyPressed = false;
    
    // Track key pickup/drop for step 2
    private bool keyPickedUp = false;
    private bool keyDropped = false;
    
    private void Start()
    {
        Debug.Log("TutorialManager: Start method called");
        StartTutorial();
    }
    
    private void StartTutorial()
    {
        currentStep = 0;
        stepCompleted[0] = false;
        stepCompleted[1] = false;
        stepCompleted[2] = false;
        
        // Reset key tracking for step 0
        aKeyPressed = false;
        dKeyPressed = false;
        
        // Reset key pickup/drop tracking for step 2
        keyPickedUp = false;
        keyDropped = false;
        
        ShowTutorialStep(0);
    }
    
    private void ShowTutorialStep(int step)
    {
        Debug.Log("TutorialManager: Showing step " + step);
        
        string textToShow = "";
        switch (step)
        {
            case 0:
                textToShow = "Press A/D to move left/right";
                break;
            case 1:
                textToShow = "Press SPACE to jump";
                break;
            case 2:
                textToShow = "Press E to pick up/drop the pine cone";
                SpawnTutorialKey();
                break;
        }
        
        Debug.Log("TutorialManager: Set text to - " + textToShow);
        
        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(true);
            Debug.Log("TutorialManager: Tutorial panel activated");
        }
        else
        {
            Debug.LogError("TutorialManager: tutorialPanel is null!");
        }
        
        // Start fade animation
        StartCoroutine(FadeInText(textToShow));
    }
    
    private void SpawnTutorialKey()
    {
        if (tutorialKeyPrefab != null)
        {
            Vector3 spawnPosition;
            
            if (keySpawnPoint != null)
            {
                spawnPosition = keySpawnPoint.position;
            }
            else
            {
                // Create default spawn point if none assigned
                spawnPosition = new Vector3(0, 0, 0); // Center of scene
                Debug.LogWarning("No keySpawnPoint assigned! Using default position (0,0,0)");
            }
            
            tutorialKey = Instantiate(tutorialKeyPrefab, spawnPosition, Quaternion.identity);
        }
    }
    
    private void Update()
    {
        CheckTutorialProgress();
    }
    
    private void CheckTutorialProgress()
    {
        switch (currentStep)
        {
            case 0: // Movement step - require both A and D
                if (Input.GetKeyDown(KeyCode.A))
                {
                    aKeyPressed = true;
                    Debug.Log("TutorialManager: A key pressed");
                }
                if (Input.GetKeyDown(KeyCode.D))
                {
                    dKeyPressed = true;
                    Debug.Log("TutorialManager: D key pressed");
                }
                
                // Complete step only when both keys have been pressed
                if (aKeyPressed && dKeyPressed)
                {
                    Debug.Log("TutorialManager: Both A and D keys pressed, completing step 0");
                    CompleteStep(0);
                }
                break;
                
            case 1: // Jump step
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    CompleteStep(1);
                }
                break;
                
            case 2: // Pickup/Drop step - require both pickup and drop
                if (Input.GetKeyDown(KeyCode.E))
                {
                    if (!keyPickedUp)
                    {
                        keyPickedUp = true;
                        Debug.Log("TutorialManager: Key picked up");
                    }
                    else if (keyPickedUp && !keyDropped)
                    {
                        keyDropped = true;
                        Debug.Log("TutorialManager: Key dropped");
                    }
                }
                
                // Complete step only when both pickup and drop have occurred
                if (keyPickedUp && keyDropped)
                {
                    Debug.Log("TutorialManager: Key picked up and dropped, completing step 2");
                    CompleteStep(2);
                }
                break;
        }
    }
    
    private void CompleteStep(int step)
    {
        if (!stepCompleted[step])
        {
            stepCompleted[step] = true;
            currentStep++;
            
            if (currentStep < 3)
            {
                // Add delay before showing next step
                StartCoroutine(DelayedShowNextStep(currentStep));
            }
            else
            {
                CompleteTutorial();
            }
        }
    }
    
    private void CompleteTutorial()
    {
        // Fade out before hiding
        StartCoroutine(FadeOutAndHide());
        
        Debug.Log("Tutorial completed! Loading Level 1...");
        
        // Load Level 1 after tutorial completion
        StartCoroutine(LoadLevel1AfterDelay());
    }
    
    private System.Collections.IEnumerator LoadLevel1AfterDelay()
    {
        // Wait for fade out to complete
        yield return new WaitForSeconds(fadeOutDuration + 0.5f);
        
        // Load Level 1
        UnityEngine.SceneManagement.SceneManager.LoadScene("Level1");
    }
    
    private IEnumerator FadeInText(string text)
    {
        if (tutorialText == null) yield break;
        
        // Set text and start transparent
        tutorialText.text = text;
        Color textColor = tutorialText.color;
        textColor.a = 0f;
        tutorialText.color = textColor;
        
        // Fade in
        float elapsedTime = 0f;
        while (elapsedTime < fadeInDuration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(0f, 1f, elapsedTime / fadeInDuration);
            textColor.a = alpha;
            tutorialText.color = textColor;
            yield return null;
        }
        
        // Ensure fully visible
        textColor.a = 1f;
        tutorialText.color = textColor;
    }
    
    private IEnumerator DelayedShowNextStep(int nextStep)
    {
        Debug.Log($"TutorialManager: Waiting {stepTransitionDelay} seconds before showing step {nextStep}");
        
        // Wait for the specified delay
        yield return new WaitForSeconds(stepTransitionDelay);
        
        // Show the next step
        ShowTutorialStep(nextStep);
    }
    
    private IEnumerator FadeOutAndHide()
    {
        if (tutorialText == null) yield break;
        
        Color textColor = tutorialText.color;
        
        // Fade out
        float elapsedTime = 0f;
        while (elapsedTime < fadeOutDuration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeOutDuration);
            textColor.a = alpha;
            tutorialText.color = textColor;
            yield return null;
        }
        
        // Hide panel
        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(false);
        }
        
        // Destroy the tutorial key after pickup/drop
        if (tutorialKey != null)
        {
            Destroy(tutorialKey);
        }
    }
}
