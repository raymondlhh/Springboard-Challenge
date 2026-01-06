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
    
    [Header("Card Spawn Points")]
    [SerializeField] private Transform cardsStartPath;
    [SerializeField] private Transform cardsEndPath;
    
    [Header("Player Reference")]
    [SerializeField] private PlayerController player;
    
    private bool isCardAnimating = false;
    
    public bool IsCardAnimating => isCardAnimating;
    
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
        
        GameObject cardPrefab = GetCardPrefabForPath(pathName);
        
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
        
        // Start card animation
        isCardAnimating = true;
        cardController.AnimateCard(cardsStartPath, cardsEndPath);
        
        // Monitor when card animation completes
        StartCoroutine(WaitForCardAnimation(cardController));
        
        Debug.Log($"Spawned card: {cardPrefab.name} for path: {pathName}");
    }
    
    /// <summary>
    /// Gets the appropriate card prefab based on the path name
    /// </summary>
    private GameObject GetCardPrefabForPath(string pathName)
    {
        // Normalize path name (remove spaces, handle case)
        string normalizedPath = pathName.Trim();
        
        // Check for exact match first (e.g., "Business01", "Chance02", etc.)
        GameObject exactMatch = FindExactCardMatch(normalizedPath);
        if (exactMatch != null)
        {
            return exactMatch;
        }
        
        // Check for category match (e.g., "Business", "Chance", "FixedIncome", etc.)
        // Handle different naming conventions
        if (normalizedPath.StartsWith("Business", System.StringComparison.OrdinalIgnoreCase))
        {
            return GetRandomCardFromArray(businessCards, "Business");
        }
        else if (normalizedPath.StartsWith("Chance", System.StringComparison.OrdinalIgnoreCase))
        {
            return GetRandomCardFromArray(chanceCards, "Chance");
        }
        else if (normalizedPath.StartsWith("MarketWatch", System.StringComparison.OrdinalIgnoreCase))
        {
            return GetRandomCardFromArray(marketWatchCards, "MarketWatch");
        }
        else if (normalizedPath.StartsWith("RealEstate", System.StringComparison.OrdinalIgnoreCase))
        {
            return GetRandomCardFromArray(realEstateCards, "RealEstate");
        }
        else if (normalizedPath.StartsWith("UnitTrustEquities", System.StringComparison.OrdinalIgnoreCase) || 
                 normalizedPath.StartsWith("Equities", System.StringComparison.OrdinalIgnoreCase))
        {
            return GetRandomCardFromArray(unitTrustEquitiesCards, "UnitTrustEquities");
        }
        else if (normalizedPath.StartsWith("UnitTrustFixedIncome", System.StringComparison.OrdinalIgnoreCase) || 
                 normalizedPath.StartsWith("FixedIncome", System.StringComparison.OrdinalIgnoreCase))
        {
            return GetRandomCardFromArray(unitTrustFixedIncomeCards, "UnitTrustFixedIncome");
        }
        
        return null;
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
            unitTrustFixedIncomeCards 
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
    
    private void OnDestroy()
    {
        // Unsubscribe from player movement complete event
        if (player != null)
        {
            player.OnMovementComplete -= OnPlayerMovementComplete;
        }
    }
}
