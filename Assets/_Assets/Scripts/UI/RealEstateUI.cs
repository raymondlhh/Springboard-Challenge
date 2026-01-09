using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RealEstateUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject forSaleUIPanel;
    [SerializeField] private Button yesButton;
    [SerializeField] private Button noButton;
    
    [Header("Property Display")]
    [SerializeField] private TextMeshProUGUI propertyNameText;
    [SerializeField] private TextMeshProUGUI valueText;
    [SerializeField] private TextMeshProUGUI downpaymentText;
    [SerializeField] private TextMeshProUGUI incomeText;
    
    [Header("References")]
    [SerializeField] private RealEstateData realEstateData;
    [SerializeField] private PlayerManager playerManager; // Reference to PlayerManager
    
    private RealEstateData.RealEstateProperty currentProperty;
    private CardController currentCard;
    private Transform targetPathTransform;
    private string propertyName; // e.g., "RealEstate02"
    private GameObject playerItemPrefab;
    
    // Events
    public System.Action OnPurchaseComplete;
    public System.Action OnPurchaseCancelled;
    
    private void Start()
    {
        // Find ForSaleUI panel if not assigned
        if (forSaleUIPanel == null)
        {
            GameObject forSaleUIObj = GameObject.Find("ForSaleUI");
            if (forSaleUIObj != null)
            {
                forSaleUIPanel = forSaleUIObj;
            }
        }
        
        // Find buttons if not assigned
        if (yesButton == null)
        {
            Transform yesButtonTransform = transform.Find("BuyButton");
            if (yesButtonTransform != null)
            {
                yesButton = yesButtonTransform.GetComponent<Button>();
            }
        }
        
        if (noButton == null)
        {
            Transform noButtonTransform = transform.Find("SellButton");
            if (noButtonTransform != null)
            {
                noButton = noButtonTransform.GetComponent<Button>();
            }
        }
        
        // Setup button listeners
        if (yesButton != null)
        {
            yesButton.onClick.RemoveAllListeners();
            yesButton.onClick.AddListener(OnYesClicked);
        }
        
        if (noButton != null)
        {
            noButton.onClick.RemoveAllListeners();
            noButton.onClick.AddListener(OnNoClicked);
        }
        
        // Find PlayerManager if not assigned
        if (playerManager == null)
        {
            playerManager = FindAnyObjectByType<PlayerManager>();
        }
        
        // Load PlayerItem prefab
        if (playerItemPrefab == null)
        {
            playerItemPrefab = Resources.Load<GameObject>("PlayerItem");
            if (playerItemPrefab == null)
            {
                Debug.LogWarning("PlayerItem prefab not found in Resources folder!");
            }
        }
        
        // Hide UI initially
        if (forSaleUIPanel != null)
        {
            forSaleUIPanel.SetActive(false);
        }
    }
    
    /// <summary>
    /// Shows the ForSaleUI with property information
    /// </summary>
    public void ShowForSaleUI(RealEstateData.RealEstateProperty property, CardController card, Transform pathTransform, string propName)
    {
        if (property == null)
        {
            Debug.LogError("Cannot show ForSaleUI: Property is null!");
            return;
        }
        
        currentProperty = property;
        currentCard = card;
        targetPathTransform = pathTransform;
        propertyName = propName;
        
        // Update UI text
        if (propertyNameText != null)
        {
            propertyNameText.text = property.displayName;
        }
        
        if (valueText != null)
        {
            valueText.text = $"$ {property.value:N0}";
        }
        
        if (downpaymentText != null)
        {
            downpaymentText.text = $"$ {property.downpayment:N0}";
        }
        
        if (incomeText != null)
        {
            incomeText.text = $"+ $ {property.income:N0}";
        }
        
        // Show UI
        if (forSaleUIPanel != null)
        {
            forSaleUIPanel.SetActive(true);
        }
        else
        {
            Debug.LogWarning("ForSaleUI panel is null! Cannot show UI.");
        }
        
        // Check if current player is AI and make decision automatically
        if (playerManager != null && playerManager.CurrentPlayer != null && playerManager.CurrentPlayer.IsAI)
        {
            StartCoroutine(MakeAIDecision());
        }
    }
    
    /// <summary>
    /// Makes a purchase decision for AI players
    /// </summary>
    private System.Collections.IEnumerator MakeAIDecision()
    {
        if (currentProperty == null || playerManager == null || playerManager.CurrentPlayer == null)
        {
            yield break;
        }
        
        AIController aiController = playerManager.CurrentPlayer.AIController;
        if (aiController == null)
        {
            Debug.LogWarning($"AIController not found for AI player {playerManager.CurrentPlayer.PlayerName}");
            OnNoClicked(); // Default to no if AI controller missing
            yield break;
        }
        
        // Get current player's finance
        PlayerFinance playerFinance = playerManager.CurrentPlayer.PlayerFinance;
        if (playerFinance == null)
        {
            Debug.LogWarning("PlayerFinance is null for AI player!");
            OnNoClicked(); // Default to no if finance missing
            yield break;
        }
        
        // Make decision using AI controller
        bool shouldPurchase = false;
        bool decisionMade = false;
        
        Debug.Log($"RealEstateUI: Starting AI decision for {playerManager.CurrentPlayer.PlayerName}. Cost: {currentProperty.downpayment}, Income: {currentProperty.income}");
        
        yield return StartCoroutine(aiController.MakePurchaseDecision(
            currentProperty.downpayment,
            currentProperty.income,
            (decision) => 
            { 
                shouldPurchase = decision;
                decisionMade = true;
                Debug.Log($"RealEstateUI: AI decision callback received. Decision: {(decision ? "BUY" : "PASS")}");
            }
        ));
        
        // Wait until decision is made (should be immediate after coroutine completes, but just in case)
        float timeout = 5f;
        float elapsed = 0f;
        while (!decisionMade && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        if (!decisionMade)
        {
            Debug.LogWarning($"RealEstateUI: AI decision timeout! Defaulting to PASS.");
            shouldPurchase = false;
        }
        
        // Execute the decision
        if (shouldPurchase)
        {
            Debug.Log($"RealEstateUI: AI {playerManager.CurrentPlayer.PlayerName} decided to BUY {currentProperty.displayName}");
            OnYesClicked();
        }
        else
        {
            Debug.Log($"RealEstateUI: AI {playerManager.CurrentPlayer.PlayerName} decided to PASS on {currentProperty.displayName}");
            OnNoClicked();
        }
    }
    
    private void OnYesClicked()
    {
        if (currentProperty == null)
        {
            Debug.LogError("Cannot purchase: Current property is null!");
            return;
        }
        
        // Get current player's finance
        PlayerFinance currentPlayerFinance = GetCurrentPlayerFinance();
        if (currentPlayerFinance == null)
        {
            Debug.LogError("PlayerFinance is null! Cannot purchase property.");
            return;
        }
        
        // Check if player has enough cash for the downpayment
        if (currentPlayerFinance.CurrentCash < currentProperty.downpayment)
        {
            Debug.LogWarning($"Not enough cash! Need {currentProperty.downpayment}, have {currentPlayerFinance.CurrentCash}");
            return;
        }
        
        // Subtract the downpayment from player's cash
        bool cashSubtracted = currentPlayerFinance.SubtractCash(currentProperty.downpayment);
        if (!cashSubtracted)
        {
            Debug.LogWarning("Failed to subtract cash! Purchase cancelled.");
            return;
        }
        
        // Spawn PlayerItem prefab at the path transform
        GameObject playerItem = null;
        if (playerItemPrefab != null && targetPathTransform != null)
        {
            playerItem = Instantiate(playerItemPrefab, targetPathTransform.position, targetPathTransform.rotation, targetPathTransform);
            playerItem.name = $"PlayerItem_{propertyName}";
            Debug.Log($"Spawned PlayerItem at {targetPathTransform.name}");
        }
        else
        {
            Debug.LogWarning("Cannot spawn PlayerItem: Prefab or transform is null!");
        }
        
        // Add income item to PlayerFinance
        if (currentPlayerFinance != null && !string.IsNullOrEmpty(propertyName))
        {
            // Add income item with property name as details and income amount
            currentPlayerFinance.AddIncomeItem(propertyName, currentProperty.income);
            Debug.Log($"Added income item: {propertyName} - {currentProperty.income}");
        }
        
        // Add PlayerItem to current player's owned items
        if (playerManager != null && playerManager.CurrentPlayer != null && playerItem != null)
        {
            playerManager.CurrentPlayer.AddPlayerItem(playerItem);
        }
        
        // Hide UI
        if (forSaleUIPanel != null)
        {
            forSaleUIPanel.SetActive(false);
        }
        
        // Destroy the card
        if (currentCard != null)
        {
            Destroy(currentCard.gameObject);
        }
        
        // Notify CardsManager that purchase is complete
        OnPurchaseComplete?.Invoke();
        
        // Spawn dice (this will be handled by CardsManager)
    }
    
    private void OnNoClicked()
    {
        // Hide UI
        if (forSaleUIPanel != null)
        {
            forSaleUIPanel.SetActive(false);
        }
        
        // Destroy the card
        if (currentCard != null)
        {
            Destroy(currentCard.gameObject);
        }
        
        // Notify CardsManager that purchase was cancelled
        OnPurchaseCancelled?.Invoke();
        
        // Spawn dice (this will be handled by CardsManager)
    }
    
    /// <summary>
    /// Hides the ForSaleUI
    /// </summary>
    public void HideForSaleUI()
    {
        if (forSaleUIPanel != null)
        {
            forSaleUIPanel.SetActive(false);
        }
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
}
