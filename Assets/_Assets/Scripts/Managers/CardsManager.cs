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
    
    [Header("Card Spawn Points")]
    [SerializeField] private Transform cardsStartPath;
    [SerializeField] private Transform cardsEndPath;
    
    [Header("Card Animation Settings")]
    [Tooltip("Card movement speed. Higher values = faster movement. (Speed of 1 = 2 seconds duration, Speed of 2 = 1 second duration)")]
    [SerializeField] private float cardMoveSpeed = 1f;
    [Tooltip("Time in seconds the card waits before being destroyed after reaching the end position")]
    [SerializeField] private float cardWaitDuration = 3f;
    
    [Header("Player Reference")]
    [SerializeField] private PlayerController player;
    
    [Header("Debug Manager Reference")]
    [SerializeField] private StockManager debugManager;
    
    [Header("Real Estate Settings")]
    [SerializeField] private RealEstateData realEstateData;
    [SerializeField] private ForSaleUIController forSaleUIController;
    
    private bool isCardAnimating = false;
    private CardController currentRealEstateCard;
    private string currentRealEstatePathName;
    
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
        
        var (cardPrefab, _) = GetCardPrefabForPath(pathName);
        return cardPrefab != null;
    }
    
    void Start()
    {
        // Find player if not assigned
        if (player == null)
        {
            FindPlayer();
        }
        
        // Find card spawn points if not assigned
        if (cardsStartPath == null || cardsEndPath == null)
        {
            FindCardSpawnPoints();
        }
        
        // Find DebugManager if not assigned
        if (debugManager == null)
        {
            debugManager = FindAnyObjectByType<StockManager>();
        }
        
        // Find ForSaleUIController if not assigned
        if (forSaleUIController == null)
        {
            GameObject forSaleUIObj = GameObject.Find("ForSaleUI");
            if (forSaleUIObj != null)
            {
                forSaleUIController = forSaleUIObj.GetComponent<ForSaleUIController>();
            }
            
            if (forSaleUIController == null)
            {
                forSaleUIController = FindAnyObjectByType<ForSaleUIController>();
            }
        }
        
        // Subscribe to ForSaleUI events
        if (forSaleUIController != null)
        {
            forSaleUIController.OnPurchaseComplete += OnRealEstatePurchaseComplete;
            forSaleUIController.OnPurchaseCancelled += OnRealEstatePurchaseCancelled;
        }
        
        // Subscribe to player movement complete event
        if (player != null)
        {
            player.OnMovementComplete += OnPlayerMovementComplete;
        }
    }
    
    private void FindPlayer()
    {
        GameObject playerObj = GameObject.Find("Player");
        if (playerObj != null)
        {
            player = playerObj.GetComponent<PlayerController>();
            if (player == null)
            {
                Debug.LogWarning("Player GameObject found but PlayerController component is missing!");
            }
        }
        else
        {
            Debug.LogWarning("Player GameObject not found in scene!");
        }
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
        // Get the current waypoint name from player
        if (player != null)
        {
            string waypointName = player.GetCurrentWaypointName();
            
            // Check if path contains "Stocks" keyword and activate minigame
            if (!string.IsNullOrEmpty(waypointName) && waypointName.Contains("Stocks", System.StringComparison.OrdinalIgnoreCase))
            {
                if (debugManager != null)
                {
                    debugManager.ActivateMiniGame();
                }
                else
                {
                    Debug.LogWarning("DebugManager not found! Cannot activate minigame for Stocks path.");
                }
            }
            
            SpawnCardForPath(waypointName);
        }
        else
        {
            Debug.LogWarning("Player reference is null! Cannot spawn card.");
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
        
        // Check if this is a RealEstate path
        bool isRealEstatePath = IsRealEstatePath(pathName);
        bool waitForInput = isRealEstatePath;
        
        // If it's a RealEstate path, subscribe to card reached end event
        if (isRealEstatePath)
        {
            currentRealEstateCard = cardController;
            currentRealEstatePathName = pathName;
            cardController.OnCardReachedEnd += OnRealEstateCardReachedEnd;
        }
        
        // Start card animation with the configured speed and wait duration
        // Convert speed to duration: higher speed = lower duration (faster movement)
        // Base duration of 2 seconds divided by speed
        float moveDuration = 2f / Mathf.Max(0.1f, cardMoveSpeed); // Prevent division by zero
        isCardAnimating = true;
        cardController.AnimateCard(cardsStartPath, cardsEndPath, moveDuration, cardWaitDuration, waitForInput);
        
        // Monitor when card animation completes (only for non-RealEstate cards)
        if (!isRealEstatePath)
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
    /// </summary>
    private (GameObject cardPrefab, bool isRandomSelection) GetCardPrefabForPath(string pathName)
    {
        // Normalize path name (remove spaces, handle case)
        string normalizedPath = pathName.Trim();
        
        // Extract category name from path format: "PathXX_Category" -> "Category"
        string categoryName = ExtractCategoryFromPath(normalizedPath);
        
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
        }
        
        // Check for category match (e.g., "Business", "Chance", "FixedIncome", etc.)
        // Handle different naming conventions
        GameObject randomCard = null;
        
        if (categoryName.StartsWith("Business", System.StringComparison.OrdinalIgnoreCase))
        {
            randomCard = GetRandomCardFromArray(businessCards, "Business");
        }
        else if (categoryName.StartsWith("Chance", System.StringComparison.OrdinalIgnoreCase))
        {
            randomCard = GetRandomCardFromArray(chanceCards, "Chance");
        }
        else if (categoryName.StartsWith("MarketWatch", System.StringComparison.OrdinalIgnoreCase))
        {
            randomCard = GetRandomCardFromArray(marketWatchCards, "MarketWatch");
        }
        else if (categoryName.StartsWith("RealEstate", System.StringComparison.OrdinalIgnoreCase))
        {
            randomCard = GetRandomCardFromArray(realEstateCards, "RealEstate");
        }
        else if (categoryName.StartsWith("UnitTrustEquities", System.StringComparison.OrdinalIgnoreCase) || 
                 categoryName.StartsWith("MutualFundEquities", System.StringComparison.OrdinalIgnoreCase) ||
                 categoryName.StartsWith("Equities", System.StringComparison.OrdinalIgnoreCase))
        {
            randomCard = GetRandomCardFromArray(unitTrustEquitiesCards, "UnitTrustEquities");
        }
        else if (categoryName.StartsWith("UnitTrustFixedIncome", System.StringComparison.OrdinalIgnoreCase) || 
                 categoryName.StartsWith("MutualFundFixedIncome", System.StringComparison.OrdinalIgnoreCase) ||
                 categoryName.StartsWith("FixedIncome", System.StringComparison.OrdinalIgnoreCase))
        {
            randomCard = GetRandomCardFromArray(unitTrustFixedIncomeCards, "UnitTrustFixedIncome");
        }
        
        if (randomCard != null)
        {
            // If the category name doesn't have a number, it's a random selection
            // If it has a number but no exact match was found, it's also random (fallback)
            return (randomCard, !isSpecificCard);
        }
        
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
    /// Called when a RealEstate card reaches the end path
    /// </summary>
    private void OnRealEstateCardReachedEnd(CardController cardController)
    {
        if (string.IsNullOrEmpty(currentRealEstatePathName))
        {
            Debug.LogError("RealEstate path name is empty!");
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
        
        // Show ForSaleUI
        if (forSaleUIController != null)
        {
            forSaleUIController.ShowForSaleUI(property, cardController, pathTransform, categoryName);
        }
        else
        {
            Debug.LogError("ForSaleUIController is not assigned! Cannot show ForSaleUI.");
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
    
    private void OnDestroy()
    {
        // Unsubscribe from player movement complete event
        if (player != null)
        {
            player.OnMovementComplete -= OnPlayerMovementComplete;
        }
        
        // Unsubscribe from ForSaleUI events
        if (forSaleUIController != null)
        {
            forSaleUIController.OnPurchaseComplete -= OnRealEstatePurchaseComplete;
            forSaleUIController.OnPurchaseCancelled -= OnRealEstatePurchaseCancelled;
        }
        
        // Unsubscribe from card events
        if (currentRealEstateCard != null)
        {
            currentRealEstateCard.OnCardReachedEnd -= OnRealEstateCardReachedEnd;
        }
    }
}
