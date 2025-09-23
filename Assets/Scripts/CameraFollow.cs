using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform target;
    
    [Header("Follow Settings")]
    public float smoothSpeed = 0.125f;
    public Vector3 offset = new Vector3(0, 2, -10);
    
    [Header("Ground Clamping")]
    public bool clampToGround = true;
    public float groundLevel = -5f;
    public float minCameraHeight = 5f;
    
    private float leftBoundary;
    private float rightBoundary;
    private float bottomBoundary;
    private float topBoundary;

    void LateUpdate()
    {
        if (target == null) return;
        
        Vector3 desiredPosition = target.position + offset;
        
        // Apply ground clamping first
        if (clampToGround)
        {
            float minY = groundLevel + minCameraHeight;
            desiredPosition.y = Mathf.Max(desiredPosition.y, minY);
        }
        
        // Apply camera boundaries - stop following before player hits boundary
        PlayerMovement player = target.GetComponent<PlayerMovement>();
        if (player != null)
        {
            Camera cam = GetComponent<Camera>();
            float cameraWidth = cam.orthographicSize * cam.aspect;
            float cameraHeight = cam.orthographicSize;
            
            float leftCameraLimit = player.leftBoundary + cameraWidth;
            float rightCameraLimit = player.rightBoundary - cameraWidth;
            float bottomCameraLimit = player.bottomBoundary + cameraHeight;
            float topCameraLimit = player.topBoundary - cameraHeight;
            
            // Apply horizontal boundaries
            if (desiredPosition.x < leftCameraLimit)
                desiredPosition.x = leftCameraLimit;
            else if (desiredPosition.x > rightCameraLimit)
                desiredPosition.x = rightCameraLimit;
            
            // Apply vertical boundaries (but respect ground clamping)
            if (desiredPosition.y < bottomCameraLimit)
                desiredPosition.y = bottomCameraLimit;
            else if (desiredPosition.y > topCameraLimit)
                desiredPosition.y = topCameraLimit;
        }
        
        // Smooth follow
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        transform.position = smoothedPosition;
    }
}