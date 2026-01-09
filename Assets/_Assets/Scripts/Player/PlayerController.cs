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
    private bool shouldUseOneDice = false; // Flag to indicate next dice roll should use only one dice
    private bool passedPath01Start = false; // Flag to track if player passed through Path01_Start during movement
    
    // Event for when player movement completes
    public System.Action OnMovementComplete;
    
    // Event for when player passes through Path01_Start
    public System.Action OnPassedPath01Start;
    
    public bool IsMoving => isMoving;
    public int CurrentPathIndex => currentPathIndex;
    public bool ShouldUseOneDice => shouldUseOneDice; // Public property to check if one dice should be used
    public List<Transform> PathWaypoints => pathWaypoints; // Public access to path waypoints
    
    /// <summary>
    /// Clears the one dice flag so next roll will use two dice
    /// </summary>
    public void ClearOneDiceFlag()
    {
        shouldUseOneDice = false;
    }
    
    /// <summary>
    /// Gets the name of the current waypoint the player is standing on
    /// </summary>
    public string GetCurrentWaypointName()
    {
        // If in Fortune Road sequence, return Fortune Road waypoint name
        if (isInFortuneRoadSequence)
        {
            if (fortuneRoadSequenceIndex >= 0 && fortuneRoadSequenceIndex < fortuneRoadWaypoints.Count && fortuneRoadWaypoints[fortuneRoadSequenceIndex] != null)
            {
                return fortuneRoadWaypoints[fortuneRoadSequenceIndex].name;
            }
            return string.Empty;
        }
        
        // Otherwise, return normal path waypoint name
        if (currentPathIndex >= 0 && currentPathIndex < pathWaypoints.Count && pathWaypoints[currentPathIndex] != null)
        {
            return pathWaypoints[currentPathIndex].name;
        }
        return string.Empty;
    }
    
    /// <summary>
    /// Gets the name of the waypoint the player will land on after moving the specified number of steps
    /// </summary>
    public string GetFutureWaypointName(int steps)
    {
        if (pathWaypoints == null || pathWaypoints.Count == 0)
        {
            return string.Empty;
        }
        
        // Calculate future path index
        int futureIndex = currentPathIndex + steps;
        
        // Handle looping
        if (futureIndex >= pathWaypoints.Count)
        {
            futureIndex = futureIndex % pathWaypoints.Count;
        }
        
        if (futureIndex >= 0 && futureIndex < pathWaypoints.Count && pathWaypoints[futureIndex] != null)
        {
            return pathWaypoints[futureIndex].name;
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
        passedPath01Start = false; // Reset flag at start of movement
        
        // Check if we should enter Fortune Road sequence at the start of this movement
        if (shouldEnterFortuneRoad && !isInFortuneRoadSequence)
        {
            Debug.Log($"Entering Fortune Road sequence with {steps} steps from previous stop on Fortune Road tile.");
            
            // Enter Fortune Road sequence
            isInFortuneRoadSequence = true;
            fortuneRoadSequenceIndex = 0;
            savedNormalPathIndex = currentPathIndex + 1; // Save the next normal path index
            
            // Start directly from FortuneRoad01 (skip the Fortune Road tile itself)
            // Move through Fortune Road waypoints based on dice roll steps
            int fortuneRoadStepsUsed = 0;
            for (int step = 0; step < steps && fortuneRoadSequenceIndex < fortuneRoadWaypoints.Count; step++)
            {
                if (fortuneRoadSequenceIndex < fortuneRoadWaypoints.Count && fortuneRoadWaypoints[fortuneRoadSequenceIndex] != null)
                {
                    Transform targetWaypoint = fortuneRoadWaypoints[fortuneRoadSequenceIndex];
                    Vector3 targetPosition = targetWaypoint.position;
                    
                    // Jump to Fortune Road waypoint (sound plays during jump)
                    yield return StartCoroutine(JumpToPosition(targetPosition));
                    
                    fortuneRoadSequenceIndex++;
                    fortuneRoadStepsUsed++;
                    Debug.Log($"Moved to Fortune Road waypoint {fortuneRoadSequenceIndex}: {targetWaypoint.name}");
                }
            }
            
            // Check if we've completed all Fortune Road waypoints
            if (fortuneRoadSequenceIndex >= fortuneRoadWaypoints.Count)
            {
                // All Fortune Road waypoints visited, move to Path39
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
                    shouldUseOneDice = false; // Clear one dice flag after using it
                    
                    Debug.Log("Completed all Fortune Road waypoints! Moved to Path39 and resuming normal path.");
                    
                    // Calculate remaining steps after Fortune Road sequence and Path39
                    int remainingSteps = steps - fortuneRoadStepsUsed - 1; // -1 for Path39
                    
                    // Continue with remaining steps on normal path
                    if (remainingSteps > 0)
                    {
                        for (int remainingStep = 0; remainingStep < remainingSteps; remainingStep++)
                        {
                            currentPathIndex++;
                            
                            // Loop back to start if we reach the end
                            if (currentPathIndex >= pathWaypoints.Count)
                            {
                                currentPathIndex = 0;
                                Debug.Log("Player reached the end of the path! Looping back to start.");
                            }
                            
                            Transform remainingWaypoint = pathWaypoints[currentPathIndex];
                            Vector3 remainingPosition = remainingWaypoint.position;
                            
                            // Check if this waypoint is Path01_Start (or Path001_Start)
                            CheckAndTriggerPath01Start(remainingWaypoint);
                            
                            // Jump to the waypoint (sound plays during jump)
                            yield return StartCoroutine(JumpToPosition(remainingPosition));
                        }
                    }
                }
                else
                {
                    Debug.LogWarning("Path39 waypoint not assigned! Cannot exit Fortune Road sequence.");
                    // Fallback: exit sequence and use saved index
                    isInFortuneRoadSequence = false;
                    fortuneRoadSequenceIndex = -1;
                    shouldEnterFortuneRoad = false;
                    shouldUseOneDice = false; // Clear one dice flag
                    currentPathIndex = savedNormalPathIndex;
                }
            }
            else
            {
                // Not all Fortune Road waypoints visited yet, continue in Fortune Road sequence
                // Calculate remaining steps
                int remainingSteps = steps - fortuneRoadStepsUsed;
                
                // Continue with remaining steps in Fortune Road sequence
                if (remainingSteps > 0)
                {
                    for (int step = 0; step < remainingSteps && fortuneRoadSequenceIndex < fortuneRoadWaypoints.Count; step++)
                    {
                        if (fortuneRoadSequenceIndex < fortuneRoadWaypoints.Count && fortuneRoadWaypoints[fortuneRoadSequenceIndex] != null)
                        {
                            Transform targetWaypoint = fortuneRoadWaypoints[fortuneRoadSequenceIndex];
                            Vector3 targetPosition = targetWaypoint.position;
                            
                            // Jump to Fortune Road waypoint (sound plays during jump)
                            yield return StartCoroutine(JumpToPosition(targetPosition));
                            
                            fortuneRoadSequenceIndex++;
                            Debug.Log($"Continued Fortune Road sequence: {targetWaypoint.name}");
                        }
                    }
                }
                
                // If we've now completed all Fortune Road waypoints, go to Path39
                if (fortuneRoadSequenceIndex >= fortuneRoadWaypoints.Count)
                {
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
                            currentPathIndex = savedNormalPathIndex;
                        }
                        
                        // Clear Fortune Road flags
                        isInFortuneRoadSequence = false;
                        fortuneRoadSequenceIndex = -1;
                        shouldEnterFortuneRoad = false;
                        shouldUseOneDice = false;
                        
                        Debug.Log("Completed all Fortune Road waypoints! Moved to Path39.");
                    }
                }
            }
        }
        else if (isInFortuneRoadSequence)
        {
            // Already in Fortune Road sequence, continue through it using dice sum
            int fortuneRoadStepsUsed = 0;
            
            // Move through Fortune Road waypoints based on dice sum
            for (int step = 0; step < steps && fortuneRoadSequenceIndex < fortuneRoadWaypoints.Count; step++)
            {
                if (fortuneRoadSequenceIndex < fortuneRoadWaypoints.Count && fortuneRoadWaypoints[fortuneRoadSequenceIndex] != null)
                {
                    Transform targetWaypoint = fortuneRoadWaypoints[fortuneRoadSequenceIndex];
                    Vector3 targetPosition = targetWaypoint.position;
                    
                    // Jump to Fortune Road waypoint (sound plays during jump)
                    yield return StartCoroutine(JumpToPosition(targetPosition));
                    
                    fortuneRoadSequenceIndex++;
                    fortuneRoadStepsUsed++;
                    Debug.Log($"Moved to Fortune Road waypoint {fortuneRoadSequenceIndex}: {targetWaypoint.name} (using dice sum: {steps})");
                }
            }
            
            // Check if we've completed all Fortune Road waypoints
            if (fortuneRoadSequenceIndex >= fortuneRoadWaypoints.Count)
            {
                // Move to Path39
                if (path39Waypoint != null)
                {
                    Transform targetWaypoint = path39Waypoint;
                    Vector3 targetPosition = path39Waypoint.position;
                    
                    // Jump to Path39 (sound plays during jump)
                    yield return StartCoroutine(JumpToPosition(targetPosition));
                    
                    // Find Path39 index in normal path
                    currentPathIndex = FindWaypointIndex(path39Waypoint);
                    if (currentPathIndex == -1)
                    {
                        currentPathIndex = savedNormalPathIndex;
                    }
                    
                    // Clear Fortune Road flags
                    isInFortuneRoadSequence = false;
                    fortuneRoadSequenceIndex = -1;
                    shouldEnterFortuneRoad = false;
                    shouldUseOneDice = false;
                    
                    Debug.Log("Completed all Fortune Road waypoints! Moved to Path39.");
                    
                    // Calculate remaining steps after Fortune Road sequence and Path39
                    int remainingSteps = steps - fortuneRoadStepsUsed - 1; // -1 for Path39
                    
                    // Continue with remaining steps on normal path
                    if (remainingSteps > 0)
                    {
                        Debug.Log($"Continuing with {remainingSteps} remaining steps on normal path after Fortune Road sequence.");
                        for (int remainingStep = 0; remainingStep < remainingSteps; remainingStep++)
                        {
                            currentPathIndex++;
                            
                            // Loop back to start if we reach the end
                            if (currentPathIndex >= pathWaypoints.Count)
                            {
                                currentPathIndex = 0;
                                Debug.Log("Player reached the end of the path! Looping back to start.");
                            }
                            
                            Transform remainingWaypoint = pathWaypoints[currentPathIndex];
                            Vector3 remainingPosition = remainingWaypoint.position;
                            
                            // Check if this waypoint is Path01_Start (or Path001_Start)
                            CheckAndTriggerPath01Start(remainingWaypoint);
                            
                            // Jump to the waypoint (sound plays during jump)
                            yield return StartCoroutine(JumpToPosition(remainingPosition));
                        }
                    }
                }
                else
                {
                    Debug.LogWarning("Path39 waypoint not assigned! Cannot exit Fortune Road sequence.");
                    // Fallback: exit sequence and use saved index
                    isInFortuneRoadSequence = false;
                    fortuneRoadSequenceIndex = -1;
                    shouldEnterFortuneRoad = false;
                    shouldUseOneDice = false;
                    currentPathIndex = savedNormalPathIndex;
                }
            }
            else
            {
                // Still in Fortune Road sequence, not all waypoints completed yet
                Debug.Log($"Still in Fortune Road sequence. Progress: {fortuneRoadSequenceIndex}/{fortuneRoadWaypoints.Count} waypoints visited.");
            }
        }
        else
        {
            // Normal path movement
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
                
                Transform normalWaypoint = pathWaypoints[currentPathIndex];
                Vector3 normalPosition = normalWaypoint.position;
                
                // Check if this waypoint is Path01_Start (or Path001_Start)
                CheckAndTriggerPath01Start(normalWaypoint);
                
                // Jump to the waypoint (sound plays during jump)
                yield return StartCoroutine(JumpToPosition(normalPosition));
            }
        }
        
        isMoving = false;
        string currentWaypointName = isInFortuneRoadSequence 
            ? (fortuneRoadSequenceIndex >= 0 && fortuneRoadSequenceIndex < fortuneRoadWaypoints.Count
                ? fortuneRoadWaypoints[fortuneRoadSequenceIndex].name 
                : "Fortune Road Sequence (completed)") 
            : (currentPathIndex < pathWaypoints.Count ? pathWaypoints[currentPathIndex].name : "Unknown");
        Debug.Log($"Player movement complete! Now at waypoint: {currentWaypointName}");
        
        // Check if player stopped on a Fortune Road tile - set flags for next movement
        if (!isInFortuneRoadSequence && IsCurrentWaypointFortuneRoad())
        {
            shouldEnterFortuneRoad = true;
            shouldUseOneDice = true; // Next dice roll should use only one dice
            Debug.Log($"Player stopped on Fortune Road! Next dice roll will use one dice and go through Fortune Road sequence.");
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
        shouldUseOneDice = false;
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
    /// Checks if a waypoint is Path01_Start and triggers the event if not already triggered
    /// </summary>
    private void CheckAndTriggerPath01Start(Transform waypoint)
    {
        if (waypoint != null && !passedPath01Start)
        {
            string waypointName = waypoint.name;
            if (waypointName == "Path01_Start" || waypointName == "Path001_Start")
            {
                passedPath01Start = true;
                OnPassedPath01Start?.Invoke();
                Debug.Log($"Player passed through {waypointName} during movement!");
            }
        }
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
    
    /// <summary>
    /// Set path waypoints programmatically
    /// </summary>
    public void SetPathWaypoints(List<Transform> waypoints)
    {
        if (waypoints != null)
        {
            pathWaypoints = new List<Transform>(waypoints);
            Debug.Log($"PlayerController: Assigned {pathWaypoints.Count} path waypoints");
        }
    }
    
    /// <summary>
    /// Set Fortune Road waypoints programmatically
    /// </summary>
    public void SetFortuneRoadWaypoints(List<Transform> waypoints)
    {
        if (waypoints != null)
        {
            fortuneRoadWaypoints = new List<Transform>(waypoints);
            Debug.Log($"PlayerController: Assigned {fortuneRoadWaypoints.Count} Fortune Road waypoints");
        }
    }
    
    /// <summary>
    /// Set Path39 waypoint (exit from Fortune Road)
    /// </summary>
    public void SetPath39Waypoint(Transform waypoint)
    {
        path39Waypoint = waypoint;
        if (waypoint != null)
        {
            Debug.Log($"PlayerController: Assigned Path39 waypoint: {waypoint.name}");
        }
    }
}

