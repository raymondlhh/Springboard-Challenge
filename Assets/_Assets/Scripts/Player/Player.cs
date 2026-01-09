using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Represents a player in the game (human or AI)
/// Contains all player-specific data: Model, Finance, and PlayerItems
/// </summary>
public class Player : MonoBehaviour
{
    [Header("Player Identity")]
    [SerializeField] private int playerID;
    [SerializeField] private string playerName;
    [SerializeField] private bool isAI;
    
    [Header("Player Components")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private PlayerFinance playerFinance;
    [SerializeField] private GameObject playerModel; // Visual representation
    
    [Header("Player Items")]
    [SerializeField] private List<GameObject> ownedPlayerItems = new List<GameObject>(); // List of PlayerItem GameObjects owned by this player
    
    [Header("AI Settings")]
    [SerializeField] private AIController aiController;
    
    // Properties
    public int PlayerID => playerID;
    public string PlayerName => playerName;
    public bool IsAI => isAI;
    public PlayerController PlayerController => playerController;
    public PlayerFinance PlayerFinance => playerFinance;
    public GameObject PlayerModel => playerModel;
    public IReadOnlyList<GameObject> OwnedPlayerItems => ownedPlayerItems;
    public AIController AIController => aiController;
    
    /// <summary>
    /// Initialize the player with ID, name, and AI status
    /// </summary>
    public void Initialize(int id, string name, bool ai)
    {
        playerID = id;
        playerName = name;
        isAI = ai;
        
        // Ensure components exist
        if (playerController == null)
        {
            playerController = GetComponent<PlayerController>();
            if (playerController == null)
            {
                playerController = gameObject.AddComponent<PlayerController>();
            }
        }
        
        if (playerFinance == null)
        {
            playerFinance = GetComponent<PlayerFinance>();
            if (playerFinance == null)
            {
                playerFinance = gameObject.AddComponent<PlayerFinance>();
            }
        }
        
        // Setup AI controller if this is an AI player
        if (isAI)
        {
            // Get or create AIController
            if (aiController == null)
            {
                aiController = GetComponent<AIController>();
                if (aiController == null)
                {
                    aiController = gameObject.AddComponent<AIController>();
                }
            }
            // Always initialize, even if it already existed (to ensure player reference is set)
            if (aiController != null)
            {
                aiController.Initialize(this);
                Debug.Log($"AIController initialized for AI player: {playerName}");
            }
        }
        
        // Initialize player items list
        if (ownedPlayerItems == null)
        {
            ownedPlayerItems = new List<GameObject>();
        }
        
        // Try to find player model from prefab structure if not already set
        if (playerModel == null)
        {
            // Look for a child GameObject that might be the model
            // Common names: "Model", "PlayerModel", "Mesh", or check for MeshRenderer
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                if (child.GetComponent<MeshRenderer>() != null || 
                    child.GetComponent<SkinnedMeshRenderer>() != null ||
                    child.name.Contains("Model", System.StringComparison.OrdinalIgnoreCase))
                {
                    playerModel = child.gameObject;
                    Debug.Log($"Found player model: {child.name}");
                    break;
                }
            }
        }
        
        Debug.Log($"Player {playerID} ({playerName}) initialized. IsAI: {isAI}");
    }
    
    /// <summary>
    /// Add a PlayerItem to this player's owned items
    /// </summary>
    public void AddPlayerItem(GameObject playerItem)
    {
        if (playerItem != null && !ownedPlayerItems.Contains(playerItem))
        {
            ownedPlayerItems.Add(playerItem);
            Debug.Log($"Player {playerName} now owns: {playerItem.name}");
        }
    }
    
    /// <summary>
    /// Remove a PlayerItem from this player's owned items
    /// </summary>
    public void RemovePlayerItem(GameObject playerItem)
    {
        if (playerItem != null && ownedPlayerItems.Contains(playerItem))
        {
            ownedPlayerItems.Remove(playerItem);
            Debug.Log($"Player {playerName} no longer owns: {playerItem.name}");
        }
    }
    
    /// <summary>
    /// Set the visual model for this player
    /// </summary>
    public void SetPlayerModel(GameObject model)
    {
        if (playerModel != null && playerModel != model)
        {
            // Optionally destroy old model
            // Destroy(playerModel);
        }
        
        playerModel = model;
        
        if (model != null)
        {
            model.transform.SetParent(transform);
            model.name = $"PlayerModel_{playerName}";
        }
    }
    
    /// <summary>
    /// Get a summary of this player's status
    /// </summary>
    public string GetPlayerSummary()
    {
        string summary = $"=== Player {playerID}: {playerName} ===\n";
        summary += $"Type: {(isAI ? "AI" : "Human")}\n";
        summary += $"Cash: ${playerFinance?.CurrentCash:F2}\n";
        summary += $"Payday: ${playerFinance?.CurrentPayday:F2}\n";
        summary += $"Owned Items: {ownedPlayerItems.Count}\n";
        summary += $"Current Position: {playerController?.GetCurrentWaypointName()}\n";
        return summary;
    }
    
    /// <summary>
    /// Reset player to initial state
    /// </summary>
    public void ResetPlayer()
    {
        if (playerController != null)
        {
            playerController.ResetPlayerPosition();
        }
        
        if (playerFinance != null)
        {
            playerFinance.ResetFinance();
        }
        
        // Clear owned items
        foreach (var item in ownedPlayerItems)
        {
            if (item != null)
            {
                Destroy(item);
            }
        }
        ownedPlayerItems.Clear();
    }
}
