using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraAspectRatio : MonoBehaviour
{
    [Header("Aspect Ratio Settings")]
    public float targetAspectRatio = 16f / 9f; // 16:9 aspect ratio
    
    private Camera cam;
    
    void Start()
    {
        cam = GetComponent<Camera>();
        SetAspectRatio();
    }
    
    void Update()
    {
        // Update aspect ratio if screen size changes
        SetAspectRatio();
    }
    
    void SetAspectRatio()
    {
        float currentAspectRatio = (float)Screen.width / Screen.height;
        
        if (currentAspectRatio > targetAspectRatio)
        {
            // Screen is wider than 16:9, add letterboxing
            float scaleHeight = currentAspectRatio / targetAspectRatio;
            cam.rect = new Rect(0, (1f - 1f / scaleHeight) / 2f, 1f, 1f / scaleHeight);
        }
        else if (currentAspectRatio < targetAspectRatio)
        {
            // Screen is taller than 16:9, add pillarboxing
            float scaleWidth = targetAspectRatio / currentAspectRatio;
            cam.rect = new Rect((1f - 1f / scaleWidth) / 2f, 0, 1f / scaleWidth, 1f);
        }
        else
        {
            // Perfect 16:9 aspect ratio
            cam.rect = new Rect(0, 0, 1, 1);
        }
    }
    
    void OnValidate()
    {
        if (Application.isPlaying)
        {
            SetAspectRatio();
        }
    }
}
