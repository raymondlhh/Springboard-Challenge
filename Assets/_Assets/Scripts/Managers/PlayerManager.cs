using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;

/// <summary>
/// Manages all players in the game (1-4 players: 1 human + 0-3 AI)
/// Handles turn-based gameplay and player switching
/// </summary>
public class PlayerManager : MonoBehaviour
{
    [Header("Player Settings")]
    [Tooltip("Number of players (1-4). First player is always human, rest are AI.")]
    [Range(1, 4)]
    [SerializeField] private int numberOfPlayers = 4;
    [SerializeField] private GameObject playerPrefab; // Prefab with Player component (model should be inside this prefab)
    [SerializeField] private GameObject playerUIPrefab; // Prefab with PlayerUI component
    
    [Header("Player UI References")]
    [Tooltip("Custom names for each player. Element 0 = Player 1, Element 1 = Player 2, etc. Leave empty to use default names.")]
    [SerializeField] private List<string> playerNames = new List<string>();
    
    [Tooltip("Initial cash amount for each player. Element 0 = Player 1, Element 1 = Player 2, etc. Leave empty to use prefab default.")]
    [SerializeField] private List<float> playerInitialCash = new List<float>();
    
    [Tooltip("AI status for each player. Element 0 = Player 1, Element 1 = Player 2, etc. True = AI, False = Human. Leave empty to use default (first player human, rest AI).")]
    [SerializeField] private List<bool> playerIsAI = new List<bool>();
    
    [Tooltip("Purchase probability for each AI player (0-1). Element 0 = Player 1, Element 1 = Player 2, etc. 0 = never buy, 1 = always buy (if affordable). Only applies to AI players. Leave empty to use AIController default.")]
    [SerializeField] private List<float> playerPurchaseProbability = new List<float>();
    
    [Header("Player Name Texts (Auto-Assign)")]
    [Tooltip("NameText UI elements for each player. Can be manually assigned or auto-populated when players are spawned.")]
    [SerializeField] private List<TextMeshProUGUI> playerNameTexts = new List<TextMeshProUGUI>();
    
    [Header("Player Spawn Settings")]
    [Tooltip("Spawn positions for each player. Should have at least as many spawn points as numberOfPlayers.")]
    [SerializeField] private Transform[] playerSpawnPoints = new Transform[4]; // Spawn positions for each player
    
    [Header("Waypoint Settings")]
    [Tooltip("Parent GameObject containing all path waypoints (e.g., 'Paths' or 'Gamemap/Paths'). Leave empty to auto-find.")]
    [SerializeField] private Transform pathsParent;
    [Tooltip("Parent GameObject containing Fortune Road waypoints (e.g., 'FortuneRoad'). Leave empty to auto-find.")]
    [SerializeField] private Transform fortuneRoadParent;
    
    [Header("Current Player")]
    [SerializeField] private int currentPlayerIndex = 0;
    
    private List<Player> players = new List<Player>();
    private Player currentPlayer;
    
    // Events
    public System.Action<Player> OnPlayerTurnStarted;
    public System.Action<Player> OnPlayerTurnEnded;
    public System.Action<Player> OnCurrentPlayerChanged;
    
    // Properties
    public int NumberOfPlayers => numberOfPlayers;
    public Player CurrentPlayer => currentPlayer;
    public int CurrentPlayerIndex => currentPlayerIndex;
    public IReadOnlyList<Player> AllPlayers => players;
    
    void Start()
    {
        // Validate number of players
        ValidateNumberOfPlayers();
        
        // Initialize players if not already initialized
        if (players.Count == 0)
        {
            InitializePlayers();
        }
    }
    
    /// <summary>
    /// Validate and clamp the number of players to valid range (1-4)
    /// </summary>
    private void ValidateNumberOfPlayers()
    {
        if (numberOfPlayers < 1)
        {
            Debug.LogWarning($"Number of players ({numberOfPlayers}) is less than 1. Setting to 1.");
            numberOfPlayers = 1;
        }
        else if (numberOfPlayers > 4)
        {
            Debug.LogWarning($"Number of players ({numberOfPlayers}) is greater than 4. Setting to 4.");
            numberOfPlayers = 4;
        }
    }
    
