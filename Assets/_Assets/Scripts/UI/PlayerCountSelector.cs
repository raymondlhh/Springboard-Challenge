using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI component to select the number of players before the game starts
/// Attach this to a UI panel with buttons or a dropdown
/// </summary>
public class PlayerCountSelector : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button[] playerCountButtons; // Buttons for 1, 2, 3, 4 players
    [SerializeField] private TMP_Dropdown playerCountDropdown; // Alternative: Dropdown menu
    [SerializeField] private GameObject gameStartPanel; // Panel to hide when game starts
    [SerializeField] private GameObject gameUI; // Main game UI to show when game starts
    
    [Header("Player Manager Reference")]
    [SerializeField] private PlayerManager playerManager;
    
    private int selectedPlayerCount = 4;
    
    void Start()
    {
        // Find PlayerManager if not assigned
        if (playerManager == null)
        {
            playerManager = FindAnyObjectByType<PlayerManager>();
        }
        
        // Setup button listeners
        if (playerCountButtons != null && playerCountButtons.Length >= 4)
        {
            for (int i = 0; i < playerCountButtons.Length && i < 4; i++)
            {
                int playerCount = i + 1; // 1, 2, 3, 4
                if (playerCountButtons[i] != null)
                {
                    playerCountButtons[i].onClick.RemoveAllListeners();
                    playerCountButtons[i].onClick.AddListener(() => OnPlayerCountSelected(playerCount));
                }
            }
        }
        
        // Setup dropdown listener
        if (playerCountDropdown != null)
        {
            playerCountDropdown.onValueChanged.RemoveAllListeners();
            playerCountDropdown.onValueChanged.AddListener(OnDropdownValueChanged);
            
            // Set default value
            playerCountDropdown.value = 3; // 4 players (0-indexed: 0=1, 1=2, 2=3, 3=4)
        }
        
        // Show selection UI, hide game UI
        if (gameStartPanel != null)
        {
            gameStartPanel.SetActive(true);
        }
        
        if (gameUI != null)
        {
            gameUI.SetActive(false);
        }
    }
    
    /// <summary>
    /// Called when a player count button is clicked
    /// </summary>
    public void OnPlayerCountSelected(int count)
    {
        selectedPlayerCount = Mathf.Clamp(count, 1, 4);
        Debug.Log($"Player count selected: {selectedPlayerCount}");
        
        // Set the number of players in PlayerManager
        if (playerManager != null)
        {
            playerManager.SetNumberOfPlayers(selectedPlayerCount);
        }
        else
        {
            Debug.LogError("PlayerManager not found! Cannot set player count.");
        }
    }
    
    /// <summary>
    /// Called when dropdown value changes
    /// </summary>
    private void OnDropdownValueChanged(int index)
    {
        int playerCount = index + 1; // Convert 0-3 to 1-4
        OnPlayerCountSelected(playerCount);
    }
    
    /// <summary>
    /// Start the game with selected player count
    /// Call this from a "Start Game" button
    /// </summary>
    public void StartGame()
    {
        // Ensure player count is set
        if (playerManager != null)
        {
            playerManager.SetNumberOfPlayers(selectedPlayerCount);
            
            // Initialize players if not already initialized
            if (playerManager.AllPlayers.Count == 0)
            {
                playerManager.InitializePlayers();
            }
        }
        
        // Hide selection UI, show game UI
        if (gameStartPanel != null)
        {
            gameStartPanel.SetActive(false);
        }
        
        if (gameUI != null)
        {
            gameUI.SetActive(true);
        }
        
        Debug.Log($"Game started with {selectedPlayerCount} player(s)!");
    }
    
    /// <summary>
    /// Get the currently selected player count
    /// </summary>
    public int GetSelectedPlayerCount()
    {
        return selectedPlayerCount;
    }
}
