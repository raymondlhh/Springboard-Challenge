using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CardsManager : MonoBehaviour
{
    [Header("Card Prefabs")]
    [SerializeField] private GameObject[] businessCards = new GameObject[3];
    [SerializeField] private GameObject[] chanceCards = new GameObject[4];
    [SerializeField] private GameObject[] marketWatchCards = new GameObject[3];
    [SerializeField] private GameObject[] realEstateCards = new GameObject[3];
    [SerializeField] private GameObject[] unitTrustEquitiesCards = new GameObject[3];
    [SerializeField] private GameObject[] unitTrustFixedIncomeCards = new GameObject[3];
    [SerializeField] private GameObject[] fortuneRoadCards = new GameObject[5];
    
    [Header("Paths Without Cards")]
    [Tooltip("Drag and drop path GameObjects here. These paths will NOT spawn any cards. Player will skip card and spawn dice directly.")]
    [SerializeField] private List<GameObject> pathsWithoutCards = new List<GameObject>();
    
    [Header("Stock Path Manager Reference")]
    [Tooltip("Reference to StockPathManager. Will auto-find if not assigned. Handles all Stock path logic separately.")]
    [SerializeField] private StockPathManager stockPathManager;
    
    [Header("Card Spawn Points")]
    [SerializeField] private Transform cardsStartPath;
    [SerializeField] private Transform cardsEndPath;
    
    [Header("Card Animation Settings")]
    [Tooltip("Card movement speed. Higher values = faster movement. (Speed of 1 = 2 seconds duration, Speed of 2 = 1 second duration)")]
    [SerializeField] private float cardMoveSpeed = 1f;
    [Tooltip("Time in seconds the card waits before being destroyed after reaching the end position")]
    [SerializeField] private float cardWaitDuration = 3f;
    
    [Header("Player Manager Reference")]
    [Tooltip("Reference to PlayerManager. Will auto-find if not assigned. Supports 1-4 players.")]
    [SerializeField] private PlayerManager playerManager;
    
    [Header("Debug Manager Reference")]
    [Tooltip("Legacy reference - Stock path handling is now done by StockPathManager. This is kept for backward compatibility.")]
    [SerializeField] private StockManager debugManager;
    
    [Header("Real Estate Settings")]
    [SerializeField] private RealEstateData realEstateData;
    [SerializeField] private RealEstateUI realEstateUIController;
    
    [Header("Business Settings")]
    [SerializeField] private BusinessData businessData;
    [SerializeField] private BusinessUI businessUIController;
    
    [Header("Market Watch Settings")]
    [SerializeField] private MarketWatchData marketWatchData;
    [SerializeField] private MarketWatchUI marketWatchUIController;
    
    private bool isCardAnimating = false;
    private CardController currentRealEstateCard;
    private string currentRealEstatePathName;
    private CardController currentBusinessCard;
    private string currentBusinessPathName;
    private CardController currentMarketWatchCard;
    private string currentMarketWatchPathName;
    private bool isProcessingPath = false; // Flag to prevent duplicate processing
    private PlayerController subscribedPlayerController = null; // Track which PlayerController we're subscribed to
    private int subscribedPlayerID = -1; // Track which player ID we're subscribed to (for verification)
    
    public bool IsCardAnimating => isCardAnimating;
    
    /// <summary>
    /// Checks if a card will be spawned for the given path name
    /// </summary>
    public bool WillSpawnCardForPath(string pathName)
    {
        if (string.IsNullOrEmpty(pathName))
        {
            return false;
        }
        
        // Check if this is a Stock path (handled by StockPathManager - highest priority)
        if (stockPathManager != null && stockPathManager.IsStockPath(pathName))
        {
            Debug.Log($"Path '{pathName}' is a Stock path. No card will spawn (will activate StockMarket instead).");
            return false;
        }
        
        // Fallback: Check if path contains "Stocks" keyword (in case StockPathManager is not available)
        if (pathName.Contains("Stocks", System.StringComparison.OrdinalIgnoreCase))
        {
            Debug.Log($"Path '{pathName}' contains 'Stocks' keyword. No card will spawn.");
            return false;
        }
        
        // Check if this path is in the "paths without cards" list
        if (IsPathInNullCardsList(pathName))
        {
            Debug.Log($"Path '{pathName}' is in the null cards list. No card will spawn.");
            return false;
        }
        
        var (cardPrefab, _) = GetCardPrefabForPath(pathName);
        return cardPrefab != null;
    }
    
    /// <summary>
    /// Checks if a path is in the list of paths that should not spawn cards
    /// </summary>
    private bool IsPathInNullCardsList(string pathName)
    {
        if (pathsWithoutCards == null || pathsWithoutCards.Count == 0)
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
        foreach (GameObject pathObject in pathsWithoutCards)
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
                Debug.Log($"[CardsManager] Path '{normalizedPathName}' matched null cards list entry: '{objectName}'");
                return true;
            }
        }
        
        return false;
    }
    
    void Start()
    {
        // Find PlayerManager if not assigned
        if (playerManager == null)
        {
            playerManager = FindAnyObjectByType<PlayerManager>();
        }
        
        // Subscribe to PlayerManager events
        if (playerManager != null)
        {
            playerManager.OnCurrentPlayerChanged += OnCurrentPlayerChanged;
            
            // Immediately check if there's already a current player (in case PlayerManager initialized first)
            // and subscribe to them right away, not just in the delayed coroutine
            if (playerManager.CurrentPlayer != null)
            {
                Debug.Log($"[CardsManager] Found existing CurrentPlayer ({playerManager.CurrentPlayer.PlayerName}) during Start(). Subscribing immediately.");
                SubscribeToCurrentPlayerEvents();
            }
        }
        
        // Find StockPathManager if not assigned
        if (stockPathManager == null)
        {
            stockPathManager = FindAnyObjectByType<StockPathManager>();
            if (stockPathManager != null)
            {
                Debug.Log("[CardsManager] StockPathManager found. Stock paths will be handled by StockPathManager.");
            }
            else
            {
                Debug.LogWarning("[CardsManager] StockPathManager not found. Stock path detection may not work correctly.");
            }
        }
        
        // Find card spawn points if not assigned
        if (cardsStartPath == null || cardsEndPath == null)
        {
            FindCardSpawnPoints();
        }
        
        // Find DebugManager if not assigned (legacy - kept for backward compatibility)
        if (debugManager == null)
        {
            debugManager = FindAnyObjectByType<StockManager>();
        }
        
        // Find RealEstateUIController if not assigned
        if (realEstateUIController == null)
        {
            GameObject forSaleUIObj = GameObject.Find("ForSaleUI");
            if (forSaleUIObj != null)
            {
                realEstateUIController = forSaleUIObj.GetComponent<RealEstateUI>();
            }
            
            if (realEstateUIController == null)
            {
                realEstateUIController = FindAnyObjectByType<RealEstateUI>();
            }
        }
        
        // Subscribe to RealEstateUI events
        if (realEstateUIController != null)
        {
            realEstateUIController.OnPurchaseComplete += OnRealEstatePurchaseComplete;
            realEstateUIController.OnPurchaseCancelled += OnRealEstatePurchaseCancelled;
        }
        
        // Find BusinessUIController if not assigned
        if (businessUIController == null)
        {
            GameObject businessUIObj = GameObject.Find("BusinessUI");
            if (businessUIObj != null)
            {
                businessUIController = businessUIObj.GetComponent<BusinessUI>();
            }
            
            if (businessUIController == null)
            {
                businessUIController = FindAnyObjectByType<BusinessUI>();
            }
        }
        
        // Subscribe to BusinessUI events
        if (businessUIController != null)
        {
            businessUIController.OnPurchaseComplete += OnBusinessPurchaseComplete;
            businessUIController.OnPurchaseCancelled += OnBusinessPurchaseCancelled;
        }
        
        // Find MarketWatchUIController if not assigned
        if (marketWatchUIController == null)
        {
            GameObject marketWatchUIObj = GameObject.Find("MarketWatchUI");
            if (marketWatchUIObj != null)
            {
                marketWatchUIController = marketWatchUIObj.GetComponent<MarketWatchUI>();
            }
            
            if (marketWatchUIController == null)
            {
                marketWatchUIController = FindAnyObjectByType<MarketWatchUI>();
            }
        }
        
        // Subscribe to MarketWatchUI events
        if (marketWatchUIController != null)
        {
            marketWatchUIController.OnEffectComplete += OnMarketWatchEffectComplete;
        }
        
        // Subscribe to current player's movement complete event
        // Use a coroutine to ensure PlayerManager has initialized first
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
        
        Debug.Log($"[CardsManager] Current player changed to {newPlayer?.PlayerName ?? "null"}");
        
        // Verify subscription worked
        if (newPlayer != null)
        {
            PlayerController currentPlayerCtrl = GetCurrentPlayerController();
            if (currentPlayerCtrl == null)
            {
                Debug.LogWarning($"[CardsManager] OnCurrentPlayerChanged: Player {newPlayer.PlayerName} exists but PlayerController is null. Subscription may have failed.");
            }
            else
            {
                Debug.Log($"[CardsManager] Successfully subscribed to {newPlayer.PlayerName}'s OnMovementComplete event.");
            }
        }
    }
    
    /// <summary>
    /// Subscribe to current player's events with a delay to ensure PlayerManager has initialized
    /// </summary>
    private IEnumerator SubscribeToCurrentPlayerEventsDelayed()
    {
        // Wait one frame to ensure PlayerManager.Start() has executed
        yield return null;
        
        // Retry up to 10 frames if CurrentPlayer is still null (in case PlayerManager initializes later)
        int retryCount = 0;
        while (playerManager != null && playerManager.CurrentPlayer == null && retryCount < 10)
        {
            yield return null;
            retryCount++;
        }
        
        // Now subscribe to current player events
        // This handles the case where OnCurrentPlayerChanged was fired before we subscribed to it
        SubscribeToCurrentPlayerEvents();
        
        // Also verify subscription worked - if CurrentPlayer exists but we couldn't subscribe,
        // log a warning to help debug
        if (playerManager != null && playerManager.CurrentPlayer != null)
        {
            PlayerController currentPlayerCtrl = GetCurrentPlayerController();
            if (currentPlayerCtrl == null)
            {
                Debug.LogWarning($"[CardsManager] Delayed subscription: CurrentPlayer exists ({playerManager.CurrentPlayer.PlayerName}) but PlayerController is null. This may cause issues.");
            }
            else
            {
                Debug.Log($"[CardsManager] Delayed subscription successful for: {playerManager.CurrentPlayer.PlayerName}");
            }
        }
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
            Debug.LogWarning("[CardsManager] Cannot subscribe: PlayerManager is null");
            return;
        }
        
        if (playerManager.CurrentPlayer == null)
        {
            Debug.LogWarning("[CardsManager] Cannot subscribe: CurrentPlayer is null");
            return;
        }
        
        PlayerController currentPlayerCtrl = GetCurrentPlayerController();
        if (currentPlayerCtrl != null)
        {
            currentPlayerCtrl.OnMovementComplete += OnPlayerMovementComplete;
            subscribedPlayerController = currentPlayerCtrl; // Store reference to subscribed controller
            subscribedPlayerID = playerManager.CurrentPlayer.PlayerID; // Store player ID for verification
            Debug.Log($"[CardsManager] ✓ Successfully subscribed to OnMovementComplete for player: {playerManager.CurrentPlayer.PlayerName} (ID: {subscribedPlayerID})");
        }
        else
        {
            subscribedPlayerController = null; // Clear reference if subscription failed
            subscribedPlayerID = -1; // Clear player ID
            Debug.LogError($"[CardsManager] ✗ Cannot subscribe: PlayerController is null for player {playerManager.CurrentPlayer.PlayerName}. This will prevent path processing!");
        }
    }
    
    /// <summary>
    /// Unsubscribe from player events
    /// </summary>
    private void UnsubscribeFromPlayerEvents()
    {
        // Unsubscribe from the previously subscribed PlayerController
        if (subscribedPlayerController != null)
        {
            subscribedPlayerController.OnMovementComplete -= OnPlayerMovementComplete;
            Debug.Log($"[CardsManager] Unsubscribed from player events for: {playerManager?.CurrentPlayer?.PlayerName ?? "Unknown"}");
            subscribedPlayerController = null; // Clear reference
            subscribedPlayerID = -1; // Clear player ID
        }
        else
        {
            // Fallback: Try to unsubscribe from current player if we don't have a stored reference
            PlayerController currentPlayerCtrl = GetCurrentPlayerController();
            if (currentPlayerCtrl != null)
            {
                currentPlayerCtrl.OnMovementComplete -= OnPlayerMovementComplete;
            }
            subscribedPlayerID = -1; // Clear player ID
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
        
        // Return null silently - this is expected during initialization
        return null;
    }
    
    
    private void FindCardSpawnPoints()
    {
        GameObject startObj = GameObject.Find("CardsStartPath");
        GameObject endObj = GameObject.Find("CardsEndPath");
        
        if (startObj != null)
        {
            cardsStartPath = startObj.transform;
        }
        
        if (endObj != null)
        {
            cardsEndPath = endObj.transform;
        }
    }
    
    private void OnPlayerMovementComplete()
    {
        // CRITICAL FIX: Only process paths when the movement is from the CURRENT player
        // This prevents processing paths for players whose turn has already ended
        // The event might fire after the turn has switched, so we need to verify the current player
        
        // First, check if we have a valid current player
        if (playerManager == null || playerManager.CurrentPlayer == null)
        {
            Debug.LogWarning("[CardsManager] OnPlayerMovementComplete called but CurrentPlayer is null. Ignoring.");
            return;
        }
        
        // CRITICAL VERIFICATION: Check if the current player matches the player we're subscribed to
        // This is the most reliable check - if the player ID doesn't match, the turn has switched
        int currentPlayerID = playerManager.CurrentPlayer.PlayerID;
        if (subscribedPlayerID >= 0 && currentPlayerID != subscribedPlayerID)
        {
            Debug.LogWarning($"[CardsManager] OnPlayerMovementComplete called but turn has already switched. Ignoring event from previous player. (Subscribed to Player ID: {subscribedPlayerID}, Current Player ID: {currentPlayerID}, Current Player: {playerManager.CurrentPlayer.PlayerName})");
            return;
        }
        
        // Get the current player's controller
        PlayerController currentPlayerCtrl = GetCurrentPlayerController();
        
        // Additional verification: Ensure the subscribed controller matches the current player's controller
        if (subscribedPlayerController != null && currentPlayerCtrl != subscribedPlayerController)
        {
            Debug.LogWarning($"[CardsManager] OnPlayerMovementComplete called but PlayerController doesn't match subscribed controller. Ignoring. (Subscribed to: {subscribedPlayerController.name}, Current: {currentPlayerCtrl?.name ?? "null"})");
            return;
        }
        
        if (currentPlayerCtrl == null)
        {
            Debug.LogWarning("[CardsManager] OnPlayerMovementComplete called but CurrentPlayer's controller is null. Ignoring.");
            return;
        }
        
        // CRITICAL: Verify that the player has actually finished moving (not just standing at a path)
        // This prevents processing paths when a player's turn starts (they might be standing at a path from a previous turn)
        // The IsMoving flag should be false when movement completes, but we check to be safe
        if (currentPlayerCtrl.IsMoving)
        {
            Debug.LogWarning("[CardsManager] OnPlayerMovementComplete called but player is still moving. This shouldn't happen. Ignoring.");
            return;
        }
        
        // Use the current player's controller (which should match subscribed controller)
        PlayerController controllerToUse = currentPlayerCtrl;
        
        string playerName = playerManager.CurrentPlayer.PlayerName;
        Debug.Log($"[CardsManager] OnPlayerMovementComplete called for player: {playerName} (Player ID: {currentPlayerID})");
        
        // Prevent duplicate processing
        if (isProcessingPath)
        {
            Debug.LogWarning("[CardsManager] Already processing a path, ignoring duplicate call.");
            return;
        }
        
        isProcessingPath = true;
        
        try
        {
            // Get the waypoint name from the current player's controller
            // This ensures we're processing the path for the correct player
            string waypointName = controllerToUse.GetCurrentWaypointName();
            
            if (string.IsNullOrEmpty(waypointName))
            {
                Debug.LogWarning("[CardsManager] Waypoint name is empty! Cannot process path.");
                return;
            }
            
            // Normalize the waypoint name (trim whitespace)
            waypointName = waypointName.Trim();
            
            Debug.Log($"[CardsManager] ===== Player {playerName} stopped at path: '{waypointName}' =====");
            
            // Get GameManager reference once for use throughout this method
            GameManager gameManager = FindAnyObjectByType<GameManager>();
            
            // PRIORITY 1: Check if this is a Stock path (handled by StockPathManager)
            // StockPathManager handles its own subscription to player movement events,
            // so we just need to skip card spawning for stock paths
            bool isStockPath = false;
            if (stockPathManager != null)
            {
                isStockPath = stockPathManager.IsStockPath(waypointName);
            }
            else
            {
                // Fallback: Check if path contains "Stocks" keyword if StockPathManager is not available
                isStockPath = waypointName.Contains("Stocks", System.StringComparison.OrdinalIgnoreCase);
            }
            
            if (isStockPath)
            {
                Debug.Log($"[CardsManager] Stock path detected: '{waypointName}' (Player: {playerName}). Skipping card spawn - StockPathManager will handle activation.");
                // StockPathManager handles the activation, we just skip card spawning
                return;
            }
            
            // PRIORITY 2: Check if this path is in the "paths without cards" list
            bool isInNullList = IsPathInNullCardsList(waypointName);
            if (isInNullList)
            {
                Debug.Log($"[CardsManager] ✓ Path '{waypointName}' is in the null cards list. No card will spawn. (Player: {playerName})");
                return;
            }
            
            // PRIORITY 3: Check if this is a Real Estate or Business path that's already owned
            bool isRealEstatePath = IsRealEstatePath(waypointName);
            bool isBusinessPath = IsBusinessPath(waypointName);
            
            if (isRealEstatePath || isBusinessPath)
            {
                // Check if property is already owned
                Player propertyOwner = null;
                if (playerManager != null)
                {
                    propertyOwner = playerManager.FindPropertyOwner(waypointName);
                }
                
                if (propertyOwner != null)
                {
                    // Property is owned - check if visitor is different from owner
                    Player currentPlayer = playerManager?.CurrentPlayer;
                    if (currentPlayer != null && currentPlayer != propertyOwner)
                    {
                        // Other player visiting owned property - handle visit instead of spawning card
                        Debug.Log($"[CardsManager] Property at path '{waypointName}' is owned by {propertyOwner.PlayerName}. Handling visit instead of spawning card. (Visitor: {playerName})");
                        HandlePropertyVisit(waypointName, isRealEstatePath, isBusinessPath, propertyOwner);
                        return;
                    }
                    else if (currentPlayer == propertyOwner)
                    {
                        // Owner visiting their own property - no card spawn, no income (they already have the monthly income)
                        Debug.Log($"[CardsManager] Property at path '{waypointName}' is owned by current player {playerName}. No card spawn, no visit income.");
                        // Spawn dice
                        if (gameManager != null)
                        {
                            gameManager.SpawnDice();
                        }
                        return;
                    }
                }
            }
            
            // PRIORITY 4: Check if cards can spawn (IsFirstRound check)
            if (gameManager != null && !gameManager.CanSpawnCards())
            {
                Debug.Log($"[CardsManager] IsFirstRound is enabled and player hasn't passed Path01_Start yet. Skipping card spawn for path: '{waypointName}' (Player: {playerName})");
                // Spawn dice to continue the game
                gameManager.SpawnDice();
                return;
            }
            
            // PRIORITY 5: Only spawn card if one should be spawned for this path
            bool willSpawn = WillSpawnCardForPath(waypointName);
            if (willSpawn)
            {
                Debug.Log($"[CardsManager] → Spawning card for path: '{waypointName}' (Player: {playerName})");
                SpawnCardForPath(waypointName);
            }
            else
            {
                Debug.Log($"[CardsManager] → No card will be spawned for path: '{waypointName}' (Player: {playerName})");
            }
        }
        finally
        {
            isProcessingPath = false;
        }
    }
    
    /// <summary>
    /// Spawns a card based on the path name
    /// </summary>
    private void SpawnCardForPath(string pathName)
    {
        if (string.IsNullOrEmpty(pathName))
        {
            Debug.LogWarning("Path name is empty! Cannot determine which card to spawn.");
            return;
        }
        
        // Check if spawn points are available
        if (cardsStartPath == null || cardsEndPath == null)
        {
            Debug.LogWarning("Card spawn points not found! Cannot spawn card.");
            return;
        }
        
        var (cardPrefab, isRandomSelection) = GetCardPrefabForPath(pathName);
        
        if (cardPrefab == null)
        {
            Debug.LogWarning($"No card found for path: {pathName}");
            return;
        }
        
        // Spawn the card
        GameObject spawnedCard = Instantiate(cardPrefab);
        spawnedCard.name = cardPrefab.name;
        
        // Get or add CardController
        CardController cardController = spawnedCard.GetComponent<CardController>();
        if (cardController == null)
        {
            cardController = spawnedCard.AddComponent<CardController>();
        }
        
        // Check if this is a RealEstate, Business, or MarketWatch path
        bool isRealEstatePath = IsRealEstatePath(pathName);
        bool isBusinessPath = IsBusinessPath(pathName);
        bool isMarketWatchPath = IsMarketWatchPath(pathName);
        bool waitForInput = isRealEstatePath || isBusinessPath || isMarketWatchPath;
        
        // If it's a RealEstate path, subscribe to card reached end event
        if (isRealEstatePath)
        {
            currentRealEstateCard = cardController;
            currentRealEstatePathName = pathName;
            cardController.OnCardReachedEnd += OnRealEstateCardReachedEnd;
        }
        
        // If it's a Business path, subscribe to card reached end event
        if (isBusinessPath)
        {
            currentBusinessCard = cardController;
            currentBusinessPathName = pathName;
            cardController.OnCardReachedEnd += OnBusinessCardReachedEnd;
        }
        
        // If it's a MarketWatch path, subscribe to card reached end event
        if (isMarketWatchPath)
        {
            currentMarketWatchCard = cardController;
            currentMarketWatchPathName = pathName;
            cardController.OnCardReachedEnd += OnMarketWatchCardReachedEnd;
        }
        
        // Start card animation with the configured speed and wait duration
        // Convert speed to duration: higher speed = lower duration (faster movement)
        // Base duration of 2 seconds divided by speed
        float moveDuration = 2f / Mathf.Max(0.1f, cardMoveSpeed); // Prevent division by zero
        isCardAnimating = true;
        cardController.AnimateCard(cardsStartPath, cardsEndPath, moveDuration, cardWaitDuration, waitForInput);
        
        // Monitor when card animation completes (only for non-RealEstate, non-Business, and non-MarketWatch cards)
        if (!isRealEstatePath && !isBusinessPath && !isMarketWatchPath)
        {
            StartCoroutine(WaitForCardAnimation(cardController));
        }
        
        // Log with indication of random selection
        if (isRandomSelection)
        {
            string categoryName = ExtractCategoryFromPath(pathName);
            Debug.Log($"Spawned card: {cardPrefab.name} for path: {pathName} [Randomly selected from {categoryName} Cards]");
        }
        else
        {
            Debug.Log($"Spawned card: {cardPrefab.name} for path: {pathName}");
        }
    }
    
    /// <summary>
    /// Gets the appropriate card prefab based on the path name
    /// Returns a tuple with the card prefab and a boolean indicating if it was randomly selected
    /// Returns null if no card should be spawned for this path
    /// </summary>
    private (GameObject cardPrefab, bool isRandomSelection) GetCardPrefabForPath(string pathName)
    {
        // Normalize path name (remove spaces, handle case)
        string normalizedPath = pathName.Trim();
        
        // Check if this is a Stock path (handled by StockPathManager - highest priority)
        if (stockPathManager != null && stockPathManager.IsStockPath(normalizedPath))
        {
            Debug.Log($"Path '{normalizedPath}' is a Stock path. Returning null card (will activate StockMarket instead).");
            return (null, false);
        }
        
        // Fallback: Check if path contains "Stocks" keyword (in case StockPathManager is not available)
        if (normalizedPath.Contains("Stocks", System.StringComparison.OrdinalIgnoreCase))
        {
            Debug.Log($"Path '{normalizedPath}' contains 'Stocks' keyword. Returning null card.");
            return (null, false);
        }
        
        // Check if this path is in the "paths without cards" list
        if (IsPathInNullCardsList(normalizedPath))
        {
            Debug.Log($"Path '{normalizedPath}' is in the null cards list. Returning null card.");
            return (null, false);
        }
        
        // Extract category name from path format: "PathXX_Category" -> "Category"
        string categoryName = ExtractCategoryFromPath(normalizedPath);
        
        // If category name is empty or same as original path, it means the path doesn't follow the expected format
        // Only proceed if we have a valid category name
        if (string.IsNullOrEmpty(categoryName) || categoryName.Equals(normalizedPath, System.StringComparison.OrdinalIgnoreCase))
        {
            // Path doesn't match expected format "PathXX_Category", so no card should spawn
            return (null, false);
        }
        
        // Check if category name ends with digits (e.g., "Business01", "RealEstate02")
        // This indicates a specific card, not a category
        bool isSpecificCard = System.Text.RegularExpressions.Regex.IsMatch(categoryName, @"\d+$");
        
        // Check for exact match first (e.g., "Business01", "Chance02", etc.)
        GameObject exactMatch = FindExactCardMatch(categoryName);
        if (exactMatch != null)
        {
            return (exactMatch, false); // Exact match, not random
        }
        
        // Check for FortuneRoad paths first (exact name matching)
        // FortuneRoad paths are like "FortuneRoad01_PropertyCashflow", "FortuneRoad02_BusinessIncome", etc.
        if (normalizedPath.StartsWith("FortuneRoad", System.StringComparison.OrdinalIgnoreCase))
        {
            // Try to find exact match in fortuneRoadCards array first
            GameObject fortuneRoadCard = FindExactCardMatchInArray(fortuneRoadCards, normalizedPath);
            if (fortuneRoadCard != null)
            {
                return (fortuneRoadCard, false); // Exact match, not random
            }
            
            // If exact match not found, try to match by number (FortuneRoad01 -> FortuneRoad001, etc.)
            // Extract number from path (e.g., "FortuneRoad01_PropertyCashflow" -> 1)
            var match = System.Text.RegularExpressions.Regex.Match(normalizedPath, @"FortuneRoad(\d+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (match.Success && int.TryParse(match.Groups[1].Value, out int waypointNumber))
            {
                // Try to find card with matching number (handle "01" vs "001" formats)
                // Look for cards named like "FortuneRoad001", "FortuneRoad01", etc.
                foreach (GameObject card in fortuneRoadCards)
                {
                    if (card != null)
                    {
                        string cardName = card.name;
                        // Extract number from card name
                        var cardMatch = System.Text.RegularExpressions.Regex.Match(cardName, @"FortuneRoad(\d+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                        if (cardMatch.Success && int.TryParse(cardMatch.Groups[1].Value, out int cardNumber))
                        {
                            // Match if numbers are the same (e.g., 1 matches 1, regardless of "01" vs "001" format)
                            if (cardNumber == waypointNumber)
                            {
                                return (card, false);
                            }
                        }
                    }
                }
            }
            
            // No FortuneRoad card found, return null (don't spawn random card)
            return (null, false);
        }
        
        // Check for category match (e.g., "Business", "Chance", "FixedIncome", etc.)
        // IMPORTANT: Only match if the category name EXACTLY matches a known category prefix
        // AND there are actual cards assigned in the Inspector for that category
        // This prevents random matching of unrelated paths
        GameObject randomCard = null;
        bool isKnownCategory = false;
        string matchedCategory = null;
        
        // Check each known category with strict matching
        // Only match if category name starts with the known prefix AND we have cards for it
        if (categoryName.StartsWith("Business", System.StringComparison.OrdinalIgnoreCase))
        {
            // Check if we actually have Business cards assigned
            if (HasValidCardsInArray(businessCards))
            {
                randomCard = GetRandomCardFromArray(businessCards, "Business");
                isKnownCategory = true;
                matchedCategory = "Business";
            }
        }
        else if (categoryName.StartsWith("Chance", System.StringComparison.OrdinalIgnoreCase))
        {
            if (HasValidCardsInArray(chanceCards))
            {
                randomCard = GetRandomCardFromArray(chanceCards, "Chance");
                isKnownCategory = true;
                matchedCategory = "Chance";
            }
        }
        else if (categoryName.StartsWith("MarketWatch", System.StringComparison.OrdinalIgnoreCase))
        {
            if (HasValidCardsInArray(marketWatchCards))
            {
                randomCard = GetRandomCardFromArray(marketWatchCards, "MarketWatch");
                isKnownCategory = true;
                matchedCategory = "MarketWatch";
            }
        }
        else if (categoryName.StartsWith("RealEstate", System.StringComparison.OrdinalIgnoreCase))
        {
            if (HasValidCardsInArray(realEstateCards))
            {
                randomCard = GetRandomCardFromArray(realEstateCards, "RealEstate");
                isKnownCategory = true;
                matchedCategory = "RealEstate";
            }
        }
        else if (categoryName.StartsWith("UnitTrustEquities", System.StringComparison.OrdinalIgnoreCase) || 
                 categoryName.StartsWith("MutualFundEquities", System.StringComparison.OrdinalIgnoreCase) ||
                 categoryName.StartsWith("Equities", System.StringComparison.OrdinalIgnoreCase))
        {
            if (HasValidCardsInArray(unitTrustEquitiesCards))
            {
                randomCard = GetRandomCardFromArray(unitTrustEquitiesCards, "UnitTrustEquities");
                isKnownCategory = true;
                matchedCategory = "UnitTrustEquities";
            }
        }
        else if (categoryName.StartsWith("UnitTrustFixedIncome", System.StringComparison.OrdinalIgnoreCase) || 
                 categoryName.StartsWith("MutualFundFixedIncome", System.StringComparison.OrdinalIgnoreCase) ||
                 categoryName.StartsWith("FixedIncome", System.StringComparison.OrdinalIgnoreCase))
        {
            if (HasValidCardsInArray(unitTrustFixedIncomeCards))
            {
                randomCard = GetRandomCardFromArray(unitTrustFixedIncomeCards, "UnitTrustFixedIncome");
                isKnownCategory = true;
                matchedCategory = "UnitTrustFixedIncome";
            }
        }
        
        // Only return a card if:
        // 1. It's a known category
        // 2. We found a valid card in that category
        // 3. The category actually has cards assigned in the Inspector
        if (isKnownCategory && randomCard != null)
        {
            Debug.Log($"Matched category '{matchedCategory}' for path '{pathName}'. Spawning random card: {randomCard.name}");
            // If the category name doesn't have a number, it's a random selection
            // If it has a number but no exact match was found, it's also random (fallback)
            return (randomCard, !isSpecificCard);
        }
        
        // Unknown category or no card found - return null (don't spawn any card)
        Debug.Log($"No card match found for path '{pathName}' (category: '{categoryName}'). Skipping card spawn.");
        return (null, false);
    }
    
    /// <summary>
    /// Extracts the category name from path format "PathXX_Category" -> "Category"
    /// If path doesn't match the format, returns the original path name
    /// </summary>
    private string ExtractCategoryFromPath(string pathName)
    {
        // Check if path follows the pattern "PathXX_Category" where XX is digits
        int underscoreIndex = pathName.IndexOf('_');
        
        if (underscoreIndex > 0 && underscoreIndex < pathName.Length - 1)
        {
            string prefix = pathName.Substring(0, underscoreIndex);
            
            // Check if prefix starts with "Path" followed by digits
            if (prefix.StartsWith("Path", System.StringComparison.OrdinalIgnoreCase))
            {
                string numberPart = prefix.Substring(4); // Skip "Path"
                
                // Check if the remaining part is all digits
                if (numberPart.Length > 0 && System.Text.RegularExpressions.Regex.IsMatch(numberPart, @"^\d+$"))
                {
                    // Extract everything after the underscore
                    return pathName.Substring(underscoreIndex + 1);
                }
            }
        }
        
        // If path doesn't match the pattern, return the original name
        return pathName;
    }
    
    /// <summary>
    /// Tries to find an exact card match by name
    /// </summary>
    private GameObject FindExactCardMatch(string pathName)
    {
        // Search through all card arrays for exact name match
        GameObject[][] allCardArrays = new GameObject[][] 
        { 
            businessCards, 
            chanceCards, 
            marketWatchCards, 
            realEstateCards, 
            unitTrustEquitiesCards, 
            unitTrustFixedIncomeCards,
            fortuneRoadCards
        };
        
        foreach (GameObject[] cardArray in allCardArrays)
        {
            foreach (GameObject card in cardArray)
            {
                if (card != null && card.name.Equals(pathName, System.StringComparison.OrdinalIgnoreCase))
                {
                    return card;
                }
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// Finds an exact card match in a specific array
    /// </summary>
    private GameObject FindExactCardMatchInArray(GameObject[] cardArray, string pathName)
    {
        if (cardArray == null || cardArray.Length == 0)
        {
            return null;
        }
        
        foreach (GameObject card in cardArray)
        {
            if (card != null && card.name.Equals(pathName, System.StringComparison.OrdinalIgnoreCase))
            {
                return card;
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// Checks if an array has any valid (non-null) cards
    /// </summary>
    private bool HasValidCardsInArray(GameObject[] cardArray)
    {
        if (cardArray == null || cardArray.Length == 0)
        {
            return false;
        }
        
        foreach (GameObject card in cardArray)
        {
            if (card != null)
            {
                return true;
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// Gets a random card from the specified array
    /// </summary>
    private GameObject GetRandomCardFromArray(GameObject[] cardArray, string categoryName)
    {
        if (cardArray == null || cardArray.Length == 0)
        {
            Debug.LogWarning($"No {categoryName} cards assigned! Cannot spawn card.");
            return null;
        }
        
        // Filter out null prefabs
        List<GameObject> validCards = new List<GameObject>();
        foreach (GameObject card in cardArray)
        {
            if (card != null)
            {
                validCards.Add(card);
            }
        }
        
        if (validCards.Count == 0)
        {
            Debug.LogWarning($"No valid {categoryName} card prefabs found! Please assign card prefabs in the Inspector.");
            return null;
        }
        
        // Select random card
        return validCards[Random.Range(0, validCards.Count)];
    }
    
    private IEnumerator WaitForCardAnimation(CardController cardController)
    {
        // Wait until card animation is complete
        while (cardController != null && cardController.IsAnimating)
        {
            yield return null;
        }
        
        // Card has been destroyed, allow dice rolling again
        isCardAnimating = false;
        Debug.Log("Card animation complete. Dice rolling enabled again.");
    }
    
    /// <summary>
    /// Handles when a player visits an owned Real Estate or Business property
    /// Visitor pays the owner, and owner receives income
    /// </summary>
    private void HandlePropertyVisit(string pathName, bool isRealEstate, bool isBusiness, Player propertyOwner)
    {
        if (propertyOwner == null || propertyOwner.PlayerFinance == null)
        {
            Debug.LogWarning($"[CardsManager] Cannot handle property visit: Property owner or finance is null.");
            return;
        }
        
        // Get the current visiting player
        Player visitingPlayer = playerManager?.CurrentPlayer;
        if (visitingPlayer == null || visitingPlayer.PlayerFinance == null)
        {
            Debug.LogWarning($"[CardsManager] Cannot handle property visit: Visiting player or finance is null.");
            return;
        }
        
        // Extract property name from path (e.g., "Path11_RealEstate03" -> "RealEstate03")
        string categoryName = ExtractCategoryFromPath(pathName);
        if (string.IsNullOrEmpty(categoryName))
        {
            Debug.LogWarning($"[CardsManager] Cannot extract property name from path: {pathName}");
            return;
        }
        
        float paymentAmount = 0f;
        
        if (isRealEstate)
        {
            // For Real Estate: add incomePerVisit to owner's investment income
            if (realEstateData == null)
            {
                Debug.LogWarning($"[CardsManager] RealEstateData is null! Cannot get income per visit.");
                return;
            }
            
            RealEstateData.RealEstateProperty property = realEstateData.GetPropertyByName(categoryName);
            if (property == null)
            {
                Debug.LogWarning($"[CardsManager] Property data not found for: {categoryName}");
                return;
            }
            
            paymentAmount = property.incomePerVisit;
            
            // Visitor pays the owner
            if (paymentAmount > 0)
            {
                bool paymentSuccessful = visitingPlayer.PlayerFinance.SubtractCash(paymentAmount);
                if (paymentSuccessful)
                {
                    // Add income per visit to owner's investment income
                    propertyOwner.PlayerFinance.AddInvestmentIncomeItem(categoryName, paymentAmount);
                    Debug.Log($"[CardsManager] {visitingPlayer.PlayerName} paid {paymentAmount} to {propertyOwner.PlayerName} for visiting {categoryName}. Owner received investment income.");
                }
                else
                {
                    Debug.LogWarning($"[CardsManager] {visitingPlayer.PlayerName} doesn't have enough cash ({visitingPlayer.PlayerFinance.CurrentCash}) to pay {paymentAmount} for visiting {categoryName}.");
                    // Still add income to owner even if visitor can't pay (visitor goes into debt or payment is waived)
                    propertyOwner.PlayerFinance.AddInvestmentIncomeItem(categoryName, paymentAmount);
                    Debug.Log($"[CardsManager] Owner still received {paymentAmount} investment income for {categoryName} visit (visitor couldn't pay).");
                }
            }
        }
        else if (isBusiness)
        {
            // For Business: add incomePerVisit to owner's investment income
            if (businessData == null)
            {
                Debug.LogWarning($"[CardsManager] BusinessData is null! Cannot get income per visit.");
                return;
            }
            
            BusinessData.BusinessProperty business = businessData.GetBusinessByName(categoryName);
            if (business == null)
            {
                Debug.LogWarning($"[CardsManager] Business data not found for: {categoryName}");
                return;
            }
            
            paymentAmount = business.incomePerVisit;
            
            // Visitor pays the owner
            if (paymentAmount > 0)
            {
                bool paymentSuccessful = visitingPlayer.PlayerFinance.SubtractCash(paymentAmount);
                if (paymentSuccessful)
                {
                    // Add income per visit to owner's investment income
                    propertyOwner.PlayerFinance.AddInvestmentIncomeItem(categoryName, paymentAmount);
                    Debug.Log($"[CardsManager] {visitingPlayer.PlayerName} paid {paymentAmount} to {propertyOwner.PlayerName} for visiting {categoryName}. Owner received investment income.");
                }
                else
                {
                    Debug.LogWarning($"[CardsManager] {visitingPlayer.PlayerName} doesn't have enough cash ({visitingPlayer.PlayerFinance.CurrentCash}) to pay {paymentAmount} for visiting {categoryName}.");
                    // Still add income to owner even if visitor can't pay (visitor goes into debt or payment is waived)
                    propertyOwner.PlayerFinance.AddInvestmentIncomeItem(categoryName, paymentAmount);
                    Debug.Log($"[CardsManager] Owner still received {paymentAmount} investment income for {categoryName} visit (visitor couldn't pay).");
                }
            }
        }
        
        // Spawn dice after handling the visit
        GameManager gameManager = FindAnyObjectByType<GameManager>();
        if (gameManager != null)
        {
            gameManager.SpawnDice();
        }
    }
    
    /// <summary>
    /// Checks if the path name is a RealEstate path
    /// </summary>
    private bool IsRealEstatePath(string pathName)
    {
        if (string.IsNullOrEmpty(pathName))
        {
            return false;
        }
        
        string categoryName = ExtractCategoryFromPath(pathName);
        return categoryName.StartsWith("RealEstate", System.StringComparison.OrdinalIgnoreCase);
    }
    
    /// <summary>
    /// Checks if the path name is a Business path (e.g., "PathXX_BusinessXX")
    /// </summary>
    private bool IsBusinessPath(string pathName)
    {
        if (string.IsNullOrEmpty(pathName))
        {
            return false;
        }
        
        string categoryName = ExtractCategoryFromPath(pathName);
        // Check if it starts with "Business" and has digits at the end (e.g., "Business01", "Business02")
        return categoryName.StartsWith("Business", System.StringComparison.OrdinalIgnoreCase) &&
               System.Text.RegularExpressions.Regex.IsMatch(categoryName, @"Business\d+$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }
    
    /// <summary>
    /// Checks if the path name is a MarketWatch path (e.g., "PathXX_MarketWatch")
    /// </summary>
    private bool IsMarketWatchPath(string pathName)
    {
        if (string.IsNullOrEmpty(pathName))
        {
            return false;
        }
        
        string categoryName = ExtractCategoryFromPath(pathName);
        return categoryName.StartsWith("MarketWatch", System.StringComparison.OrdinalIgnoreCase);
    }
    
    /// <summary>
    /// Called when a RealEstate card reaches the end path
    /// </summary>
    private void OnRealEstateCardReachedEnd(CardController cardController)
    {
        if (string.IsNullOrEmpty(currentRealEstatePathName))
        {
            Debug.LogError("RealEstate path name is empty!");
            return;
        }
        
        // Check if cards can spawn (IsFirstRound check)
        GameManager gameManager = FindAnyObjectByType<GameManager>();
        if (gameManager != null && !gameManager.CanSpawnCards())
        {
            Debug.LogWarning($"IsFirstRound is enabled and player hasn't passed Path01_Start yet. Real estate event will not activate for: {currentRealEstatePathName}");
            // Destroy the card and spawn dice
            if (cardController != null)
            {
                cardController.DestroyCard();
            }
            isCardAnimating = false;
            currentRealEstateCard = null;
            currentRealEstatePathName = null;
            gameManager.SpawnDice();
            return;
        }
        
        // Extract property name from path (e.g., "Path11_RealEstate03" -> "RealEstate03")
        string categoryName = ExtractCategoryFromPath(currentRealEstatePathName);
        
        // Get property data
        if (realEstateData == null)
        {
            Debug.LogError("RealEstateData is not assigned! Cannot show ForSaleUI.");
            return;
        }
        
        RealEstateData.RealEstateProperty property = realEstateData.GetPropertyByName(categoryName);
        if (property == null)
        {
            Debug.LogWarning($"Property data not found for: {categoryName}");
            return;
        }
        
        // Find the path transform to spawn PlayerItem
        Transform pathTransform = FindPathTransform(currentRealEstatePathName);
        if (pathTransform == null)
        {
            Debug.LogWarning($"Path transform not found for: {currentRealEstatePathName}");
        }
        
        // Show RealEstateUI
        if (realEstateUIController != null)
        {
            realEstateUIController.ShowForSaleUI(property, cardController, pathTransform, categoryName);
        }
        else
        {
            Debug.LogError("RealEstateUIController is not assigned! Cannot show RealEstateUI.");
        }
    }
    
    /// <summary>
    /// Finds the transform of a path by name
    /// </summary>
    private Transform FindPathTransform(string pathName)
    {
        GameObject pathObj = GameObject.Find(pathName);
        if (pathObj != null)
        {
            return pathObj.transform;
        }
        
        // Try to find in GameMap
        GameObject gameMap = GameObject.Find("GameMap");
        if (gameMap != null)
        {
            Transform pathTransform = gameMap.transform.Find(pathName);
            if (pathTransform != null)
            {
                return pathTransform;
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// Called when player completes a RealEstate purchase
    /// </summary>
    private void OnRealEstatePurchaseComplete()
    {
        // Destroy the card
        if (currentRealEstateCard != null)
        {
            currentRealEstateCard.DestroyCard();
            currentRealEstateCard = null;
        }
        
        // Card has been destroyed, allow dice rolling again
        isCardAnimating = false;
        
        // Spawn dice
        GameManager gameManager = FindAnyObjectByType<GameManager>();
        if (gameManager != null)
        {
            gameManager.SpawnDice();
        }
        
        Debug.Log("RealEstate purchase complete. Card destroyed and dice spawned.");
    }
    
    /// <summary>
    /// Called when player cancels a RealEstate purchase
    /// </summary>
    private void OnRealEstatePurchaseCancelled()
    {
        // Destroy the card
        if (currentRealEstateCard != null)
        {
            currentRealEstateCard.DestroyCard();
            currentRealEstateCard = null;
        }
        
        // Card has been destroyed, allow dice rolling again
        isCardAnimating = false;
        
        // Spawn dice
        GameManager gameManager = FindAnyObjectByType<GameManager>();
        if (gameManager != null)
        {
            gameManager.SpawnDice();
        }
        
        Debug.Log("RealEstate purchase cancelled. Card destroyed and dice spawned.");
    }
    
    /// <summary>
    /// Called when a Business card reaches the end path
    /// </summary>
    private void OnBusinessCardReachedEnd(CardController cardController)
    {
        if (string.IsNullOrEmpty(currentBusinessPathName))
        {
            Debug.LogError("Business path name is empty!");
            return;
        }
        
        // Check if cards can spawn (IsFirstRound check)
        GameManager gameManager = FindAnyObjectByType<GameManager>();
        if (gameManager != null && !gameManager.CanSpawnCards())
        {
            Debug.LogWarning($"IsFirstRound is enabled and player hasn't passed Path01_Start yet. Business event will not activate for: {currentBusinessPathName}");
            // Destroy the card and spawn dice
            if (cardController != null)
            {
                cardController.DestroyCard();
            }
            isCardAnimating = false;
            currentBusinessCard = null;
            currentBusinessPathName = null;
            gameManager.SpawnDice();
            return;
        }
        
        // Extract business name from path (e.g., "Path02_Business01" -> "Business01")
        string categoryName = ExtractCategoryFromPath(currentBusinessPathName);
        
        // Get business data
        if (businessData == null)
        {
            Debug.LogError("BusinessData is not assigned! Cannot show BusinessUI.");
            return;
        }
        
        BusinessData.BusinessProperty business = businessData.GetBusinessByName(categoryName);
        if (business == null)
        {
            Debug.LogWarning($"Business data not found for: {categoryName}");
            return;
        }
        
        // Find the path transform to spawn PlayerItem
        Transform pathTransform = FindPathTransform(currentBusinessPathName);
        if (pathTransform == null)
        {
            Debug.LogWarning($"Path transform not found for: {currentBusinessPathName}");
        }
        
        // Show BusinessUI
        if (businessUIController != null)
        {
            businessUIController.ShowBusinessUI(business, cardController, pathTransform, categoryName);
        }
        else
        {
            Debug.LogError("BusinessUIController is not assigned! Cannot show BusinessUI.");
        }
    }
    
    /// <summary>
    /// Called when player completes a Business purchase
    /// </summary>
    private void OnBusinessPurchaseComplete()
    {
        // Destroy the card
        if (currentBusinessCard != null)
        {
            currentBusinessCard.DestroyCard();
            currentBusinessCard = null;
        }
        
        // Card has been destroyed, allow dice rolling again
        isCardAnimating = false;
        
        // Spawn dice
        GameManager gameManager = FindAnyObjectByType<GameManager>();
        if (gameManager != null)
        {
            gameManager.SpawnDice();
        }
        
        Debug.Log("Business purchase complete. Card destroyed and dice spawned.");
    }
    
    /// <summary>
    /// Called when player cancels a Business purchase
    /// </summary>
    private void OnBusinessPurchaseCancelled()
    {
        // Destroy the card
        if (currentBusinessCard != null)
        {
            currentBusinessCard.DestroyCard();
            currentBusinessCard = null;
        }
        
        // Card has been destroyed, allow dice rolling again
        isCardAnimating = false;
        
        // Spawn dice
        GameManager gameManager = FindAnyObjectByType<GameManager>();
        if (gameManager != null)
        {
            gameManager.SpawnDice();
        }
        
        Debug.Log("Business purchase cancelled. Card destroyed and dice spawned.");
    }
    
    /// <summary>
    /// Called when a MarketWatch card reaches the end path
    /// </summary>
    private void OnMarketWatchCardReachedEnd(CardController cardController)
    {
        if (string.IsNullOrEmpty(currentMarketWatchPathName))
        {
            Debug.LogError("MarketWatch path name is empty!");
            return;
        }
        
        // Extract card name from path (e.g., "Path12_MarketWatch" -> "MarketWatch")
        // For MarketWatch, we'll use a random card or match by card name if available
        string categoryName = ExtractCategoryFromPath(currentMarketWatchPathName);
        
        // Get MarketWatch card data
        if (marketWatchData == null)
        {
            Debug.LogError("MarketWatchData is not assigned! Cannot show MarketWatchUI.");
            return;
        }
        
        // Try to get card by name first (if card name matches path), otherwise get random
        MarketWatchData.MarketWatchCard card = null;
        
        // Check if we can match by card name (e.g., if path is "Path12_MarketWatch01")
        if (categoryName.Contains("MarketWatch"))
        {
            // Try to extract card number if available
            string cardName = categoryName; // Use full category name as card name
            card = marketWatchData.GetCardByName(cardName);
        }
        
        // If no specific card found, get a random one
        if (card == null)
        {
            card = marketWatchData.GetRandomCard();
        }
        
        if (card == null)
        {
            Debug.LogWarning($"MarketWatch card data not found for: {categoryName}. Skipping MarketWatch processing.");
            
            // Wait for Card Wait Duration before continuing (same as normal card processing)
            StartCoroutine(WaitAndSkipMarketWatch(cardController));
            
            return;
        }
        
        // Show MarketWatchUI
        if (marketWatchUIController != null)
        {
            marketWatchUIController.ShowMarketWatchUI(card, cardController);
        }
        else
        {
            Debug.LogError("MarketWatchUIController is not assigned! Cannot show MarketWatchUI.");
        }
    }
    
    /// <summary>
    /// Called when MarketWatch effect is complete
    /// </summary>
    private void OnMarketWatchEffectComplete()
    {
        // Card has been destroyed by MarketWatchUI, allow dice rolling again
        isCardAnimating = false;
        currentMarketWatchCard = null;
        currentMarketWatchPathName = null;
        
        // Spawn dice
        GameManager gameManager = FindAnyObjectByType<GameManager>();
        if (gameManager != null)
        {
            gameManager.SpawnDice();
        }
        
        Debug.Log("MarketWatch effect complete. Dice spawned.");
    }
    
    /// <summary>
    /// Waits for Card Wait Duration before skipping MarketWatch processing
    /// </summary>
    private IEnumerator WaitAndSkipMarketWatch(CardController cardController)
    {
        // Wait for the Card Wait Duration (same as normal card processing)
        yield return new WaitForSeconds(cardWaitDuration);
        
        // Destroy the card since we can't process it
        if (cardController != null)
        {
            cardController.DestroyCard();
        }
        
        // Reset state and allow dice rolling again
        isCardAnimating = false;
        currentMarketWatchCard = null;
        currentMarketWatchPathName = null;
        
        // Spawn dice to continue the game
        GameManager gameManager = FindAnyObjectByType<GameManager>();
        if (gameManager != null)
        {
            gameManager.SpawnDice();
        }
        
        Debug.Log("MarketWatch skipped after waiting Card Wait Duration. Dice spawned.");
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
        
        // Unsubscribe from RealEstateUI events
        if (realEstateUIController != null)
        {
            realEstateUIController.OnPurchaseComplete -= OnRealEstatePurchaseComplete;
            realEstateUIController.OnPurchaseCancelled -= OnRealEstatePurchaseCancelled;
        }
        
        // Unsubscribe from BusinessUI events
        if (businessUIController != null)
        {
            businessUIController.OnPurchaseComplete -= OnBusinessPurchaseComplete;
            businessUIController.OnPurchaseCancelled -= OnBusinessPurchaseCancelled;
        }
        
        // Unsubscribe from MarketWatchUI events
        if (marketWatchUIController != null)
        {
            marketWatchUIController.OnEffectComplete -= OnMarketWatchEffectComplete;
        }
        
        // Unsubscribe from card events
        if (currentRealEstateCard != null)
        {
            currentRealEstateCard.OnCardReachedEnd -= OnRealEstateCardReachedEnd;
        }
        
        if (currentBusinessCard != null)
        {
            currentBusinessCard.OnCardReachedEnd -= OnBusinessCardReachedEnd;
        }
        
        if (currentMarketWatchCard != null)
        {
            currentMarketWatchCard.OnCardReachedEnd -= OnMarketWatchCardReachedEnd;
        }
    }
}
