using UnityEngine;
using System.Collections;

public class GameManager : MonoBehaviour
{
    [Header("Spawner References")]
    [SerializeField] private Transform firstSpawner;
    [SerializeField] private Transform secondSpawner;
    [SerializeField] private Transform oneDiceSpawner; // Spawner for one dice mode (OneDice/FirstSpawner)
    
    [Header("Dice References")]
    private DiceController firstDice; // Managed internally, not shown in Inspector
    private DiceController secondDice; // Managed internally, not shown in Inspector
    [SerializeField] private GameObject dicePrefab;
    
    [Header("Dice Settings")]
    [SerializeField] private float diceCheckInterval = 0.1f;
    
    [Header("Debug Settings")]
    [SerializeField] private bool IsDebugging = false; // Enable debug mode to use fixed movement steps
    [SerializeField] private int debugFixedSteps = 1; // Fixed number of steps to move when IsDebugging is true
    
    [Header("Player Manager Reference")]
    [SerializeField] private PlayerManager playerManager;
    
    [Header("Card Manager Reference")]
    [SerializeField] private CardsManager cardsManager;
    
    [Header("UI References")]
    [SerializeField] private GameObject miniGamesUI;
    
    private int diceSum = 0;
    private bool isRolling = false;
    private float lastCheckTime = 0f;
    private StockManager keyboardManager;
    private bool isProcessingDiceResult = false;
    
    public int DiceSum => diceSum;
    public bool IsRolling => isRolling;
    public bool CanRollDice => !isRolling && !isProcessingDiceResult && (cardsManager == null || !cardsManager.IsCardAnimating) && (GetCurrentPlayerController() == null || !GetCurrentPlayerController().IsMoving) && !IsMiniGameActive();
    
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
        
        // Find KeyboardManager to check MiniGameStockMarket status
        keyboardManager = FindAnyObjectByType<StockManager>();
        
        // Subscribe to current player's movement complete event
        SubscribeToCurrentPlayerEvents();
        
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
        if (isRolling && Time.time - lastCheckTime >= diceCheckInterval)
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
    /// Auto-roll dice for AI players
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
    
    private void FindSpawners()
    {
        // Try to find spawners by name
        GameObject firstSpawnerObj = GameObject.Find("FirstSpawner");
        GameObject secondSpawnerObj = GameObject.Find("SecondSpawner");
        
        // Try to find OneDice/FirstSpawner for one dice mode
        GameObject oneDiceParent = GameObject.Find("OneDice");
        if (oneDiceParent != null)
        {
            Transform oneDiceFirstSpawner = oneDiceParent.transform.Find("FirstSpawner");
            if (oneDiceFirstSpawner != null)
            {
                oneDiceSpawner = oneDiceFirstSpawner;
            }
        }
        
        // Fallback: try to find OneDice/FirstSpawner directly
        if (oneDiceSpawner == null)
        {
            Transform oneDiceFirstSpawner = GameObject.Find("OneDice/FirstSpawner")?.transform;
            if (oneDiceFirstSpawner != null)
            {
                oneDiceSpawner = oneDiceFirstSpawner;
            }
        }
        
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
    
    public void SpawnDice()
    {
        // Check if current player should use one dice (after stopping on Fortune Road)
        PlayerController currentPlayerCtrl = GetCurrentPlayerController();
        bool useOneDice = currentPlayerCtrl != null && currentPlayerCtrl.ShouldUseOneDice;
        
        if (useOneDice && oneDiceSpawner != null)
        {
            // One dice mode: spawn only one dice at OneDice/FirstSpawner
            Debug.Log("Spawning one dice at OneDice/FirstSpawner (Fortune Road mode)");
            
            // Destroy all existing dice from both spawners
            if (firstSpawner != null)
            {
                for (int i = firstSpawner.childCount - 1; i >= 0; i--)
                {
                    Destroy(firstSpawner.GetChild(i).gameObject);
                }
            }
            
            if (secondSpawner != null)
            {
                for (int i = secondSpawner.childCount - 1; i >= 0; i--)
                {
                    Destroy(secondSpawner.GetChild(i).gameObject);
                }
            }
            
            // Destroy any existing dice from one dice spawner
            if (oneDiceSpawner != null)
            {
                for (int i = oneDiceSpawner.childCount - 1; i >= 0; i--)
                {
                    Destroy(oneDiceSpawner.GetChild(i).gameObject);
                }
            }
            
            // Spawn one dice at OneDice/FirstSpawner
            if (dicePrefab != null && oneDiceSpawner != null)
            {
                GameObject spawnedDice = Instantiate(dicePrefab, oneDiceSpawner.position, oneDiceSpawner.rotation, oneDiceSpawner);
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
            int movementSteps = IsDebugging ? debugFixedSteps : diceSum;
            
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
            if (IsDebugging)
            {
                Debug.Log($"DEBUG MODE: Using fixed steps ({debugFixedSteps}) instead of dice sum ({diceSum})");
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
        
        // Determine target spawner for first dice
        Transform firstTargetSpawner = null;
        if (useOneDice)
        {
            firstTargetSpawner = oneDiceSpawner != null ? oneDiceSpawner : firstSpawner;
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
            firstTargetSpawner = firstSpawner != null ? firstSpawner : firstDice.transform;
        }
        
        Vector3 firstDiceTargetPos = firstTargetSpawner.position;
        Quaternion firstDiceStartRot = firstDice.transform.rotation;
        
        Vector3 secondDiceStartPos = secondDice != null ? secondDice.transform.position : Vector3.zero;
        Vector3 secondDiceTargetPos = secondSpawner != null ? secondSpawner.position : secondDiceStartPos;
        Quaternion secondDiceStartRot = secondDice != null ? secondDice.transform.rotation : Quaternion.identity;
        
        // Calculate target rotations to show the rolled values on top
        // Get the base spawner rotations (firstTargetSpawner was already determined above)
        Quaternion firstSpawnerBaseRot = firstTargetSpawner != null ? firstTargetSpawner.rotation : Quaternion.identity;
        Quaternion secondSpawnerBaseRot = secondSpawner != null ? secondSpawner.rotation : Quaternion.identity;
        
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
            StartCoroutine(WaitForCardAndRespawnDice());
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
