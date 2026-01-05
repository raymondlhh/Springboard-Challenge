using UnityEngine;
using System.Collections;

public class GameManager : MonoBehaviour
{
    [Header("Spawner References")]
    [SerializeField] private Transform firstSpawner;
    [SerializeField] private Transform secondSpawner;
    
    [Header("Dice References")]
    [SerializeField] private DiceController firstDice;
    [SerializeField] private DiceController secondDice;
    [SerializeField] private GameObject dicePrefab;
    
    [Header("Dice Settings")]
    [SerializeField] private float diceCheckInterval = 0.1f;
    
    [Header("Player Reference")]
    [SerializeField] private PlayerController player;
    
    [Header("Card System")]
    [SerializeField] private GameObject[] cardPrefabs = new GameObject[6];
    [SerializeField] private Transform cardsStartPath;
    [SerializeField] private Transform cardsEndPath;
    
    private int diceSum = 0;
    private bool isRolling = false;
    private float lastCheckTime = 0f;
    private bool isCardAnimating = false;
    
    public int DiceSum => diceSum;
    public bool IsRolling => isRolling;
    public bool CanRollDice => !isRolling && !isCardAnimating && (player == null || !player.IsMoving);
    
    // Display current dice values
    public int FirstDiceValue => firstDice != null ? firstDice.CurrentValue : 0;
    public int SecondDiceValue => secondDice != null ? secondDice.CurrentValue : 0;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Find spawners if not assigned
        if (firstSpawner == null || secondSpawner == null)
        {
            FindSpawners();
        }
        
        // Load dice prefab if not assigned
        if (dicePrefab == null)
        {
            LoadDicePrefab();
        }
        
        // Always spawn dice at start (remove any existing dice first)
        SpawnDice();
        
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

    // Update is called once per frame
    void Update()
    {
        // Handle input for web (mouse click) and mobile (touch)
        // Only allow rolling if dice are not rolling, player is not moving, and card is not animating
        if (CanRollDice && (Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)))
        {
            RollDice();
        }
        
