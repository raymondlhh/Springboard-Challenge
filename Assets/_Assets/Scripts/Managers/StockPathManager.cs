using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Dedicated manager for handling Stock paths and StockMarket activation.
/// Separated from CardsManager to reduce complexity and improve maintainability.
/// </summary>
public class StockPathManager : MonoBehaviour
{
    [Header("Stock Paths Configuration")]
    [Tooltip("Drag and drop Stock path GameObjects here. These paths will activate the StockMarket minigame instead of spawning cards.")]
    [SerializeField] private List<GameObject> stockPaths = new List<GameObject>();
    
    [Header("References")]
    [Tooltip("Reference to StockManager. Will auto-find if not assigned.")]
    [SerializeField] private StockManager stockManager;
    
    [Tooltip("Reference to PlayerManager. Will auto-find if not assigned.")]
    [SerializeField] private PlayerManager playerManager;
    
    [Header("Debug Settings")]
    [SerializeField] private bool enableDebugLogs = true;
    
    private bool isProcessingStockPath = false;
    
    private void Start()
    {
        // Find PlayerManager if not assigned
        if (playerManager == null)
        {
            playerManager = FindAnyObjectByType<PlayerManager>();
        }
        
        // Find StockManager if not assigned
        if (stockManager == null)
        {
            stockManager = FindAnyObjectByType<StockManager>();
        }
        
        // Subscribe to PlayerManager events
        if (playerManager != null)
        {
            playerManager.OnCurrentPlayerChanged += OnCurrentPlayerChanged;
            
            // Subscribe to current player's movement complete event
            if (playerManager.CurrentPlayer != null)
            {
                SubscribeToCurrentPlayerEvents();
            }
        }
        
        // Use coroutine to ensure PlayerManager has initialized
        StartCoroutine(SubscribeToCurrentPlayerEventsDelayed());
    }
    
    /// <summary>
    /// Called when the current player changes
    /// </summary>
    private void OnCurrentPlayerChanged(Player newPlayer)
    {
        // Unsubscribe from old player
        UnsubscribeFromPlayerEvents();
        
        // Subscribe to new player
        SubscribeToCurrentPlayerEvents();
        
        if (enableDebugLogs)
        {
            Debug.Log($"[StockPathManager] Current player changed to {newPlayer?.PlayerName ?? "null"}");
        }
    }
    
    /// <summary>
    /// Subscribe to current player's events with a delay to ensure PlayerManager has initialized
    /// </summary>
    private IEnumerator SubscribeToCurrentPlayerEventsDelayed()
    {
        // Wait one frame to ensure PlayerManager.Start() has executed
        yield return null;
        
        // Retry up to 10 frames if CurrentPlayer is still null
        int retryCount = 0;
        while (playerManager != null && playerManager.CurrentPlayer == null && retryCount < 10)
        {
            yield return null;
            retryCount++;
        }
        
        // Now subscribe to current player events
        SubscribeToCurrentPlayerEvents();
    }
    
    /// <summary>
    /// Subscribe to current player's events
    /// </summary>
    private void SubscribeToCurrentPlayerEvents()
    {
        // First unsubscribe from any previous player to prevent duplicate subscriptions
        UnsubscribeFromPlayerEvents();
        
        if (playerManager == null)
        {
            if (enableDebugLogs)
            {
                Debug.LogWarning("[StockPathManager] Cannot subscribe: PlayerManager is null");
            }
            return;
        }
        
        if (playerManager.CurrentPlayer == null)
        {
            if (enableDebugLogs)
            {
                Debug.LogWarning("[StockPathManager] Cannot subscribe: CurrentPlayer is null");
            }
            return;
        }
        
        PlayerController currentPlayerCtrl = GetCurrentPlayerController();
        if (currentPlayerCtrl != null)
        {
            currentPlayerCtrl.OnMovementComplete += OnPlayerMovementComplete;
            if (enableDebugLogs)
            {
                Debug.Log($"[StockPathManager] ✓ Successfully subscribed to OnMovementComplete for player: {playerManager.CurrentPlayer.PlayerName}");
            }
        }
        else
        {
            if (enableDebugLogs)
            {
                Debug.LogError($"[StockPathManager] ✗ Cannot subscribe: PlayerController is null for player {playerManager.CurrentPlayer.PlayerName}.");
            }
        }
    }
    
    /// <summary>
    /// Unsubscribe from player events
    /// </summary>
    private void UnsubscribeFromPlayerEvents()
    {
        PlayerController currentPlayerCtrl = GetCurrentPlayerController();
        if (currentPlayerCtrl != null)
        {
            currentPlayerCtrl.OnMovementComplete -= OnPlayerMovementComplete;
            if (enableDebugLogs)
            {
                Debug.Log($"[StockPathManager] Unsubscribed from player events for: {playerManager?.CurrentPlayer?.PlayerName ?? "Unknown"}");
            }
        }
    }
    
    /// <summary>
    /// Get the current player's PlayerController
    /// </summary>
    private PlayerController GetCurrentPlayerController()
    {
        if (playerManager != null && playerManager.CurrentPlayer != null)
        {
            return playerManager.CurrentPlayer.PlayerController;
        }
        return null;
    }
    
