using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float jumpHeight = 1f;
    [SerializeField] private float jumpDuration = 0.5f;
    
    [Header("Path Waypoints")]
    [Tooltip("Manually assign all path waypoints in order")]
    [SerializeField] private List<Transform> pathWaypoints = new List<Transform>();
    
    private int currentPathIndex = 0;
    private bool isMoving = false;
    
    // Event for when player movement completes
    public System.Action OnMovementComplete;
    
    public bool IsMoving => isMoving;
    public int CurrentPathIndex => currentPathIndex;
    
    private void Start()
    {
        // Set initial position to first waypoint if available
        if (pathWaypoints.Count > 0)
        {
            transform.position = pathWaypoints[0].position;
            currentPathIndex = 0;
        }
    }
    
    public void MovePlayer(int steps)
    {
        if (isMoving)
        {
            Debug.LogWarning("Player is already moving!");
            return;
        }
        
        if (pathWaypoints.Count == 0)
        {
            Debug.LogWarning("No path waypoints found! Please assign waypoints in the Inspector.");
            return;
        }
        
        StartCoroutine(MoveToWaypoints(steps));
    }
    
    private IEnumerator MoveToWaypoints(int steps)
    {
        isMoving = true;
        
        for (int step = 0; step < steps; step++)
        {
            // Move to next waypoint (with looping)
            currentPathIndex++;
            
            // Loop back to start if we reach the end
            if (currentPathIndex >= pathWaypoints.Count)
            {
                currentPathIndex = 0;
                Debug.Log("Player reached the end of the path! Looping back to start.");
            }
            
            Transform targetWaypoint = pathWaypoints[currentPathIndex];
            Vector3 targetPosition = targetWaypoint.position;
            
            // Jump to the waypoint
            yield return StartCoroutine(JumpToPosition(targetPosition));
        }
        
        isMoving = false;
        Debug.Log($"Player movement complete! Now at waypoint {currentPathIndex}");
        
        // Notify that movement is complete
        OnMovementComplete?.Invoke();
    }
    
    private IEnumerator JumpToPosition(Vector3 targetPosition)
    {
        Vector3 startPos = transform.position;
        float elapsedTime = 0f;
        
        while (elapsedTime < jumpDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / jumpDuration;
            
            // Smooth curve for jump (ease in-out)
            float curve = t * t * (3f - 2f * t);
            
            // Calculate position with arc
            Vector3 currentPos = Vector3.Lerp(startPos, targetPosition, curve);
            currentPos.y += Mathf.Sin(curve * Mathf.PI) * jumpHeight;
            
            transform.position = currentPos;
            
            yield return null;
        }
        
        // Ensure we're exactly at the target position
        transform.position = targetPosition;
    }
    
    public void ResetPlayerPosition()
    {
        if (pathWaypoints.Count > 0)
        {
            transform.position = pathWaypoints[0].position;
            currentPathIndex = 0;
        }
    }
    
    // Called from GameManager when dice sum is calculated
    public void OnDiceRollComplete(int diceSum)
    {
        MovePlayer(diceSum);
    }
}