    /// <summary>
    /// Set the number of players before the game starts
    /// Call this before Start() or before InitializePlayers()
    /// </summary>
    public void SetNumberOfPlayers(int count)
    {
        numberOfPlayers = Mathf.Clamp(count, 1, 4);
        Debug.Log($"Number of players set to: {numberOfPlayers}");
        
        // If players are already initialized, reinitialize with new count
        if (players.Count > 0)
        {
            Debug.LogWarning("Players already initialized. Reinitializing with new player count.");
            InitializePlayers();
        }
    }
    
    /// <summary>
    /// Initialize all players (1 human + 0-3 AI depending on numberOfPlayers)
    /// </summary>
    public void InitializePlayers()
    {
        // Validate before initializing
        ValidateNumberOfPlayers();
        
        // Clear existing players and their UI
        foreach (var player in players)
        {
            if (player != null)
            {
                // Destroy associated PlayerUI
                DestroyPlayerUI(player);
                Destroy(player.gameObject);
            }
        }
        players.Clear();
        
        // Clear NameText list
        playerNameTexts.Clear();
        
        // Find Path01_Start for initial spawn position
        Transform startWaypoint = FindStartWaypoint();
        
        // Create players based on numberOfPlayers
        for (int i = 0; i < numberOfPlayers; i++)
        {
            // Determine if player is AI from list, or use default (first player human, rest AI)
            bool isAI;
            if (i < playerIsAI.Count)
            {
                isAI = playerIsAI[i];
            }
            else
            {
                // Default behavior: first player (index 0) is human, rest are AI
                isAI = i > 0;
            }
            
            // Determine spawn position
            Vector3 spawnPos = Vector3.zero;
            if (playerSpawnPoints != null && i < playerSpawnPoints.Length && playerSpawnPoints[i] != null)
            {
                spawnPos = playerSpawnPoints[i].position;
            }
            else if (startWaypoint != null)
            {
                // Use Path01_Start if spawn points not set
                spawnPos = startWaypoint.position;
            }
            
            // Create player GameObject
            GameObject playerObj;
            if (playerPrefab != null)
            {
                playerObj = Instantiate(playerPrefab, spawnPos, Quaternion.identity);
            }
            else
            {
                // Create empty GameObject if no prefab
                playerObj = new GameObject($"Player_{i + 1}");
                playerObj.transform.position = spawnPos;
            }
            
            // Add Player component if not present
            Player player = playerObj.GetComponent<Player>();
            if (player == null)
            {
                player = playerObj.AddComponent<Player>();
            }
            
            // Initialize player with custom name if provided, otherwise use default
            string playerName;
            if (i < playerNames.Count && !string.IsNullOrEmpty(playerNames[i]))
            {
                // Use custom name from list
                playerName = playerNames[i];
            }
            else
            {
                // Use default name
                playerName = isAI ? $"AI Player {i}" : "Human Player";
            }
            player.Initialize(i, playerName, isAI);
            
            // Set initial cash if provided in list
            if (player.PlayerFinance != null && i < playerInitialCash.Count)
            {
                float initialCash = playerInitialCash[i];
                if (initialCash > 0)
                {
                    player.PlayerFinance.SetInitialCash(initialCash);
                    Debug.Log($"Set initial cash for {playerName} to: {initialCash}");
                }
            }
            
            // Set purchase probability for AI players if provided in list
            if (isAI && player.AIController != null && i < playerPurchaseProbability.Count)
            {
                float purchaseProb = playerPurchaseProbability[i];
                if (purchaseProb >= 0f && purchaseProb <= 1f)
                {
                    player.AIController.SetPurchaseProbability(purchaseProb);
                    Debug.Log($"Set purchase probability for {playerName} to: {purchaseProb}");
                }
            }
            
            // Auto-assign waypoints to player's PlayerController
            AssignWaypointsToPlayer(player);
            
            // Position player at start waypoint if waypoints were assigned
            if (player.PlayerController != null && player.PlayerController.PathWaypoints != null && 
                player.PlayerController.PathWaypoints.Count > 0)
            {
                Transform firstWaypoint = player.PlayerController.PathWaypoints[0];
                if (firstWaypoint != null)
                {
                    playerObj.transform.position = firstWaypoint.position;
                    Debug.Log($"Positioned {playerName} at start waypoint: {firstWaypoint.name}");
                }
            }
            
            // Note: Player model should already be part of the playerPrefab structure
            // The Player class will find it automatically if needed
            
            // Spawn PlayerUI for this player
            SpawnPlayerUI(player, playerName);
            
            players.Add(player);
            Debug.Log($"Created {(isAI ? "AI" : "Human")} player: {playerName}");
        }
        
        // Count human and AI players for summary
        int humanCount = players.Count(p => !p.IsAI);
        int aiCount = players.Count(p => p.IsAI);
        Debug.Log($"Initialized {players.Count} player(s): {humanCount} Human + {aiCount} AI");
        
        // Set first player as current
        if (players.Count > 0)
        {
            SetCurrentPlayer(0);
        }
        else
        {
            Debug.LogError("No players were created! Check playerPrefab and spawn points.");
        }
    }
    