    /// <summary>
    /// Called when player movement is complete
    /// </summary>
    private void OnPlayerMovementComplete()
    {
        // Prevent duplicate processing
        if (isProcessingStockPath)
        {
            if (enableDebugLogs)
            {
                Debug.LogWarning("[StockPathManager] Already processing a stock path, ignoring duplicate call.");
            }
            return;
        }
        
        string playerName = playerManager?.CurrentPlayer?.PlayerName ?? "Unknown";
        
        // Get the current player's controller
        PlayerController currentPlayerCtrl = GetCurrentPlayerController();
        if (currentPlayerCtrl == null)
        {
            if (enableDebugLogs)
            {
                Debug.LogWarning($"[StockPathManager] Current player controller is null! Cannot process path. Player: {playerName}");
            }
            return;
        }
        
        // Get the current waypoint name from player
        string waypointName = currentPlayerCtrl.GetCurrentWaypointName();
        if (string.IsNullOrEmpty(waypointName))
        {
            if (enableDebugLogs)
            {
                Debug.LogWarning("[StockPathManager] Waypoint name is empty! Cannot process path.");
            }
            return;
        }
        
        // Normalize the waypoint name
        waypointName = waypointName.Trim();
        
        if (enableDebugLogs)
        {
            Debug.Log($"[StockPathManager] ===== Player {playerName} stopped at path: '{waypointName}' =====");
        }
        
        // Check if this is a Stock path
        if (IsStockPath(waypointName))
        {
            isProcessingStockPath = true;
            
            try
            {
                ActivateStockMarket(playerName, waypointName);
            }
            finally
            {
                isProcessingStockPath = false;
            }
        }
    }
    
    /// <summary>
    /// Checks if a path is a Stock path
    /// </summary>
    public bool IsStockPath(string pathName)
    {
        if (string.IsNullOrEmpty(pathName))
        {
            return false;
        }
        
        // Normalize the input path name
        string normalizedPathName = pathName.Trim();
        
        // Check if path is in the Stock paths list
        bool isInStockPathsList = IsPathInStockPathsList(normalizedPathName);
        
        // Fallback: Check if path name contains "Stocks" keyword
        bool containsStocksKeyword = normalizedPathName.Contains("Stocks", System.StringComparison.OrdinalIgnoreCase);
        
        bool isStockPath = isInStockPathsList || containsStocksKeyword;
        
        if (enableDebugLogs && isStockPath)
        {
            Debug.Log($"[StockPathManager] Stock path detected: '{normalizedPathName}' (inList: {isInStockPathsList}, hasKeyword: {containsStocksKeyword})");
        }
        
        return isStockPath;
    }
    
    /// <summary>
    /// Checks if a path is in the Stock paths list
    /// </summary>
    private bool IsPathInStockPathsList(string pathName)
    {
        if (stockPaths == null || stockPaths.Count == 0)
        {
            return false;
        }
        
        // Normalize the input path name
        string normalizedPathName = pathName?.Trim() ?? string.Empty;
        
        if (string.IsNullOrEmpty(normalizedPathName))
        {
            return false;
        }
        
        // Check for exact match (case-insensitive) by comparing GameObject names
        foreach (GameObject pathObject in stockPaths)
        {
            if (pathObject == null)
            {
                continue;
            }
            
            // Normalize the GameObject's name
            string objectName = pathObject.name?.Trim() ?? string.Empty;
            
            if (string.IsNullOrEmpty(objectName))
            {
                continue;
            }
            
            // Compare the GameObject's name with the path name (case-insensitive)
            if (normalizedPathName.Equals(objectName, System.StringComparison.OrdinalIgnoreCase))
            {
                if (enableDebugLogs)
                {
                    Debug.Log($"[StockPathManager] Path '{normalizedPathName}' matched stock paths list entry: '{objectName}'");
                }
                return true;
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// Activates the StockMarket minigame for the specified player
    /// </summary>
    private void ActivateStockMarket(string playerName, string waypointName)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[StockPathManager] STOCKS PATH DETECTED: '{waypointName}' (Player: {playerName})");
        }
        
        // Always try to find StockManager fresh (don't cache to avoid stale references)
        // This ensures it works for all players, including the first one
        StockManager currentStockManager = stockManager;
        if (currentStockManager == null)
        {
            currentStockManager = FindAnyObjectByType<StockManager>();
            if (currentStockManager != null)
            {
                stockManager = currentStockManager; // Cache it for next time
                if (enableDebugLogs)
                {
                    Debug.Log($"[StockPathManager] StockManager found and assigned. (Player: {playerName})");
                }
            }
        }
        
        if (currentStockManager == null)
        {
            Debug.LogError($"[StockPathManager] StockManager not found in scene! Cannot activate minigame for Stocks path: '{waypointName}' (Player: {playerName})");
            return;
        }
        
        // Activate the minigame
        if (enableDebugLogs)
        {
            Debug.Log($"[StockPathManager] Activating StockMarket for player: {playerName}");
        }
        
        currentStockManager.ActivateMiniGame();
        
        if (enableDebugLogs)
        {
            Debug.Log($"[StockPathManager] ✓ StockMarket activation requested for path: '{waypointName}' (Player: {playerName})");
        }
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from player events
        UnsubscribeFromPlayerEvents();
        
        // Unsubscribe from PlayerManager events
        if (playerManager != null)
        {
            playerManager.OnCurrentPlayerChanged -= OnCurrentPlayerChanged;
        }
    }
}
