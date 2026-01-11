using UnityEngine;
using System.Collections;

public class GameManager : MonoBehaviour
{
    [Header("Dice Manager Reference")]
    [SerializeField] private DiceManager diceManager;
    
    [Header("Dice References")]
    private DiceController firstDice; // Managed internally, not shown in Inspector
    private DiceController secondDice; // Managed internally, not shown in Inspector
    
    [Header("Player Manager Reference")]
    [SerializeField] private PlayerManager playerManager;
    
    [Header("Card Manager Reference")]
    [SerializeField] private CardsManager cardsManager;
    
    [Header("Stock Path Manager Reference")]
    [Tooltip("Reference to StockPathManager. Will auto-find if not assigned.")]
    [SerializeField] private StockPathManager stockPathManager;
    
    [Header("UI References")]
    [SerializeField] private GameObject stockUI;
    
    private int diceSum = 0;
    private bool isRolling = false;
    private float lastCheckTime = 0f;
    private StockManager keyboardManager;
    private bool isProcessingDiceResult = false;
    private bool shouldGrantExtraTurnForMatchingDice = false; // Flag to grant extra turn when both dice match
    private bool isDiceMeterActive = false; // Track if DiceMeter is currently active (for second method)
    private int firstDiceValue = 0; // Store dice values for second method
    private int secondDiceValue = 0; // Store dice values for second method
    
    public int DiceSum => diceSum;
    public bool IsRolling => isRolling;
    public bool CanRollDice => !isRolling && !isProcessingDiceResult && (cardsManager == null || !cardsManager.IsCardAnimating) && (GetCurrentPlayerController() == null || !GetCurrentPlayerController().IsMoving) && !IsMiniGameActive();
    
    // Display current dice values
    public int FirstDiceValue => firstDice != null ? firstDice.CurrentValue : 0;
    public int SecondDiceValue => secondDice != null ? secondDice.CurrentValue : 0;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Find DiceManager if not assigned
        if (diceManager == null)
        {
            diceManager = FindAnyObjectByType<DiceManager>();
        }
        
        // Find PlayerManager if not assigned
        if (playerManager == null)
        {
            playerManager = FindAnyObjectByType<PlayerManager>();
        }
        
        // Subscribe to PlayerManager events
        if (playerManager != null)
        {
            playerManager.OnCurrentPlayerChanged += OnCurrentPlayerChanged;
        }
        
        // Always spawn dice at start (remove any existing dice first)
        SpawnDice();
        
        // Find CardsManager if not assigned
        if (cardsManager == null)
        {
            cardsManager = FindAnyObjectByType<CardsManager>();
        }
        
        // Find StockPathManager if not assigned
        if (stockPathManager == null)
        {
            stockPathManager = FindAnyObjectByType<StockPathManager>();
        }
        
        // Find KeyboardManager to check MiniGameStockMarket status
        keyboardManager = FindAnyObjectByType<StockManager>();
        
        // Subscribe to current player's movement complete event
        SubscribeToCurrentPlayerEvents();
        
        // Find and hide MiniGamesUI at start
        if (stockUI == null)
        {
            GameObject miniGamesUIObj = GameObject.Find("MiniGamesUI");
            if (miniGamesUIObj != null)
            {
                stockUI = miniGamesUIObj;
            }
        }
        