    /// <summary>
    /// Set the current active player
    /// </summary>
    public void SetCurrentPlayer(int playerIndex)
    {
        if (playerIndex < 0 || playerIndex >= players.Count)
        {
            Debug.LogError($"Invalid player index: {playerIndex}");
            return;
        }
        
        // End previous player's turn
        if (currentPlayer != null)
        {
            OnPlayerTurnEnded?.Invoke(currentPlayer);
        }
        
        // Set new current player
        currentPlayerIndex = playerIndex;
        currentPlayer = players[playerIndex];
        
        Debug.Log($"Current player changed to: {currentPlayer.PlayerName} (Index: {currentPlayerIndex})");
        
        OnCurrentPlayerChanged?.Invoke(currentPlayer);
        OnPlayerTurnStarted?.Invoke(currentPlayer);
    }
    
    /// <summary>
    /// Move to the next player's turn
    /// </summary>
    public void NextPlayer()
    {
        int nextIndex = (currentPlayerIndex + 1) % players.Count;
        SetCurrentPlayer(nextIndex);
    }
    
    /// <summary>
    /// Get a player by ID
    /// </summary>
    public Player GetPlayer(int playerID)
    {
        return players.FirstOrDefault(p => p.PlayerID == playerID);
    }
    
    /// <summary>
    /// Get a player by index
    /// </summary>
    public Player GetPlayerByIndex(int index)
    {
        if (index >= 0 && index < players.Count)
        {
            return players[index];
        }
        return null;
    }
    
    /// <summary>
    /// Get the human player
    /// </summary>
    public Player GetHumanPlayer()
    {
        return players.FirstOrDefault(p => !p.IsAI);
    }
    
    /// <summary>
    /// Get all AI players
    /// </summary>
    public List<Player> GetAIPlayers()
    {
        return players.Where(p => p.IsAI).ToList();
    }
    
    /// <summary>
    /// Check if it's currently a human player's turn
    /// </summary>
    public bool IsHumanPlayerTurn()
    {
        return currentPlayer != null && !currentPlayer.IsAI;
    }
    
    /// <summary>
    /// Check if it's currently an AI player's turn
    /// </summary>
    public bool IsAIPlayerTurn()
    {
        return currentPlayer != null && currentPlayer.IsAI;
    }
    
    /// <summary>
    /// Reset all players to initial state
    /// </summary>
    public void ResetAllPlayers()
    {
        foreach (var player in players)
        {
            if (player != null)
            {
                player.ResetPlayer();
            }
        }
        
        if (players.Count > 0)
        {
            SetCurrentPlayer(0);
        }
    }
    
    /// <summary>
    /// Get the number of human players (should always be 1)
    /// </summary>
    public int GetHumanPlayerCount()
    {
        return players.Count(p => !p.IsAI);
    }
    
    /// <summary>
    /// Get the number of AI players
    /// </summary>
    public int GetAIPlayerCount()
    {
        return players.Count(p => p.IsAI);
    }
    
    /// <summary>
    /// Get summary of all players
    /// </summary>
    public string GetAllPlayersSummary()
    {
        string summary = "=== ALL PLAYERS SUMMARY ===\n\n";
        foreach (var player in players)
        {
            summary += player.GetPlayerSummary() + "\n";
        }
        return summary;
    }
    
