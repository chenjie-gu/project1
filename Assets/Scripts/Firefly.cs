using UnityEngine;
using System.Collections;

[RequireComponent(typeof(SpriteRenderer))]
public class Firefly : MonoBehaviour
{
    [Header("Firefly Animation")]
    public Sprite[] fireflySprites; // Assign the firefly sprite frames from your sprite sheet
    public float animationSpeed = 3f; // Speed of sprite animation
    
    [Header("Fade Settings")]
    public float minAlpha = 0.1f; // Minimum alpha (how dim it gets)
    public float maxAlpha = 1.0f; // Maximum alpha (how bright it gets)
    public float fadeSpeed = 2.0f; // Speed of fade in/out
    
    [Header("Movement Settings")]
    public bool enableMovement = true;
    public float moveSpeed = 1.0f; // Increased default speed
    public float moveRadius = 3.0f; // Increased default radius
    public float directionChangeInterval = 1.0f; // More frequent direction changes for random movement
    
    private SpriteRenderer spriteRenderer;
    private int currentSpriteIndex = 0;
    private float animationTimer = 0f;
    private float alphaTimer = 0f;
    private bool isFadingIn = true;
    
    // Movement variables
    private Vector3 startPosition;
    private Vector3 targetPosition;
    private float directionChangeTimer = 0f;
    private Vector2 currentDirection;
    
    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        startPosition = transform.position;
        
        // Set initial random direction
        if (enableMovement)
        {
            currentDirection = Random.insideUnitCircle.normalized;
            SetNewTargetPosition();
        }
        
        // Start with random alpha timer to make fireflies out of sync
        alphaTimer = Random.Range(0f, 6.28f); // Random value between 0 and 2π for sine wave
        
        // Also randomize animation timer so sprites are out of sync
        animationTimer = Random.Range(0f, animationSpeed);
        
        // Randomize direction change timer
        directionChangeTimer = Random.Range(0f, directionChangeInterval);
    }
    
    void Update()
    {
        AnimateSprite();
        AnimateFade();
        
        if (enableMovement)
        {
            MoveFirefly();
        }
    }
    
    void AnimateSprite()
    {
        if (fireflySprites == null || fireflySprites.Length == 0) return;
        
        animationTimer += Time.deltaTime;
        
        if (animationTimer >= animationSpeed)
        {
            animationTimer = 0f;
            currentSpriteIndex = (currentSpriteIndex + 1) % fireflySprites.Length;
            spriteRenderer.sprite = fireflySprites[currentSpriteIndex];
        }
    }
    
    void AnimateFade()
    {
        alphaTimer += Time.deltaTime * fadeSpeed;
        
        // Calculate alpha based on sine wave for smooth fade in/out
        // Use a slower sine wave for more natural firefly behavior
        float sineValue = Mathf.Sin(alphaTimer * 0.5f);
        float alphaValue = Mathf.Lerp(minAlpha, maxAlpha, 
            (sineValue + 1f) * 0.5f);
        
        Color currentColor = spriteRenderer.color;
        currentColor.a = alphaValue;
        spriteRenderer.color = currentColor;
    }
    
    void MoveFirefly()
    {
        directionChangeTimer += Time.deltaTime;
        
        // Change direction periodically
        if (directionChangeTimer >= directionChangeInterval)
        {
            directionChangeTimer = 0f;
            SetNewTargetPosition();
        }
        
        // Move towards target position
        Vector3 direction = (targetPosition - transform.position).normalized;
        transform.position += direction * moveSpeed * Time.deltaTime;
        
        // If we've gone too far from start, pick a new random target within bounds
        float distanceFromStart = Vector3.Distance(transform.position, startPosition);
        if (distanceFromStart >= moveRadius)
        {
            // Instead of going back to center, pick a new random target within bounds
            SetNewTargetPosition();
        }
    }
    
    void SetNewTargetPosition()
    {
        // Generate a new random position within the move radius
        // Use Random.insideUnitCircle to ensure it's always within the radius
        Vector2 randomOffset = Random.insideUnitCircle * moveRadius;
        targetPosition = startPosition + new Vector3(randomOffset.x, randomOffset.y, 0);
        
        // Ensure the target is always within bounds (this should always be true now)
        float distanceFromStart = Vector3.Distance(targetPosition, startPosition);
        if (distanceFromStart > moveRadius)
        {
            // Clamp to radius if somehow it's outside
            Vector3 direction = (targetPosition - startPosition).normalized;
            targetPosition = startPosition + direction * moveRadius;
        }
    }
    
    // Method to manually trigger a fade cycle (useful for testing)
    [ContextMenu("Trigger Fade Cycle")]
    public void TriggerFadeCycle()
    {
        alphaTimer = 0f;
        isFadingIn = !isFadingIn;
    }
    
    // Method to reset to starting position
    [ContextMenu("Reset Position")]
    public void ResetPosition()
    {
        transform.position = startPosition;
        directionChangeTimer = 0f;
        SetNewTargetPosition();
    }
    
    // Debug method to check movement status
    [ContextMenu("Debug Movement Status")]
    public void DebugMovementStatus()
    {
        Debug.Log($"Firefly {gameObject.name}:");
        Debug.Log($"  Enable Movement: {enableMovement}");
        Debug.Log($"  Move Speed: {moveSpeed}");
        Debug.Log($"  Move Radius: {moveRadius}");
        Debug.Log($"  Current Position: {transform.position}");
        Debug.Log($"  Start Position: {startPosition}");
        Debug.Log($"  Target Position: {targetPosition}");
        Debug.Log($"  Distance from start: {Vector3.Distance(transform.position, startPosition)}");
    }
}
