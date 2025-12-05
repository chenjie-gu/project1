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
    private bool[] stepCompleted = new bool[4];
    private GameObject tutorialKey;
    private Door tutorialDoor;
    
    // Track individual key presses for step 0
    private bool aKeyPressed = false;
    private bool dKeyPressed = false;
    
    // Track key pickup/drop for step 2
    private bool keyPickedUp = false;
    private bool keyDropped = false;
    
    // Track door interaction for step 3
    private bool keyGivenToDoor = false;
    
    private void Start()
    {
        StartTutorial();
    }
    
    private void StartTutorial()
    {
        currentStep = 0;
        stepCompleted[0] = false;
        stepCompleted[1] = false;
        stepCompleted[2] = false;
        stepCompleted[3] = false;
        
        // Reset key tracking for step 0
        aKeyPressed = false;
        dKeyPressed = false;
        
        // Reset key pickup/drop tracking for step 2
        keyPickedUp = false;
        keyDropped = false;
        
        // Reset door interaction tracking for step 3
        keyGivenToDoor = false;
        
        // Find the tutorial door
        tutorialDoor = FindObjectOfType<Door>();
        if (tutorialDoor == null)
        {
            Debug.LogWarning("TutorialManager: No Door found in scene!");
        }
        
        ShowTutorialStep(0);
    }
    
    private void ShowTutorialStep(int step)
    {
        string textToShow = "";
        switch (step)
        {
            case 0:
                textToShow = "Press A/D to move left/right!";
                break;
            case 1:
                textToShow = "Press SPACE to jump!";
                break;
            case 2:
                textToShow = "Press E to pick up/drop the acorn!";
                SpawnTutorialKey();
                break;
            case 3:
                textToShow = "Give the acorn to your squirrel friend in their treehouse!";
                break;
        }
        
        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(true);
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
                }
                if (Input.GetKeyDown(KeyCode.D))
                {
                    dKeyPressed = true;
                }
                
                // Complete step only when both keys have been pressed
                if (aKeyPressed && dKeyPressed)
                {
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
                    }
                    else if (keyPickedUp && !keyDropped)
                    {
                        keyDropped = true;
                    }
                }
                
                // Complete step only when both pickup and drop have occurred
                if (keyPickedUp && keyDropped)
                {
                    CompleteStep(2);
                }
                break;
                
            case 3: // Door interaction step - check if door is open
                if (tutorialDoor != null && tutorialDoor.IsOpen())
                {
                    if (!keyGivenToDoor)
                    {
                        keyGivenToDoor = true;
                        CompleteStep(3);
                    }
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
            
            if (currentStep < 4)
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