    /// <summary>
    /// Automatically assign path waypoints and Fortune Road waypoints to a player
    /// </summary>
    private void AssignWaypointsToPlayer(Player player)
    {
        if (player == null || player.PlayerController == null)
        {
            Debug.LogWarning("Cannot assign waypoints: Player or PlayerController is null");
            return;
        }
        
        // Find and assign main path waypoints
        List<Transform> pathWaypoints = FindPathWaypoints();
        if (pathWaypoints != null && pathWaypoints.Count > 0)
        {
            player.PlayerController.SetPathWaypoints(pathWaypoints);
        }
        else
        {
            Debug.LogWarning("No path waypoints found! Player movement may not work correctly.");
        }
        
        // Find and assign Fortune Road waypoints
        List<Transform> fortuneRoadWaypoints = FindFortuneRoadWaypoints();
        if (fortuneRoadWaypoints != null && fortuneRoadWaypoints.Count > 0)
        {
            player.PlayerController.SetFortuneRoadWaypoints(fortuneRoadWaypoints);
        }
        else
        {
            Debug.LogWarning("No Fortune Road waypoints found!");
        }
        
        // Find and assign Path39 waypoint (exit from Fortune Road)
        Transform path39Waypoint = FindPath39Waypoint();
        if (path39Waypoint != null)
        {
            player.PlayerController.SetPath39Waypoint(path39Waypoint);
        }
    }
    
    /// <summary>
    /// Find all main path waypoints from the scene
    /// </summary>
    private List<Transform> FindPathWaypoints()
    {
        List<Transform> waypoints = new List<Transform>();
        
        // Try to find paths parent
        Transform pathsParentTransform = pathsParent;
        if (pathsParentTransform == null)
        {
            // Try common names
            GameObject pathsObj = GameObject.Find("Paths");
            if (pathsObj == null)
            {
                pathsObj = GameObject.Find("Gamemap/Paths");
            }
            if (pathsObj != null)
            {
                pathsParentTransform = pathsObj.transform;
            }
        }
        
        if (pathsParentTransform != null)
        {
            // Get all children that start with "Path" and sort them
            List<Transform> pathTransforms = new List<Transform>();
            for (int i = 0; i < pathsParentTransform.childCount; i++)
            {
                Transform child = pathsParentTransform.GetChild(i);
                if (child.name.StartsWith("Path", System.StringComparison.OrdinalIgnoreCase))
                {
                    pathTransforms.Add(child);
                }
            }
            
            // Sort by name to ensure correct order (Path01, Path02, etc.)
            pathTransforms.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.OrdinalIgnoreCase));
            waypoints.AddRange(pathTransforms);
            
