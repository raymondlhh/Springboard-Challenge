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
    
    [Header("Card Manager Reference")]
    [SerializeField] private CardsManager cardsManager;
    
    [Header("UI References")]
    [SerializeField] private GameObject miniGamesUI;
    
    private int diceSum = 0;
    private bool isRolling = false;
    private float lastCheckTime = 0f;
    private KeyboardManager keyboardManager;
    
    public int DiceSum => diceSum;
    public bool IsRolling => isRolling;
    public bool CanRollDice => !isRolling && (cardsManager == null || !cardsManager.IsCardAnimating) && (player == null || !player.IsMoving) && !IsMiniGameActive();
    
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
        
        // Find CardsManager if not assigned
        if (cardsManager == null)
        {
            cardsManager = FindAnyObjectByType<CardsManager>();
        }
        
        // Find KeyboardManager to check MiniGameStockMarket status
        keyboardManager = FindAnyObjectByType<KeyboardManager>();
        
        // Find and hide MiniGamesUI at start
        if (miniGamesUI == null)
        {
            GameObject miniGamesUIObj = GameObject.Find("MiniGamesUI");
            if (miniGamesUIObj != null)
            {
                miniGamesUI = miniGamesUIObj;
            }
        }
        
        // Hide MiniGamesUI at start
        if (miniGamesUI != null)
        {
            miniGamesUI.SetActive(false);
        }
    }
    
    private bool IsMiniGameActive()
    {
        // Check if MiniGamesUI is active (use activeInHierarchy to account for parent inactive state)
        if (miniGamesUI != null && miniGamesUI.activeInHierarchy)
        {
            Debug.Log("Dice blocked: MiniGamesUI is active");
            return true;
        }
        
        // Check if MiniGameStockMarket is active via KeyboardManager
        if (keyboardManager != null && keyboardManager.IsMiniGameActive)
        {
            Debug.Log("Dice blocked: MiniGameStockMarket is active");
            return true;
        }
        
        // Fallback: Try to find MiniGameStockMarket GameObject directly
        GameObject miniGame = GameObject.Find("MiniGameStockMarket");
        if (miniGame != null && miniGame.activeInHierarchy)
        {
            Debug.Log("Dice blocked: MiniGameStockMarket found and active");
            return true;
        }
        
        return false;
    }

    // Update is called once per frame
    void Update()
    {
        // Handle input for web (mouse click) and mobile (touch)
        // Only allow rolling if dice are not rolling, player is not moving, and card is not animating
        if (Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
        {
            if (!CanRollDice)
            {
                // Debug why dice cannot be rolled
                if (isRolling)
                    Debug.Log("Cannot roll dice: Dice are currently rolling");
                else if (cardsManager != null && cardsManager.IsCardAnimating)
                    Debug.Log("Cannot roll dice: Card is animating");
                else if (player != null && player.IsMoving)
                    Debug.Log("Cannot roll dice: Player is moving");
                else if (IsMiniGameActive())
                    Debug.Log("Cannot roll dice: Mini game is active");
                else
                    Debug.Log("Cannot roll dice: Unknown reason");
            }
            else if (CanRollDice)
            {
                RollDice();
            }
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
        
        // Play rolling dice sound effect
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX("RollingDice");
        }
        
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
        }
        else
        {
            Debug.LogWarning("Player GameObject not found in scene!");
        }
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
