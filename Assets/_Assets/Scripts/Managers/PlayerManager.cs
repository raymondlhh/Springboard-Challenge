using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

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
        
        // Clear existing players
        foreach (var player in players)
        {
            if (player != null)
            {
                Destroy(player.gameObject);
            }
        }
        players.Clear();
        
        // Find Path01_Start for initial spawn position
        Transform startWaypoint = FindStartWaypoint();
        
        // Create players based on numberOfPlayers
        for (int i = 0; i < numberOfPlayers; i++)
        {
            bool isAI = i > 0; // First player (index 0) is human, rest are AI
            
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
            
            // Initialize player
            string playerName = isAI ? $"AI Player {i}" : "Human Player";
            player.Initialize(i, playerName, isAI);
            
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
            
            players.Add(player);
            Debug.Log($"Created {(isAI ? "AI" : "Human")} player: {playerName}");
        }
        
        Debug.Log($"Initialized {players.Count} player(s): 1 Human + {players.Count - 1} AI");
        
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
}