            Debug.Log($"Found {waypoints.Count} path waypoints from '{pathsParentTransform.name}'");
        }
        else
        {
            // Fallback: Search entire scene for Path objects
            GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            List<Transform> pathTransforms = new List<Transform>();
            
            foreach (GameObject obj in allObjects)
            {
                if (obj.name.StartsWith("Path", System.StringComparison.OrdinalIgnoreCase) && 
                    !obj.name.Contains("FortuneRoad", System.StringComparison.OrdinalIgnoreCase))
                {
                    pathTransforms.Add(obj.transform);
                }
            }
            
            // Sort by name
            pathTransforms.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.OrdinalIgnoreCase));
            waypoints.AddRange(pathTransforms);
            
            Debug.Log($"Found {waypoints.Count} path waypoints by searching entire scene");
        }
        
        return waypoints;
    }
    
    /// <summary>
    /// Find all Fortune Road waypoints from the scene
    /// </summary>
    private List<Transform> FindFortuneRoadWaypoints()
    {
        List<Transform> waypoints = new List<Transform>();
        
        // Try to find Fortune Road parent
        Transform fortuneRoadParentTransform = fortuneRoadParent;
        if (fortuneRoadParentTransform == null)
        {
            GameObject fortuneRoadObj = GameObject.Find("FortuneRoad");
            if (fortuneRoadObj != null)
            {
                fortuneRoadParentTransform = fortuneRoadObj.transform;
            }
        }
        
        if (fortuneRoadParentTransform != null)
        {
            // Get all children that start with "Path" and sort them
            List<Transform> pathTransforms = new List<Transform>();
            for (int i = 0; i < fortuneRoadParentTransform.childCount; i++)
            {
                Transform child = fortuneRoadParentTransform.GetChild(i);
                if (child.name.StartsWith("Path", System.StringComparison.OrdinalIgnoreCase))
                {
                    pathTransforms.Add(child);
                }
            }
            
            // Sort by name to ensure correct order
            pathTransforms.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.OrdinalIgnoreCase));
            waypoints.AddRange(pathTransforms);
            
            Debug.Log($"Found {waypoints.Count} Fortune Road waypoints from '{fortuneRoadParentTransform.name}'");
        }
        else
        {
            // Fallback: Search for objects with "Path" and "FortuneRoad" in name
            GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            List<Transform> pathTransforms = new List<Transform>();
            
            foreach (GameObject obj in allObjects)
            {
                if (obj.name.StartsWith("Path", System.StringComparison.OrdinalIgnoreCase) && 
                    (obj.name.Contains("FortuneRoad", System.StringComparison.OrdinalIgnoreCase) ||
                     obj.transform.parent != null && obj.transform.parent.name.Contains("FortuneRoad", System.StringComparison.OrdinalIgnoreCase)))
                {
                    // Skip Path33_FortuneRoad (entry point, not part of the sequence)
                    if (!obj.name.Contains("Path33", System.StringComparison.OrdinalIgnoreCase))
                    {
                        pathTransforms.Add(obj.transform);
                    }
                }
            }
            
            // Sort by name
            pathTransforms.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.OrdinalIgnoreCase));
            waypoints.AddRange(pathTransforms);
            
            Debug.Log($"Found {waypoints.Count} Fortune Road waypoints by searching entire scene");
        }
        
        return waypoints;
    }
    
    /// <summary>
    /// Find Path39_TreasureChest waypoint (exit from Fortune Road)
    /// </summary>
    private Transform FindPath39Waypoint()
    {
        // Try to find by exact name
        GameObject path39Obj = GameObject.Find("Path39_TreasureChest");
        if (path39Obj == null)
        {
            path39Obj = GameObject.Find("Path39");
        }
        
        if (path39Obj != null)
        {
            Debug.Log($"Found Path39 waypoint: {path39Obj.name}");
            return path39Obj.transform;
        }
        
        // Fallback: Search for any object with Path39 in name
        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        foreach (GameObject obj in allObjects)
        {
            if (obj.name.Contains("Path39", System.StringComparison.OrdinalIgnoreCase))
            {
                Debug.Log($"Found Path39 waypoint: {obj.name}");
                return obj.transform;
            }
        }
        
        Debug.LogWarning("Path39 waypoint not found!");
        return null;
    }
    
    /// <summary>
    /// Find Path01_Start waypoint (starting position)
    /// </summary>
    private Transform FindStartWaypoint()
    {
        // Try to find by exact name
        GameObject startObj = GameObject.Find("Path01_Start");
        if (startObj == null)
        {
            startObj = GameObject.Find("Path001_Start");
        }
        if (startObj == null)
        {
            startObj = GameObject.Find("Path01");
        }
        
        if (startObj != null)
        {
            Debug.Log($"Found start waypoint: {startObj.name}");
            return startObj.transform;
        }
        
        // Fallback: Search for any object with Path01 in name
        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        foreach (GameObject obj in allObjects)
        {
            if (obj.name.Contains("Path01", System.StringComparison.OrdinalIgnoreCase) && 
                obj.name.Contains("Start", System.StringComparison.OrdinalIgnoreCase))
            {
                Debug.Log($"Found start waypoint: {obj.name}");
                return obj.transform;
            }
        }
        
        Debug.LogWarning("Start waypoint (Path01_Start) not found! Players may spawn at origin.");
        return null;
    }
    
    /// <summary>
    /// Spawn PlayerUI for a player inside PlayerProfiles
    /// </summary>
    private void SpawnPlayerUI(Player player, string playerName)
    {
        if (playerUIPrefab == null)
        {
            Debug.LogWarning("PlayerUI prefab is not assigned! Cannot spawn PlayerUI.");
            return;
        }
        
        // Find PlayerProfiles GameObject
        GameObject playerProfiles = GameObject.Find("PlayerProfiles");
        if (playerProfiles == null)
        {
            // Try alternative paths
            playerProfiles = GameObject.Find("Canvas/MainUI/PlayerProfiles");
            if (playerProfiles == null)
            {
                playerProfiles = GameObject.Find("MainUI/PlayerProfiles");
            }
        }
        
        if (playerProfiles == null)
        {
            Debug.LogWarning("PlayerProfiles GameObject not found! Cannot spawn PlayerUI.");
            return;
        }
        
        // Instantiate PlayerUI as child of PlayerProfiles
        GameObject playerUIObj = Instantiate(playerUIPrefab, playerProfiles.transform);
        playerUIObj.name = $"PlayerUI_{playerName}";
        
        // Get PlayerUI component and initialize it
        PlayerUI playerUI = playerUIObj.GetComponent<PlayerUI>();
        if (playerUI == null)
        {
            Debug.LogWarning($"PlayerUI component not found on prefab! GameObject: {playerUIPrefab.name}");
            return;
        }
        
        // Initialize PlayerUI with player name and finance
        if (player.PlayerFinance != null)
        {
            playerUI.Initialize(playerName, player.PlayerFinance);
            Debug.Log($"Spawned and initialized PlayerUI for {playerName}");
        }
        else
        {
            Debug.LogWarning($"PlayerFinance is null for player {playerName}! PlayerUI may not work correctly.");
            playerUI.SetPlayerName(playerName);
        }
        
        // Extract and store NameText reference
        TextMeshProUGUI nameText = GetNameTextFromPlayerUI(playerUI);
        if (nameText != null)
        {
            // Ensure list is large enough
            while (playerNameTexts.Count <= player.PlayerID)
            {
                playerNameTexts.Add(null);
            }
            playerNameTexts[player.PlayerID] = nameText;
            Debug.Log($"Added NameText for player {playerName} (ID: {player.PlayerID}) to list");
        }
        else
        {
            Debug.LogWarning($"Could not find NameText in PlayerUI for {playerName}");
        }
    }
    
    /// <summary>
    /// Get NameText component from PlayerUI
    /// </summary>
    private TextMeshProUGUI GetNameTextFromPlayerUI(PlayerUI playerUI)
    {
        if (playerUI == null) return null;
        
        // Try to find NameText in children
        TextMeshProUGUI[] texts = playerUI.GetComponentsInChildren<TextMeshProUGUI>();
        foreach (var text in texts)
        {
            if (text.name == "NameText")
            {
                return text;
            }
        }
        
        // Try to find by Transform path
        Transform nameTextTransform = playerUI.transform.Find("NameText");
        if (nameTextTransform == null)
        {
            nameTextTransform = playerUI.transform.Find("UpperBanner/NameText");
        }
        if (nameTextTransform != null)
        {
            return nameTextTransform.GetComponent<TextMeshProUGUI>();
        }
        
        return null;
    }
    
    /// <summary>
    /// Destroy PlayerUI associated with a player
    /// </summary>
    private void DestroyPlayerUI(Player player)
    {
        if (player == null) return;
        
        // Find PlayerProfiles
        GameObject playerProfiles = GameObject.Find("PlayerProfiles");
        if (playerProfiles == null)
        {
            playerProfiles = GameObject.Find("Canvas/MainUI/PlayerProfiles");
            if (playerProfiles == null)
            {
                playerProfiles = GameObject.Find("MainUI/PlayerProfiles");
            }
        }
        
        if (playerProfiles == null) return;
        
        // Find and destroy PlayerUI with matching name
        string playerUIName = $"PlayerUI_{player.PlayerName}";
        Transform playerUITransform = playerProfiles.transform.Find(playerUIName);
        if (playerUITransform != null)
        {
            Destroy(playerUITransform.gameObject);
            Debug.Log($"Destroyed PlayerUI for {player.PlayerName}");
        }
        
        // Remove NameText from list
        if (player.PlayerID >= 0 && player.PlayerID < playerNameTexts.Count)
        {
            playerNameTexts[player.PlayerID] = null;
        }
    }
    
    /// <summary>
    /// Get NameText for a specific player by ID
    /// </summary>
    public TextMeshProUGUI GetPlayerNameText(int playerID)
    {
        if (playerID >= 0 && playerID < playerNameTexts.Count)
        {
            return playerNameTexts[playerID];
        }
        return null;
    }
    
    /// <summary>
    /// Get NameText for a specific player by Player reference
    /// </summary>
    public TextMeshProUGUI GetPlayerNameText(Player player)
    {
        if (player != null)
        {
            return GetPlayerNameText(player.PlayerID);
        }
        return null;
    }
    
    /// <summary>
    /// Get all NameText references
    /// </summary>
    public IReadOnlyList<TextMeshProUGUI> GetAllPlayerNameTexts()
    {
        return playerNameTexts.AsReadOnly();
    }
}
