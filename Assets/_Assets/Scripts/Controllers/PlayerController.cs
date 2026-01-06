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
    
    [Header("Fortune Road Waypoints")]
    [Tooltip("Assign Fortune Road waypoints in order: FortuneRoad01 to FortuneRoad05")]
    [SerializeField] private List<Transform> fortuneRoadWaypoints = new List<Transform>();
    [Tooltip("Assign Path39_TreasureChest waypoint (the path after Fortune Road sequence)")]
    [SerializeField] private Transform path39Waypoint;
    
    private int currentPathIndex = 0;
    private bool isMoving = false;
    private bool isInFortuneRoadSequence = false;
    private int fortuneRoadSequenceIndex = -1;
    private int savedNormalPathIndex = -1; // Store the normal path index to resume after Fortune Road
    private bool shouldEnterFortuneRoad = false; // Flag to indicate next movement should go through Fortune Road
    
    // Event for when player movement completes
    public System.Action OnMovementComplete;
    
    public bool IsMoving => isMoving;
    public int CurrentPathIndex => currentPathIndex;
    
    /// <summary>
    /// Gets the name of the current waypoint the player is standing on
    /// </summary>
    public string GetCurrentWaypointName()
    {
        if (currentPathIndex >= 0 && currentPathIndex < pathWaypoints.Count && pathWaypoints[currentPathIndex] != null)
        {
            return pathWaypoints[currentPathIndex].name;
        }
        return string.Empty;
    }
    
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
        
        // Check if we should enter Fortune Road sequence at the start of this movement
        if (shouldEnterFortuneRoad && !isInFortuneRoadSequence)
        {
            Debug.Log("Entering Fortune Road sequence from previous stop on Fortune Road tile.");
            
            // Enter Fortune Road sequence
            isInFortuneRoadSequence = true;
            fortuneRoadSequenceIndex = 0;
            savedNormalPathIndex = currentPathIndex + 1; // Save the next normal path index
            
            // Complete all Fortune Road waypoints
            for (int frStep = 0; frStep < fortuneRoadWaypoints.Count; frStep++)
            {
                if (frStep < fortuneRoadWaypoints.Count && fortuneRoadWaypoints[frStep] != null)
                {
                    Transform targetWaypoint = fortuneRoadWaypoints[frStep];
                    Vector3 targetPosition = targetWaypoint.position;
                    
                    // Jump to Fortune Road waypoint (sound plays during jump)
                    yield return StartCoroutine(JumpToPosition(targetPosition));
                }
            }
            
            // Move to Path39 after Fortune Road sequence
            if (path39Waypoint != null)
            {
                Transform targetWaypoint = path39Waypoint;
                Vector3 targetPosition = path39Waypoint.position;
                
                // Jump to Path39 (sound plays during jump)
                yield return StartCoroutine(JumpToPosition(targetPosition));
                
                // Find Path39 index in normal path to continue from there
                currentPathIndex = FindWaypointIndex(path39Waypoint);
                if (currentPathIndex == -1)
                {
                    // If Path39 not found, use saved index
                    currentPathIndex = savedNormalPathIndex;
                }
                
                // Clear Fortune Road flags - back to normal path
                isInFortuneRoadSequence = false;
                fortuneRoadSequenceIndex = -1;
                shouldEnterFortuneRoad = false;
                
                Debug.Log("Completed Fortune Road sequence! Resuming normal path at Path39.");
            }
            else
            {
                Debug.LogWarning("Path39 waypoint not assigned! Cannot exit Fortune Road sequence.");
                // Fallback: exit sequence and use saved index
                isInFortuneRoadSequence = false;
                fortuneRoadSequenceIndex = -1;
                shouldEnterFortuneRoad = false;
                currentPathIndex = savedNormalPathIndex;
            }
        }
        
        for (int step = 0; step < steps; step++)
        {
            // Normal path movement
            currentPathIndex++;
            
            // Loop back to start if we reach the end
            if (currentPathIndex >= pathWaypoints.Count)
            {
                currentPathIndex = 0;
                Debug.Log("Player reached the end of the path! Looping back to start.");
            }
            
            Transform targetWaypoint = pathWaypoints[currentPathIndex];
            Vector3 targetPosition = targetWaypoint.position;
            
            // Jump to the waypoint (sound plays during jump)
            yield return StartCoroutine(JumpToPosition(targetPosition));
        }
        
        isMoving = false;
        string currentWaypointName = isInFortuneRoadSequence 
            ? (fortuneRoadSequenceIndex >= 0 && fortuneRoadSequenceIndex < fortuneRoadWaypoints.Count
                ? fortuneRoadWaypoints[fortuneRoadSequenceIndex].name 
                : "Fortune Road Sequence (completed)") 
            : (currentPathIndex < pathWaypoints.Count ? pathWaypoints[currentPathIndex].name : "Unknown");
        Debug.Log($"Player movement complete! Now at waypoint: {currentWaypointName}");
        
        // Check if player stopped on a Fortune Road tile - set flag for next movement
        if (!isInFortuneRoadSequence && IsCurrentWaypointFortuneRoad())
        {
            shouldEnterFortuneRoad = true;
            Debug.Log($"Player stopped on Fortune Road! Next dice roll will go through Fortune Road sequence.");
        }
        
        // Notify that movement is complete
        OnMovementComplete?.Invoke();
    }
    
    private IEnumerator JumpToPosition(Vector3 targetPosition)
    {
        Vector3 startPos = transform.position;
        float elapsedTime = 0f;
        bool hasPlayedSound = false;
        
        while (elapsedTime < jumpDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / jumpDuration;
            
            // Smooth curve for jump (ease in-out)
            float curve = t * t * (3f - 2f * t);
            
            // Play TileHit sound early in the jump animation for immediate feedback
            if (!hasPlayedSound && t > 0.2f)
            {
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlaySFX("TileHit");
                }
                hasPlayedSound = true;
            }
            
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
        
        // Reset Fortune Road sequence state
        isInFortuneRoadSequence = false;
        fortuneRoadSequenceIndex = -1;
        savedNormalPathIndex = -1;
        shouldEnterFortuneRoad = false;
    }
    
    /// <summary>
    /// Checks if the current waypoint is a Fortune Road tile
    /// </summary>
    private bool IsCurrentWaypointFortuneRoad()
    {
        if (currentPathIndex >= 0 && currentPathIndex < pathWaypoints.Count && pathWaypoints[currentPathIndex] != null)
        {
            string waypointName = pathWaypoints[currentPathIndex].name;
            return waypointName.Contains("FortuneRoad", System.StringComparison.OrdinalIgnoreCase);
        }
        return false;
    }
    
    /// <summary>
    /// Finds the index of a waypoint in the normal path waypoints list
    /// </summary>
    private int FindWaypointIndex(Transform waypoint)
    {
        if (waypoint == null) return -1;
        
        for (int i = 0; i < pathWaypoints.Count; i++)
        {
            if (pathWaypoints[i] == waypoint)
            {
                return i;
            }
        }
        return -1;
    }
    
    // Called from GameManager when dice sum is calculated
    public void OnDiceRollComplete(int diceSum)
    {
        MovePlayer(diceSum);
    }
}