        // Check if dice have finished rolling
        if (isRolling && Time.time - lastCheckTime >= diceCheckInterval)
        {
            lastCheckTime = Time.time;
            
            if (!firstDice.IsRolling && !secondDice.IsRolling)
            {
                CalculateDiceSum();
                isRolling = false;
            }
        }
    }
    
    private void FindSpawners()
    {
        // Try to find spawners by name
        GameObject firstSpawnerObj = GameObject.Find("FirstSpawner");
        GameObject secondSpawnerObj = GameObject.Find("SecondSpawner");
        
        if (firstSpawnerObj != null)
        {
            firstSpawner = firstSpawnerObj.transform;
        }
        
        if (secondSpawnerObj != null)
        {
            secondSpawner = secondSpawnerObj.transform;
        }
    }
    
    private void LoadDicePrefab()
    {
        // Try to load dice prefab from Resources folder
        // Note: The prefab must be in a "Resources" folder for this to work at runtime
        dicePrefab = Resources.Load<GameObject>("Dice");
        
        // If still null, the prefab should be assigned in the Inspector
        // or placed in a Resources folder
        if (dicePrefab == null)
        {
            Debug.LogWarning("Dice Prefab not found! Please assign it in the Inspector or place it in a Resources folder.");
        }
    }
    
    private void SpawnDice()
    {
        // Remove any existing dice from first spawner
        if (firstSpawner != null)
        {
            // Destroy all children (existing dice)
            for (int i = firstSpawner.childCount - 1; i >= 0; i--)
            {
                Destroy(firstSpawner.GetChild(i).gameObject);
            }
            
            // Spawn new dice at first spawner
            if (dicePrefab != null)
            {
                GameObject spawnedDice = Instantiate(dicePrefab, firstSpawner.position, firstSpawner.rotation, firstSpawner);
                spawnedDice.name = "FirstDice";
                firstDice = spawnedDice.GetComponent<DiceController>();
                
                if (firstDice == null)
                {
                    firstDice = spawnedDice.AddComponent<DiceController>();
                }
            }
            else
            {
                Debug.LogError("Dice Prefab is not assigned! Cannot spawn dice.");
            }
        }
        
        // Remove any existing dice from second spawner
        if (secondSpawner != null)
        {
            // Destroy all children (existing dice)
            for (int i = secondSpawner.childCount - 1; i >= 0; i--)
            {
                Destroy(secondSpawner.GetChild(i).gameObject);
            }
            
            // Spawn new dice at second spawner
            if (dicePrefab != null)
            {
                GameObject spawnedDice = Instantiate(dicePrefab, secondSpawner.position, secondSpawner.rotation, secondSpawner);
                spawnedDice.name = "SecondDice";
                secondDice = spawnedDice.GetComponent<DiceController>();
                
                if (secondDice == null)
                {
                    secondDice = spawnedDice.AddComponent<DiceController>();
                }
            }
            else
            {
                Debug.LogError("Dice Prefab is not assigned! Cannot spawn dice.");
            }
        }
    }
    
    private void FindDice()
    {
        // Try to find dice in spawners first
        if (firstSpawner != null)
        {
            firstDice = firstSpawner.GetComponentInChildren<DiceController>();
        }
        
        if (secondSpawner != null)
        {
            secondDice = secondSpawner.GetComponentInChildren<DiceController>();
        }
        
        // Fallback: Try to find dice by name
        if (firstDice == null)
        {
            GameObject firstDiceObj = GameObject.Find("FirstDice");
            if (firstDiceObj != null)
            {
                firstDice = firstDiceObj.GetComponent<DiceController>();
                if (firstDice == null)
                {
                    firstDice = firstDiceObj.AddComponent<DiceController>();
                }
            }
        }
        
        if (secondDice == null)
        {
            GameObject secondDiceObj = GameObject.Find("SecondDice");
            if (secondDiceObj != null)
            {
                secondDice = secondDiceObj.GetComponent<DiceController>();
                if (secondDice == null)
                {
                    secondDice = secondDiceObj.AddComponent<DiceController>();
                }
            }
        }
    }
    
    public void RollDice()
    {
        if (isRolling || firstDice == null || secondDice == null)
        {
            return;
        }
        
        isRolling = true;
        diceSum = 0;
        
        // Roll both dice
        firstDice.RollDice();
        secondDice.RollDice();
        
        lastCheckTime = Time.time;
    }
    
    private void CalculateDiceSum()
    {
        if (firstDice != null && secondDice != null)
        {
            diceSum = firstDice.CurrentValue + secondDice.CurrentValue;
            
            // Display the dice sum prominently
            string message = $"=== DICE ROLL RESULT ===\n" +
                           $"First Dice: {firstDice.CurrentValue}\n" +
                           $"Second Dice: {secondDice.CurrentValue}\n" +
                           $"TOTAL SUM: {diceSum}\n" +
                           $"========================";
            
            Debug.Log(message);
            DisplayDiceSum();
            
            // Move player based on dice sum
            if (player != null && diceSum > 0)
            {
                player.OnDiceRollComplete(diceSum);
            }
            else if (player == null)
            {
                Debug.LogWarning("Player not found! Cannot move player.");
            }
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
                player = playerObj.AddComponent<PlayerController>();
            }
            
            // Subscribe to movement complete event
            player.OnMovementComplete += OnPlayerMovementComplete;
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
        // Spawn random card after player movement completes
        SpawnRandomCard();
    }
    
    private void SpawnRandomCard()
    {
        // Check if we have card prefabs
        if (cardPrefabs == null || cardPrefabs.Length == 0)
        {
            Debug.LogWarning("No card prefabs assigned! Cannot spawn card.");
            return;
        }
        
        // Filter out null prefabs
        System.Collections.Generic.List<GameObject> validCards = new System.Collections.Generic.List<GameObject>();
        foreach (GameObject card in cardPrefabs)
        {
            if (card != null)
            {
                validCards.Add(card);
            }
        }
        
        if (validCards.Count == 0)
        {
            Debug.LogWarning("No valid card prefabs found! Please assign card prefabs in the Inspector.");
            return;
        }
        
        // Check if spawn points are available
        if (cardsStartPath == null || cardsEndPath == null)
        {
            Debug.LogWarning("Card spawn points not found! Cannot spawn card.");
            return;
        }
        
        // Select random card
        GameObject randomCardPrefab = validCards[Random.Range(0, validCards.Count)];
        
        // Spawn the card
        GameObject spawnedCard = Instantiate(randomCardPrefab);
        spawnedCard.name = randomCardPrefab.name;
        
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
        
        Debug.Log($"Spawned card: {randomCardPrefab.name}");
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
    /// Displays the current dice sum. Can be called anytime to check the current values.
    /// </summary>
    public void DisplayDiceSum()
    {
        if (firstDice != null && secondDice != null)
        {
            string status = $"Current Dice Status:\n" +
                          $"First Dice Value: {firstDice.CurrentValue}\n" +
                          $"Second Dice Value: {secondDice.CurrentValue}\n" +
                          $"Total Sum: {diceSum}";
            Debug.Log(status);
        }
        else
        {
            Debug.LogWarning("Dice not found! Cannot display dice sum.");
        }
    }
    
    public void ResetDice()
    {
        if (firstDice != null)
        {
            firstDice.ResetDice();
        }
        
        if (secondDice != null)
        {
            secondDice.ResetDice();
        }
        
        diceSum = 0;
        isRolling = false;
    }
}
