using UnityEngine;
using TMPro;
using System.Linq;

public class PlayerUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI cashText;
    
    [Header("Player Finance Reference")]
    [SerializeField] private PlayerFinance playerFinance;
    
    [Header("Display Settings")]
    [Tooltip("Format string for displaying cash. {0} will be replaced with the cash value.")]
    [SerializeField] private string cashFormat = "RM{0:F0}";
    
    private bool isInitialized = false; // Flag to track if Initialize was called
    
    private void Start()
    {
        // Only search for UI elements if this is a PlayerUI GameObject (not PlayerPrefab)
        // PlayerUI instances are named "PlayerUI_<PlayerName>" when spawned
        bool isPlayerUIGameObject = gameObject.name.StartsWith("PlayerUI_") || gameObject.name == "PlayerUI";
        
        if (!isPlayerUIGameObject)
        {
            Debug.Log($"PlayerUI.Start: Skipping UI search on {gameObject.name} (not a PlayerUI GameObject)");
            return;
        }
        
        // Find NameText if not assigned
        FindAndAssignNameText();
        
        // Find CashText if not assigned
        FindAndAssignCashText();
        
        // Find PlayerFinance if not assigned and not initialized manually
        if (!isInitialized && playerFinance == null)
        {
            // Try to find on the same GameObject
            playerFinance = GetComponent<PlayerFinance>();
            
            // If not found, try to find on Player GameObject
            if (playerFinance == null)
            {
                GameObject playerObj = GameObject.Find("Player");
                if (playerObj != null)
                {
                    playerFinance = playerObj.GetComponent<PlayerFinance>();
                }
            }
            
            // Last resort: find any PlayerFinance in scene
            if (playerFinance == null)
            {
                playerFinance = FindAnyObjectByType<PlayerFinance>();
            }
            
            // Subscribe to cash changes if found
            if (playerFinance != null)
            {
                playerFinance.OnCashChanged += UpdateCashText;
                // Update immediately with current cash value (which starts as initialCash)
                UpdateCashText(playerFinance.CurrentCash);
            }
            else
            {
                Debug.LogWarning("PlayerUI: PlayerFinance not found! CashText will not be updated.");
            }
        }
    }
    
    private void UpdateCashText(float cash)
    {
        // Try to find CashText if it's null (might not have been found yet)
        if (cashText == null)
        {
            FindAndAssignCashText();
        }
        
        if (cashText != null)
        {
            cashText.text = string.Format(cashFormat, cash);
            Debug.Log($"PlayerUI: Updated CashText to {cashText.text} for {gameObject.name}");
        }
        else
        {
            Debug.LogWarning($"PlayerUI: CashText is null! Cannot update display. GameObject: {gameObject.name}. Attempting to find again...");
            // One more attempt to find it
            FindAndAssignCashText();
            if (cashText != null)
            {
                cashText.text = string.Format(cashFormat, cash);
                Debug.Log($"PlayerUI: Found and updated CashText on retry: {cashText.text}");
            }
        }
    }
    
    /// <summary>
    /// Set the player name displayed in NameText
    /// </summary>
    public void SetPlayerName(string playerName)
    {
        // Try to find NameText if it's null (might not have been found yet)
        if (nameText == null)
        {
            FindAndAssignNameText();
        }
        
        if (nameText != null)
        {
            nameText.text = playerName;
            Debug.Log($"PlayerUI: Set player name to '{playerName}'");
        }
        else
        {
            Debug.LogWarning($"PlayerUI: NameText is null! Cannot set player name '{playerName}'. GameObject: {gameObject.name}");
        }
    }
    
    /// <summary>
    /// Manually refreshes the CashText display with the current cash value
    /// </summary>
    public void RefreshDisplay()
    {
        if (playerFinance != null)
        {
            UpdateCashText(playerFinance.CurrentCash);
        }
        else
        {
            Debug.LogWarning("PlayerUI: PlayerFinance is null! Cannot refresh display.");
        }
    }
    
    /// <summary>
    /// Find and assign NameText if not already assigned
    /// </summary>
    private void FindAndAssignNameText()
    {
        if (nameText != null) return; // Already assigned
        
        Debug.Log($"PlayerUI: Searching for NameText in {gameObject.name} (children count: {transform.childCount})");
        
        // First try to find by Transform path (most reliable)
        Transform nameTextTransform = transform.Find("UpperBanner/NameText");
        if (nameTextTransform == null)
        {
            nameTextTransform = transform.Find("NameText");
        }
        if (nameTextTransform != null)
        {
            nameText = nameTextTransform.GetComponent<TextMeshProUGUI>();
            if (nameText != null)
            {
                Debug.Log($"PlayerUI: Found NameText by path: {nameTextTransform.name} in {gameObject.name}");
                return;
            }
            else
            {
                Debug.LogWarning($"PlayerUI: NameText Transform found but no TextMeshProUGUI component on {nameTextTransform.name}");
            }
        }
        
        // Try to find it in all children recursively
        TextMeshProUGUI[] texts = GetComponentsInChildren<TextMeshProUGUI>(true);
        Debug.Log($"PlayerUI: Found {texts.Length} TextMeshProUGUI components in {gameObject.name}");
        foreach (var text in texts)
        {
            if (text.name == "NameText")
            {
                nameText = text;
                Debug.Log($"PlayerUI: Found NameText by name search: {text.name} in {text.transform.parent.name}");
                return;
            }
        }
        
        if (nameText == null)
        {
            string[] childNames = new string[transform.childCount];
            for (int i = 0; i < transform.childCount; i++)
            {
                childNames[i] = transform.GetChild(i).name;
            }
            Debug.LogWarning($"PlayerUI: Could not find NameText in {gameObject.name}. Available children: {string.Join(", ", childNames)}");
        }
    }
    
    /// <summary>
    /// Find and assign CashText if not already assigned
    /// </summary>
    private void FindAndAssignCashText()
    {
        if (cashText != null) return; // Already assigned
        
        Debug.Log($"PlayerUI: Searching for CashText in {gameObject.name} (children count: {transform.childCount})");
        
        // First try to find by Transform path (most reliable)
        Transform cashTextTransform = transform.Find("LowerBanner/CashText");
        if (cashTextTransform == null)
        {
            cashTextTransform = transform.Find("CashText");
        }
        if (cashTextTransform != null)
        {
            cashText = cashTextTransform.GetComponent<TextMeshProUGUI>();
            if (cashText != null)
            {
                Debug.Log($"PlayerUI: Found CashText by path: {cashTextTransform.name} in {gameObject.name}");
                return;
            }
            else
            {
                Debug.LogWarning($"PlayerUI: CashText Transform found but no TextMeshProUGUI component on {cashTextTransform.name}");
            }
        }
        
        // Try to find it in all children recursively
        TextMeshProUGUI[] texts = GetComponentsInChildren<TextMeshProUGUI>(true);
        Debug.Log($"PlayerUI: Found {texts.Length} TextMeshProUGUI components in {gameObject.name}");
        foreach (var text in texts)
        {
            if (text.name == "CashText")
            {
                cashText = text;
                Debug.Log($"PlayerUI: Found CashText by name search: {text.name} in {text.transform.parent.name}");
                return;
            }
        }
        
        if (cashText == null)
        {
            string[] childNames = new string[transform.childCount];
            for (int i = 0; i < transform.childCount; i++)
            {
                childNames[i] = transform.GetChild(i).name;
            }
            Debug.LogWarning($"PlayerUI: Could not find CashText in {gameObject.name}. Available children: {string.Join(", ", childNames)}");
        }
    }
    
    /// <summary>
    /// Initialize PlayerUI with player name and finance reference
    /// </summary>
    public void Initialize(string playerName, PlayerFinance finance)
    {
        isInitialized = true;
        
        Debug.Log($"PlayerUI.Initialize called for {playerName} on GameObject: {gameObject.name}");
        
        // Find UI references before using them
        FindAndAssignNameText();
        FindAndAssignCashText();
        
        // Set player name
        SetPlayerName(playerName);
        
        if (finance != null)
        {
            // Unsubscribe from old finance if exists
            if (playerFinance != null && playerFinance != finance)
            {
                playerFinance.OnCashChanged -= UpdateCashText;
                Debug.Log($"PlayerUI: Unsubscribed from old PlayerFinance");
            }
            
            playerFinance = finance;
            playerFinance.OnCashChanged += UpdateCashText;
            Debug.Log($"PlayerUI: Subscribed to PlayerFinance.OnCashChanged for {playerName}. Current cash: {playerFinance.CurrentCash}");
            
            // Force update immediately
            UpdateCashText(playerFinance.CurrentCash);
        }
        else
        {
            Debug.LogWarning($"PlayerUI.Initialize: PlayerFinance is null for {playerName}");
        }
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from event to prevent memory leaks
        if (playerFinance != null)
        {
            playerFinance.OnCashChanged -= UpdateCashText;
        }
    }
}