        // Hide MiniGamesUI at start
        if (stockUI != null)
        {
            stockUI.SetActive(false);
        }
    }
    
    private bool IsMiniGameActive()
    {
        // Check if MiniGamesUI is active (use activeInHierarchy to account for parent inactive state)
        if (stockUI != null && stockUI.activeInHierarchy)
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
        // Check which dice method to use
        bool useSecondMethod = diceManager != null && diceManager.UseSecondMethod;
        
        if (useSecondMethod)
        {
            // Second Method: Video-based dice
            HandleSecondMethodInput();
        }
        else
        {
            // First Method: Physical dice spawning
            HandleFirstMethodInput();
        }
    }
    
    /// <summary>
    /// Handles input for the first method (physical dice spawning)
    /// </summary>
    private void HandleFirstMethodInput()
    {
        // Handle input for web (mouse click) and mobile (touch)
        // Only allow rolling if it's a human player's turn
        if ((Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)))
        {
            // Check if it's a human player's turn
            bool isHumanTurn = playerManager == null || playerManager.IsHumanPlayerTurn();
            
            if (isHumanTurn && !CanRollDice)
            {
                // Debug why dice cannot be rolled
                if (isRolling)
                    Debug.Log("Cannot roll dice: Dice are currently rolling");
                else if (cardsManager != null && cardsManager.IsCardAnimating)
                    Debug.Log("Cannot roll dice: Card is animating");
                else if (GetCurrentPlayerController() != null && GetCurrentPlayerController().IsMoving)
                    Debug.Log("Cannot roll dice: Player is moving");
                else if (IsMiniGameActive())
                    Debug.Log("Cannot roll dice: Mini game is active");
                else
                    Debug.Log("Cannot roll dice: Unknown reason");
            }
            else if (isHumanTurn && CanRollDice)
            {
                RollDice();
            }
        }
        
        // Auto-roll for AI players
        if (playerManager != null && playerManager.IsAIPlayerTurn() && CanRollDice && !isRolling)
        {
            StartCoroutine(AutoRollForAI());
        }
        
        // Check if dice have finished rolling
        if (isRolling && diceManager != null && Time.time - lastCheckTime >= diceManager.DiceCheckInterval)
        {
            lastCheckTime = Time.time;
            
            // Check if we're using one dice mode
            PlayerController currentPlayerCtrl = GetCurrentPlayerController();
            bool useOneDice = currentPlayerCtrl != null && currentPlayerCtrl.ShouldUseOneDice;
            
            bool diceFinished = useOneDice 
                ? !firstDice.IsRolling 
                : (!firstDice.IsRolling && !secondDice.IsRolling);
            
            if (diceFinished)
            {
                isRolling = false;
                StartCoroutine(ProcessDiceResult());
            }
        }
    }
    
    /// <summary>
    /// Handles input for the second method (video-based dice)
    /// </summary>
    private void HandleSecondMethodInput()
    {
        // Handle input for web (mouse click) and mobile (touch)
        // Only allow rolling if it's a human player's turn
        if ((Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)))
        {
            // Check if it's a human player's turn
            bool isHumanTurn = playerManager == null || playerManager.IsHumanPlayerTurn();
            
            if (isHumanTurn && !CanRollDice)
            {
                // Debug why dice cannot be rolled
                if (isRolling)
                    Debug.Log("Cannot roll dice: Currently processing dice result");
                else if (cardsManager != null && cardsManager.IsCardAnimating)
                    Debug.Log("Cannot roll dice: Card is animating");
                else if (GetCurrentPlayerController() != null && GetCurrentPlayerController().IsMoving)
                    Debug.Log("Cannot roll dice: Player is moving");
                else if (IsMiniGameActive())
                    Debug.Log("Cannot roll dice: Mini game is active");
                else
                    Debug.Log("Cannot roll dice: Unknown reason");
            }
            else if (isHumanTurn && CanRollDice)
            {
                if (!isDiceMeterActive)
                {
                    // First click: Show DiceMeter
                    ShowDiceMeter();
                }
                else
                {
                    // Second click: Hide DiceMeter and roll dice
                    HideDiceMeterAndRoll();
                }
            }
        }
        
        // Auto-roll for AI players
        if (playerManager != null && playerManager.IsAIPlayerTurn() && CanRollDice && !isRolling)
        {
            StartCoroutine(AutoRollForAI_SecondMethod());
        }
    }
    
    /// <summary>
    /// Auto-roll dice for AI players (First Method)
    /// </summary>
    private IEnumerator AutoRollForAI()
    {
        if (playerManager == null || playerManager.CurrentPlayer == null || playerManager.CurrentPlayer.AIController == null)
        {
            yield break;
        }
        
        yield return StartCoroutine(playerManager.CurrentPlayer.AIController.RollDice(() => {
            RollDice();
        }));
    }
    
    /// <summary>
    /// Auto-roll dice for AI players (Second Method)
    /// </summary>
    private IEnumerator AutoRollForAI_SecondMethod()
    {
        if (playerManager == null || playerManager.CurrentPlayer == null || playerManager.CurrentPlayer.AIController == null)
        {
            yield break;
        }
        
        yield return StartCoroutine(playerManager.CurrentPlayer.AIController.RollDice(() => {
            // Simulate click: show meter, wait a bit, then roll
            ShowDiceMeter();
            StartCoroutine(DelayedRollForAI());
        }));
    }
    
    private IEnumerator DelayedRollForAI()
    {
        // Wait a short time to show the meter
        yield return new WaitForSeconds(0.5f);
        HideDiceMeterAndRoll();
    }
    
    /// <summary>
    /// Shows the DiceMeter video player (Second Method)
    /// </summary>
    private void ShowDiceMeter()
    {
        if (diceManager != null && diceManager.DiceMeter != null)
        {
            diceManager.DiceMeter.SetActive(true);
            isDiceMeterActive = true;
            Debug.Log("[GameManager] DiceMeter activated (Second Method)");
        }
    }
    
    /// <summary>
    /// Hides DiceMeter and starts the dice roll (Second Method)
    /// </summary>
    private void HideDiceMeterAndRoll()
    {
        if (diceManager != null && diceManager.DiceMeter != null)
        {
            diceManager.DiceMeter.SetActive(false);
            isDiceMeterActive = false;
            Debug.Log("[GameManager] DiceMeter deactivated, starting roll (Second Method)");
        }
        
        // Start the video-based dice roll
        StartCoroutine(RollDice_SecondMethod());
    }
    
    /// <summary>
    /// Rolls dice using video players (Second Method)
    /// </summary>
    private IEnumerator RollDice_SecondMethod()
    {
        if (isRolling || isProcessingDiceResult)
        {
            yield break;
        }
        
        isRolling = true;
        diceSum = 0;
        
        // Play rolling dice sound effect
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX("RollingDice");
        }
        
        // Check if we're using one dice mode
        PlayerController currentPlayerCtrl = GetCurrentPlayerController();
        bool useOneDice = currentPlayerCtrl != null && currentPlayerCtrl.ShouldUseOneDice;
        
        // Generate random dice values
        if (useOneDice)
        {
            firstDiceValue = Random.Range(1, 7); // 1-6
            secondDiceValue = 0;
            diceSum = firstDiceValue;
        }
        else
        {
            firstDiceValue = Random.Range(1, 7); // 1-6
            secondDiceValue = Random.Range(1, 7); // 1-6
            diceSum = firstDiceValue + secondDiceValue;
        }
        
        Debug.Log($"[GameManager] Second Method - Rolled: First={firstDiceValue}, Second={secondDiceValue}, Sum={diceSum}");
        
        // Check if both dice match (for extra turn)
        if (!useOneDice && diceManager != null && diceManager.AreDiceMatching(firstDiceValue, secondDiceValue))
        {
            shouldGrantExtraTurnForMatchingDice = true;
            Debug.Log($"=== MATCHING DICE! ===\n" +
                    $"Both dice show: {firstDiceValue}\n" +
                    $"Player will get an extra turn!\n" +
                    $"========================");
        }
        else
        {
            shouldGrantExtraTurnForMatchingDice = false;
        }
        
        // Show the appropriate video
        ShowDiceVideo(useOneDice, firstDiceValue, secondDiceValue);
        
        // Wait 5 seconds for the video to play
        yield return new WaitForSeconds(5f);
        
        // Hide the video
        HideDiceVideo();
        
        isRolling = false;
        
        // Process the dice result
        yield return StartCoroutine(ProcessDiceResult_SecondMethod());
    }
    
    /// <summary>
    /// Shows the appropriate dice video based on the roll (Second Method)
    /// </summary>
    private void ShowDiceVideo(bool useOneDice, int firstValue, int secondValue)
    {
        if (diceManager == null)
        {
            Debug.LogWarning("DiceManager is null! Cannot show dice video.");
            return;
        }
        
        // First, hide all videos
        HideDiceVideo();
        
        if (useOneDice)
        {
            // Single dice mode: show single_no1 to single_no6
            if (diceManager.SingleVideoPlayers != null && firstValue >= 1 && firstValue <= 6)
            {
                int index = firstValue - 1; // Convert 1-6 to 0-5
                if (index < diceManager.SingleVideoPlayers.Length && diceManager.SingleVideoPlayers[index] != null)
                {
                    diceManager.SingleVideoPlayers[index].SetActive(true);
                    Debug.Log($"[GameManager] Showing single dice video: single_no{firstValue}");
                }
                else
                {
                    Debug.LogWarning($"Single video player at index {index} is null or out of range!");
                }
            }
        }
        else
        {
            // Two dice mode
            bool isDouble = firstValue == secondValue;
            
            if (isDouble)
            {
                // Double roll: show double_no2_1+1, double_no4_2+2, etc.
                // Mapping: 1+1=2, 2+2=4, 3+3=6, 4+4=8, 5+5=10, 6+6=12
                int sum = firstValue + secondValue;
                GameObject[] doubleVideos = diceManager.DoubleVideoPlayers;
                
                if (doubleVideos != null)
                {
                    bool found = false;
                    
                    // First, try to find exact match (sum and values)
                    for (int i = 0; i < doubleVideos.Length; i++)
                    {
                        if (doubleVideos[i] != null)
                        {
                            string videoName = doubleVideos[i].name.ToLower();
                            // Check if video name contains the sum and values
                            if ((videoName.Contains($"no{sum}") || videoName.Contains($"_{sum}_")) &&
                                (videoName.Contains($"{firstValue}+{secondValue}") || videoName.Contains($"{secondValue}+{firstValue}")))
                            {
                                doubleVideos[i].SetActive(true);
                                Debug.Log($"[GameManager] Showing double dice video: {doubleVideos[i].name}");
                                found = true;
                                break;
                            }
                        }
                    }
                    
                    // If exact match not found, fall back to sum-based matching
                    if (!found)
                    {
                        for (int i = 0; i < doubleVideos.Length; i++)
                        {
                            if (doubleVideos[i] != null)
                            {
                                string videoName = doubleVideos[i].name.ToLower();
                                // Match by sum only (e.g., no7_3+4 can match no7_5+2)
                                if (videoName.Contains($"no{sum}") || videoName.Contains($"_{sum}_"))
                                {
                                    doubleVideos[i].SetActive(true);
                                    Debug.Log($"[GameManager] Showing double dice video (sum fallback): {doubleVideos[i].name} for {firstValue}+{secondValue} (sum={sum})");
                                    found = true;
                                    break;
                                }
                            }
                        }
                    }
                    
                    if (!found)
                    {
                        Debug.LogWarning($"Could not find matching double video for {firstValue}+{secondValue} (sum={sum})");
                    }
                }
            }
            else
            {
                // Non-double roll: show no3_1+2, no4_1+3, etc.
                // Need to find the correct video based on sum and individual values
                int sum = firstValue + secondValue;
                GameObject[] nonDoubleVideos = diceManager.NonDoubleVideoPlayers;
                
                if (nonDoubleVideos != null)
                {
                    // First, try to find exact match (sum and values)
                    // Videos: no3_1+2, no4_1+3, no5_2+3, no6_2+4, no7_3+4, no8_3+5, no9_4+5, no10_4+6, no11_5+6
                    bool found = false;
                    for (int i = 0; i < nonDoubleVideos.Length; i++)
                    {
                        if (nonDoubleVideos[i] != null)
                        {
                            string videoName = nonDoubleVideos[i].name.ToLower();
                            // Check if video name contains the sum and values
                            if ((videoName.Contains($"no{sum}") || videoName.Contains($"_{sum}_")) &&
                                (videoName.Contains($"{firstValue}+{secondValue}") || videoName.Contains($"{secondValue}+{firstValue}")))
                            {
                                nonDoubleVideos[i].SetActive(true);
                                Debug.Log($"[GameManager] Showing non-double dice video: {nonDoubleVideos[i].name}");
                                found = true;
                                break;
                            }
                        }
                    }
                    
                    // If exact match not found, fall back to sum-based matching
                    // (e.g., no7_3+4 can match no7_5+2 since both sum to 7)
                    if (!found)
                    {
                        for (int i = 0; i < nonDoubleVideos.Length; i++)
                        {
                            if (nonDoubleVideos[i] != null)
                            {
                                string videoName = nonDoubleVideos[i].name.ToLower();
                                // Match by sum only (e.g., no7_3+4 can match no7_5+2)
                                if (videoName.Contains($"no{sum}") || videoName.Contains($"_{sum}_"))
                                {
                                    nonDoubleVideos[i].SetActive(true);
                                    Debug.Log($"[GameManager] Showing non-double dice video (sum fallback): {nonDoubleVideos[i].name} for {firstValue}+{secondValue} (sum={sum})");
                                    found = true;
                                    break;
                                }
                            }
                        }
                    }
                    
                    if (!found)
                    {
                        Debug.LogWarning($"Could not find matching non-double video for {firstValue}+{secondValue} (sum={sum})");
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// Hides all dice videos (Second Method)
    /// </summary>
    private void HideDiceVideo()
    {
        if (diceManager == null)
        {
            return;
        }
        
        // Hide all double videos
        if (diceManager.DoubleVideoPlayers != null)
        {
            foreach (GameObject video in diceManager.DoubleVideoPlayers)
            {
                if (video != null)
                {
                    video.SetActive(false);
                }
            }
        }
        
        // Hide all single videos
        if (diceManager.SingleVideoPlayers != null)
        {
            foreach (GameObject video in diceManager.SingleVideoPlayers)
            {
                if (video != null)
                {
                    video.SetActive(false);
                }
            }
        }
        
        // Hide all non-double videos
        if (diceManager.NonDoubleVideoPlayers != null)
        {
            foreach (GameObject video in diceManager.NonDoubleVideoPlayers)
            {
                if (video != null)
                {
                    video.SetActive(false);
                }
            }
        }
    }
    
    /// <summary>
    /// Processes dice result for second method
    /// </summary>
    private IEnumerator ProcessDiceResult_SecondMethod()
    {
        isProcessingDiceResult = true;
        
        // Display the dice result
        PlayerController currentPlayerCtrl = GetCurrentPlayerController();
        bool useOneDice = currentPlayerCtrl != null && currentPlayerCtrl.ShouldUseOneDice;
        
        if (useOneDice)
        {
            string message = $"=== DICE ROLL RESULT (ONE DICE - SECOND METHOD) ===\n" +
                           $"Dice Value: {firstDiceValue}\n" +
                           $"TOTAL SUM: {diceSum}\n" +
                           $"========================";
            Debug.Log(message);
        }
        else
        {
            string message = $"=== DICE ROLL RESULT (SECOND METHOD) ===\n" +
                           $"First Dice: {firstDiceValue}\n" +
                           $"Second Dice: {secondDiceValue}\n" +
                           $"TOTAL SUM: {diceSum}\n" +
                           $"========================";
            Debug.Log(message);
        }
        
        DisplayDiceSum();
        
        // Determine movement steps: use fixed value if debugging, otherwise use dice sum
        int movementSteps = (diceManager != null && diceManager.IsDebuggingEnabled) ? diceManager.DebugFixedSteps : diceSum;
        
        // Log debug mode status if enabled
        if (diceManager != null && diceManager.IsDebuggingEnabled)
        {
            Debug.Log($"DEBUG MODE: Using fixed steps ({diceManager.DebugFixedSteps}) instead of dice sum ({diceSum})");
        }
        
        // Move current player based on calculated movement steps
        if (currentPlayerCtrl != null && movementSteps > 0)
        {
            currentPlayerCtrl.OnDiceRollComplete(movementSteps);
        }
        else
        {
            Debug.LogWarning("Current player not found! Cannot move player.");
            isProcessingDiceResult = false;
        }
        // Note: isProcessingDiceResult will be set to false in OnPlayerMovementComplete
        yield return null;
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
    
    /// <summary>
    /// Get the current player's PlayerFinance
    /// </summary>
    private PlayerFinance GetCurrentPlayerFinance()
    {
        if (playerManager != null && playerManager.CurrentPlayer != null)
        {
            return playerManager.CurrentPlayer.PlayerFinance;
        }
        
        // Return null silently - this is expected during initialization
        return null;
    }
    
    /// <summary>
    /// Called when the current player changes
    /// </summary>
    private void OnCurrentPlayerChanged(Player newPlayer)
    {
        // Unsubscribe from old player
        UnsubscribeFromPlayerEvents();
        
        // Reset extra turn flag when player changes
        shouldGrantExtraTurnForMatchingDice = false;
        
        // Subscribe to new player
        SubscribeToCurrentPlayerEvents();
        
        Debug.Log($"GameManager: Current player changed to {newPlayer.PlayerName}");
    }
    
    /// <summary>
    /// Subscribe to current player's events
    /// </summary>
    private void SubscribeToCurrentPlayerEvents()
    {
        PlayerController currentPlayerCtrl = GetCurrentPlayerController();
        if (currentPlayerCtrl != null)
        {
            currentPlayerCtrl.OnMovementComplete += OnPlayerMovementComplete;
            currentPlayerCtrl.OnPassedPath01Start += OnPlayerPassedPath01Start;
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
            currentPlayerCtrl.OnPassedPath01Start -= OnPlayerPassedPath01Start;
        }
    }
    
    
    public void SpawnDice()
    {
        // Check if using second method - if so, don't spawn physical dice
        if (diceManager != null && diceManager.UseSecondMethod)
        {
            // Second method doesn't spawn dice, just ensure videos are hidden
            HideDiceVideo();
            isDiceMeterActive = false;
            if (diceManager.DiceMeter != null)
            {
                diceManager.DiceMeter.SetActive(false);
            }
            return;
        }
        
        if (diceManager == null)
        {
            Debug.LogError("DiceManager is not assigned! Cannot spawn dice.");
            return;
        }
        
        // Check if current player should use one dice (after stopping on Fortune Road)
        PlayerController currentPlayerCtrl = GetCurrentPlayerController();
        bool useOneDice = currentPlayerCtrl != null && currentPlayerCtrl.ShouldUseOneDice;
        
        if (useOneDice && diceManager.OneDiceSpawner != null)
        {
            // One dice mode: spawn only one dice at OneDice/FirstSpawner
            Debug.Log("Spawning one dice at OneDice/FirstSpawner (Fortune Road mode)");
            
            // Destroy all existing dice from both spawners
            if (diceManager.FirstSpawner != null)
            {
                for (int i = diceManager.FirstSpawner.childCount - 1; i >= 0; i--)
                {
                    Destroy(diceManager.FirstSpawner.GetChild(i).gameObject);
                }
            }
            
            if (diceManager.SecondSpawner != null)
            {
                for (int i = diceManager.SecondSpawner.childCount - 1; i >= 0; i--)
                {
                    Destroy(diceManager.SecondSpawner.GetChild(i).gameObject);
                }
            }
            
            // Destroy any existing dice from one dice spawner
            if (diceManager.OneDiceSpawner != null)
            {
                for (int i = diceManager.OneDiceSpawner.childCount - 1; i >= 0; i--)
                {
                    Destroy(diceManager.OneDiceSpawner.GetChild(i).gameObject);
                }
            }
            
            // Spawn one dice at OneDice/FirstSpawner
            if (diceManager.DicePrefab != null && diceManager.OneDiceSpawner != null)
            {
                GameObject spawnedDice = Instantiate(diceManager.DicePrefab, diceManager.OneDiceSpawner.position, diceManager.OneDiceSpawner.rotation, diceManager.OneDiceSpawner);
                spawnedDice.name = "FirstDice";
                firstDice = spawnedDice.GetComponent<DiceController>();
                
                if (firstDice == null)
                {
                    firstDice = spawnedDice.AddComponent<DiceController>();
                }
                
                // Set second dice to null for one dice mode
                secondDice = null;
            }
            else
            {
                Debug.LogError("Dice Prefab or OneDiceSpawner is not assigned! Cannot spawn dice.");
            }
        }
        else
        {
            // Normal mode: spawn two dice
            // Remove any existing dice from first spawner
            if (diceManager.FirstSpawner != null)
            {
                // Destroy all children (existing dice)
                for (int i = diceManager.FirstSpawner.childCount - 1; i >= 0; i--)
                {
                    Destroy(diceManager.FirstSpawner.GetChild(i).gameObject);
                }
                
                // Spawn new dice at first spawner
                if (diceManager.DicePrefab != null)
                {
                    GameObject spawnedDice = Instantiate(diceManager.DicePrefab, diceManager.FirstSpawner.position, diceManager.FirstSpawner.rotation, diceManager.FirstSpawner);
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
            if (diceManager.SecondSpawner != null)
            {
                // Destroy all children (existing dice)
                for (int i = diceManager.SecondSpawner.childCount - 1; i >= 0; i--)
                {
                    Destroy(diceManager.SecondSpawner.GetChild(i).gameObject);
                }
                
                // Spawn new dice at second spawner
                if (diceManager.DicePrefab != null)
                {
                    GameObject spawnedDice = Instantiate(diceManager.DicePrefab, diceManager.SecondSpawner.position, diceManager.SecondSpawner.rotation, diceManager.SecondSpawner);
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
    }
    
    private void FindDice()
    {
        if (diceManager == null)
        {
            return;
        }
        
        // Try to find dice in spawners first
        if (diceManager.FirstSpawner != null)
        {
            firstDice = diceManager.FirstSpawner.GetComponentInChildren<DiceController>();
        }
        
        if (diceManager.SecondSpawner != null)
        {
            secondDice = diceManager.SecondSpawner.GetComponentInChildren<DiceController>();
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
        // Check if we should use one dice
        PlayerController currentPlayerCtrl = GetCurrentPlayerController();
        bool useOneDice = currentPlayerCtrl != null && currentPlayerCtrl.ShouldUseOneDice;
        
        if (isRolling || firstDice == null)
        {
            return;
        }
        
        // In one dice mode, secondDice can be null
        if (!useOneDice && secondDice == null)
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
        
        // Roll dice(s)
        if (useOneDice)
        {
            // Roll only first dice
            firstDice.RollDice();
            Debug.Log("Rolling one dice (Fortune Road mode)");
        }
        else
        {
            // Roll both dice
            firstDice.RollDice();
            secondDice.RollDice();
        }
        
        lastCheckTime = Time.time;
    }
    
    private IEnumerator ProcessDiceResult()
    {
        isProcessingDiceResult = true;
        
        // Check if we're using one dice mode
        PlayerController currentPlayerCtrl = GetCurrentPlayerController();
        bool useOneDice = currentPlayerCtrl != null && currentPlayerCtrl.ShouldUseOneDice;
        
        if (firstDice != null)
        {
            // Calculate dice sum
            if (useOneDice)
            {
                // One dice mode: use only first dice value
                diceSum = firstDice.CurrentValue;
                
                // Display the dice result
                string message = $"=== DICE ROLL RESULT (ONE DICE) ===\n" +
                               $"Dice Value: {firstDice.CurrentValue}\n" +
                               $"TOTAL SUM: {diceSum}\n" +
                               $"========================";
                
                Debug.Log(message);
            }
            else
            {
                // Two dice mode: sum both dice
                if (secondDice != null)
                {
                    diceSum = firstDice.CurrentValue + secondDice.CurrentValue;
                    
                    // Check if both dice have the same value (matching dice)
                    if (diceManager != null && diceManager.AreDiceMatching(firstDice.CurrentValue, secondDice.CurrentValue))
                    {
                        shouldGrantExtraTurnForMatchingDice = true;
                        Debug.Log($"=== MATCHING DICE! ===\n" +
                                $"Both dice show: {firstDice.CurrentValue}\n" +
                                $"Player will get an extra turn!\n" +
                                $"========================");
                    }
                    else
                    {
                        shouldGrantExtraTurnForMatchingDice = false;
                    }
                    
                    // Display the dice sum prominently
                    string message = $"=== DICE ROLL RESULT ===\n" +
                                   $"First Dice: {firstDice.CurrentValue}\n" +
                                   $"Second Dice: {secondDice.CurrentValue}\n" +
                                   $"TOTAL SUM: {diceSum}\n" +
                                   $"========================";
                    
                    Debug.Log(message);
                }
                else
                {
                    Debug.LogWarning("Second dice is null in two dice mode!");
                    isProcessingDiceResult = false;
                    yield break;
                }
            }
            
            DisplayDiceSum();
            
            // Determine movement steps: use fixed value if debugging, otherwise use dice sum
            int movementSteps = (diceManager != null && diceManager.IsDebuggingEnabled) ? diceManager.DebugFixedSteps : diceSum;
            
            // Always show dice: move them back to spawners, wait, then destroy
            // Move dice back to their spawners and show them
            yield return StartCoroutine(MoveDiceToSpawners());
            
            // Wait 2 seconds to show the dice numbers
            yield return new WaitForSeconds(2f);
            
            // Destroy the dice
            if (firstDice != null)
            {
                Destroy(firstDice.gameObject);
                firstDice = null;
            }
            
            if (secondDice != null)
            {
                Destroy(secondDice.gameObject);
                secondDice = null;
            }
            
            // Clear one dice flag immediately after using it, so next roll will be two dice
            if (useOneDice && currentPlayerCtrl != null)
            {
                // Clear the flag through a public method or directly if accessible
                currentPlayerCtrl.ClearOneDiceFlag();
                Debug.Log("One dice flag cleared. Next roll will use two dice.");
            }
            
            // Log debug mode status if enabled
            if (diceManager != null && diceManager.IsDebuggingEnabled)
            {
                Debug.Log($"DEBUG MODE: Using fixed steps ({diceManager.DebugFixedSteps}) instead of dice sum ({diceSum})");
            }
            
            // Move current player based on calculated movement steps
            if (currentPlayerCtrl != null && movementSteps > 0)
            {
                currentPlayerCtrl.OnDiceRollComplete(movementSteps);
            }
            else
            {
                Debug.LogWarning("Current player not found! Cannot move player.");
                isProcessingDiceResult = false;
            }
            // Note: isProcessingDiceResult will be set to false in OnPlayerMovementComplete
        }
        else
        {
            isProcessingDiceResult = false;
        }
    }
    
    private IEnumerator MoveDiceToSpawners()
    {
        float moveDuration = 0.5f; // Duration for moving dice back
        float elapsedTime = 0f;
        
        // Check if we're using one dice mode
        PlayerController currentPlayerCtrl = GetCurrentPlayerController();
        bool useOneDice = currentPlayerCtrl != null && currentPlayerCtrl.ShouldUseOneDice;
        
        if (firstDice == null)
        {
            Debug.LogWarning("First dice is null! Cannot move dice to spawner.");
            yield break;
        }
        
        Vector3 firstDiceStartPos = firstDice.transform.position;
        
        if (diceManager == null)
        {
            Debug.LogWarning("DiceManager is null! Cannot move dice to spawners.");
            yield break;
        }
        
        // Determine target spawner for first dice
        Transform firstTargetSpawner = null;
        if (useOneDice)
        {
            firstTargetSpawner = diceManager.OneDiceSpawner != null ? diceManager.OneDiceSpawner : diceManager.FirstSpawner;
            if (firstTargetSpawner == null)
            {
                Debug.LogWarning("One dice spawner and first spawner are both null! Using dice current position.");
                firstTargetSpawner = firstDice.transform;
            }
            else
            {
                Debug.Log($"Moving one dice to spawner: {firstTargetSpawner.name}");
            }
        }
        else
        {
            firstTargetSpawner = diceManager.FirstSpawner != null ? diceManager.FirstSpawner : firstDice.transform;
        }
        
        Vector3 firstDiceTargetPos = firstTargetSpawner.position;
        Quaternion firstDiceStartRot = firstDice.transform.rotation;
        
        Vector3 secondDiceStartPos = secondDice != null ? secondDice.transform.position : Vector3.zero;
        Vector3 secondDiceTargetPos = diceManager.SecondSpawner != null ? diceManager.SecondSpawner.position : secondDiceStartPos;
        Quaternion secondDiceStartRot = secondDice != null ? secondDice.transform.rotation : Quaternion.identity;
        
        // Calculate target rotations to show the rolled values on top
        // Get the base spawner rotations (firstTargetSpawner was already determined above)
        Quaternion firstSpawnerBaseRot = firstTargetSpawner != null ? firstTargetSpawner.rotation : Quaternion.identity;
        Quaternion secondSpawnerBaseRot = diceManager.SecondSpawner != null ? diceManager.SecondSpawner.rotation : Quaternion.identity;
        
        // Calculate rotation needed to show first dice value on top
        Quaternion firstDiceValueRot = firstDice != null ? firstDice.GetRotationForValueOnTop(firstDice.CurrentValue) : Quaternion.identity;
        Quaternion firstDiceTargetRot = firstSpawnerBaseRot * firstDiceValueRot;
        
        // Calculate rotation needed to show second dice value on top (only if using two dice)
        Quaternion secondDiceValueRot = secondDice != null ? secondDice.GetRotationForValueOnTop(secondDice.CurrentValue) : Quaternion.identity;
        Quaternion secondDiceTargetRot = secondSpawnerBaseRot * secondDiceValueRot;
        
        // Make dice kinematic so they can be moved smoothly
        Rigidbody firstRb = firstDice != null ? firstDice.GetComponent<Rigidbody>() : null;
        Rigidbody secondRb = !useOneDice && secondDice != null ? secondDice.GetComponent<Rigidbody>() : null;
        
        if (firstRb != null)
        {
            firstRb.isKinematic = true;
            firstRb.useGravity = false;
        }
        
        if (secondRb != null)
        {
            secondRb.isKinematic = true;
            secondRb.useGravity = false;
        }
        
        while (elapsedTime < moveDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / moveDuration;
            
            // Smooth curve (ease in-out)
            float curve = t * t * (3f - 2f * t);
            
            // Move first dice
            if (firstDice != null)
            {
                firstDice.transform.position = Vector3.Lerp(firstDiceStartPos, firstDiceTargetPos, curve);
                firstDice.transform.rotation = Quaternion.Lerp(firstDiceStartRot, firstDiceTargetRot, curve);
            }
            
            // Move second dice (only if using two dice mode)
            if (!useOneDice && secondDice != null)
            {
                secondDice.transform.position = Vector3.Lerp(secondDiceStartPos, secondDiceTargetPos, curve);
                secondDice.transform.rotation = Quaternion.Lerp(secondDiceStartRot, secondDiceTargetRot, curve);
            }
            
            yield return null;
        }
        
        // Ensure exact final positions
        if (firstDice != null)
        {
            firstDice.transform.position = firstDiceTargetPos;
            firstDice.transform.rotation = firstDiceTargetRot;
        }
        
        if (secondDice != null)
        {
            secondDice.transform.position = secondDiceTargetPos;
            secondDice.transform.rotation = secondDiceTargetRot;
        }
    }
    
    /// <summary>
    /// Called when player passes through Path01_Start during movement
    /// </summary>
    private void OnPlayerPassedPath01Start()
    {
        PlayerFinance currentPlayerFinance = GetCurrentPlayerFinance();
        if (currentPlayerFinance != null)
        {
            currentPlayerFinance.AddPaydayToCash();
            Debug.Log($"Player passed through Path01_Start! Added CurrentPayday ({currentPlayerFinance.CurrentPayday}) to cash. New Cash: {currentPlayerFinance.CurrentCash}");
        }
        else
        {
            Debug.LogWarning("PlayerFinance not found! Cannot add payday to cash.");
        }
    }
    
    private void OnPlayerMovementComplete()
    {
        // Get the current waypoint name to check if a card will be shown
        PlayerController currentPlayerCtrl = GetCurrentPlayerController();
        string currentWaypointName = currentPlayerCtrl != null ? currentPlayerCtrl.GetCurrentWaypointName() : string.Empty;
        
        // PRIORITY 0: Check if player should get extra turn for matching dice
        if (shouldGrantExtraTurnForMatchingDice)
        {
            Debug.Log($"[GameManager] Player rolled matching dice! Granting extra turn.");
            
            // Reset the flag
            shouldGrantExtraTurnForMatchingDice = false;
            
            // Spawn dice and keep the same player's turn (don't switch)
            SpawnDice();
            isProcessingDiceResult = false;
            
            Debug.Log($"[GameManager] Extra turn granted for matching dice. Player can roll again.");
            return; // Don't switch to next player - same player gets another turn
        }
        
        // PRIORITY 1: Check if player landed on Path33_FortuneRoad - grant extra turn with one dice
        if (!string.IsNullOrEmpty(currentWaypointName) && 
            currentWaypointName.Equals("Path33_FortuneRoad", System.StringComparison.OrdinalIgnoreCase))
        {
            Debug.Log($"[GameManager] Player landed on Path33_FortuneRoad! Granting extra turn with one dice.");
            
            // Set the one dice flag for the next roll
            if (currentPlayerCtrl != null)
            {
                // Use reflection or add a public method to set the flag
                // For now, we'll need to add a public method to PlayerController
                currentPlayerCtrl.SetOneDiceFlag();
            }
            
            // Spawn one dice and keep the same player's turn (don't switch)
            SpawnDice();
            isProcessingDiceResult = false;
            
            Debug.Log($"[GameManager] Extra turn granted. Player can now roll one dice.");
            return; // Don't switch to next player - same player gets another turn
        }
        
        // PRIORITY 2: Check if this is a Stock path (handled by StockPathManager)
        // Stock paths activate the StockMarket and must complete before switching players
        bool isStockPath = false;
        if (stockPathManager != null && !string.IsNullOrEmpty(currentWaypointName))
        {
            isStockPath = stockPathManager.IsStockPath(currentWaypointName);
        }
        else if (!string.IsNullOrEmpty(currentWaypointName))
        {
            // Fallback: Check if path contains "Stocks" keyword if StockPathManager is not available
            isStockPath = currentWaypointName.Contains("Stocks", System.StringComparison.OrdinalIgnoreCase);
        }
        
        if (isStockPath)
        {
            Debug.Log($"[GameManager] Stock path detected: '{currentWaypointName}'. Waiting for StockMarket to complete before switching players.");
            // Wait for stock market to complete, then spawn dice and switch to next player
            StartCoroutine(WaitForStockMarketAndRespawnDice());
            return;
        }
        
        bool willShowCard = false;
        
        // Check if a card will be spawned for this path
        if (cardsManager != null && !string.IsNullOrEmpty(currentWaypointName))
        {
            willShowCard = cardsManager.WillSpawnCardForPath(currentWaypointName);
        }
        
        // Check if there is a card currently animating
        bool isCardAnimating = cardsManager != null && cardsManager.IsCardAnimating;
        
        // If a card will be shown or is currently animating, wait for it to be destroyed before spawning dice
        if (willShowCard || isCardAnimating)
        {
            // Wait for card to be destroyed, then spawn dice and switch to next player
            // Check if this is Path33_FortuneRoad - if so, grant extra turn with one dice
            bool isFortuneRoadPath = !string.IsNullOrEmpty(currentWaypointName) && 
                                     currentWaypointName.Equals("Path33_FortuneRoad", System.StringComparison.OrdinalIgnoreCase);
            
            if (isFortuneRoadPath)
            {
                // Path33_FortuneRoad with card - wait for card, then grant extra turn with one dice
                StartCoroutine(WaitForCardAndGrantExtraTurn(currentPlayerCtrl));
            }
            else
            {
                // Normal card path - wait for card, then switch to next player
                StartCoroutine(WaitForCardAndRespawnDice());
            }
        }
        else
        {
            // No card on this path, spawn dice immediately and switch to next player
            SpawnDice();
            isProcessingDiceResult = false;
            
            // Switch to next player's turn
            SwitchToNextPlayer();
        }
    }
    
    /// <summary>
    /// Switch to the next player's turn
    /// </summary>
    private void SwitchToNextPlayer()
    {
        if (playerManager != null)
        {
            playerManager.NextPlayer();
        }
    }
    
    private IEnumerator WaitForCardAndRespawnDice()
    {
        // First, wait for the card to start animating (in case it hasn't started yet)
        // Give it a few frames for the card to be spawned and start animating
        int maxWaitFrames = 10;
        int framesWaited = 0;
        while (cardsManager != null && !cardsManager.IsCardAnimating && framesWaited < maxWaitFrames)
        {
            yield return null;
            framesWaited++;
        }
        
        // Now wait until card animation is complete (card is destroyed)
        while (cardsManager != null && cardsManager.IsCardAnimating)
        {
            yield return null;
        }
        
        // Add a small delay after card is destroyed to ensure it's completely gone
        yield return new WaitForSeconds(0.1f);
        
        // Card has been destroyed, spawn dice back
        SpawnDice();
        isProcessingDiceResult = false;
        
        // Switch to next player's turn
        SwitchToNextPlayer();
    }
    
    /// <summary>
    /// Waits for card to complete, then grants extra turn with one dice for Path33_FortuneRoad
    /// </summary>
    private IEnumerator WaitForCardAndGrantExtraTurn(PlayerController currentPlayerCtrl)
    {
        // First, wait for the card to start animating (in case it hasn't started yet)
        int maxWaitFrames = 10;
        int framesWaited = 0;
        while (cardsManager != null && !cardsManager.IsCardAnimating && framesWaited < maxWaitFrames)
        {
            yield return null;
            framesWaited++;
        }
        
        // Now wait until card animation is complete (card is destroyed)
        while (cardsManager != null && cardsManager.IsCardAnimating)
        {
            yield return null;
        }
        
        // Add a small delay after card is destroyed to ensure it's completely gone
        yield return new WaitForSeconds(0.1f);
        
        // Card has been destroyed, grant extra turn with one dice
        Debug.Log($"[GameManager] Card completed on Path33_FortuneRoad. Granting extra turn with one dice.");
        
        // Set the one dice flag for the next roll
        if (currentPlayerCtrl != null)
        {
            currentPlayerCtrl.SetOneDiceFlag();
        }
        
        // Spawn one dice and keep the same player's turn (don't switch)
        SpawnDice();
        isProcessingDiceResult = false;
        
        Debug.Log($"[GameManager] Extra turn granted. Player can now roll one dice.");
    }
    
    /// <summary>
    /// Waits for the StockMarket minigame to complete before spawning dice and switching to next player
    /// </summary>
    private IEnumerator WaitForStockMarketAndRespawnDice()
    {
        Debug.Log("[GameManager] Waiting for StockMarket to activate...");
        
        // First, wait a few frames for StockMarket to activate
        int maxWaitFrames = 10;
        int framesWaited = 0;
        while (!IsMiniGameActive() && framesWaited < maxWaitFrames)
        {
            yield return null;
            framesWaited++;
        }
        
        // If StockMarket didn't activate, log a warning but continue
        if (!IsMiniGameActive())
        {
            Debug.LogWarning("[GameManager] StockMarket did not activate. Proceeding anyway.");
        }
        else
        {
            Debug.Log("[GameManager] StockMarket activated. Waiting for it to complete...");
        }
        
        // Now wait until StockMarket is no longer active (player has finished with it)
        while (IsMiniGameActive())
        {
            yield return null;
        }
        
        Debug.Log("[GameManager] StockMarket completed. Spawning dice and switching to next player.");
        
        // Add a small delay after stock market closes to ensure clean state
        yield return new WaitForSeconds(0.1f);
        
        // StockMarket has been closed, spawn dice back
        SpawnDice();
        isProcessingDiceResult = false;
        
        // Switch to next player's turn
        SwitchToNextPlayer();
    }
    
    
    
    /// <summary>
    /// Displays the current dice sum. Can be called anytime to check the current values.
    /// </summary>
    public void DisplayDiceSum()
    {
        PlayerController currentPlayerCtrl = GetCurrentPlayerController();
        bool useOneDice = currentPlayerCtrl != null && currentPlayerCtrl.ShouldUseOneDice;
        
        if (firstDice != null)
        {
            if (useOneDice)
            {
                string status = $"Current Dice Status (One Dice Mode):\n" +
                              $"Dice Value: {firstDice.CurrentValue}\n" +
                              $"Total Sum: {diceSum}";
                Debug.Log(status);
            }
            else if (secondDice != null)
            {
                string status = $"Current Dice Status:\n" +
                              $"First Dice Value: {firstDice.CurrentValue}\n" +
                              $"Second Dice Value: {secondDice.CurrentValue}\n" +
                              $"Total Sum: {diceSum}";
                Debug.Log(status);
            }
            else
            {
                Debug.LogWarning("Second dice not found in two dice mode! Cannot display dice sum.");
            }
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
