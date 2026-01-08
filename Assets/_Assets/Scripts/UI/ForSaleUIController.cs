using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ForSaleUIController : MonoBehaviour
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
    [SerializeField] private PlayerFinance playerFinance;
    [SerializeField] private RealEstateData realEstateData;
    
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
        
        // Find PlayerFinance if not assigned
        if (playerFinance == null)
        {
            GameObject playerObj = GameObject.Find("Player");
            if (playerObj != null)
            {
                playerFinance = playerObj.GetComponent<PlayerFinance>();
            }
            
            if (playerFinance == null)
            {
                playerFinance = FindAnyObjectByType<PlayerFinance>();
            }
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
    }
    
    private void OnYesClicked()
    {
        if (currentProperty == null)
        {
            Debug.LogError("Cannot purchase: Current property is null!");
            return;
        }
        
        // Check if player has enough cash (using downpayment)
        if (playerFinance == null)
        {
            Debug.LogError("PlayerFinance is null! Cannot purchase property.");
            return;
        }
        
        // Check if player has enough cash for the downpayment
        if (playerFinance.CurrentCash < currentProperty.downpayment)
        {
            Debug.LogWarning($"Not enough cash! Need {currentProperty.downpayment}, have {playerFinance.CurrentCash}");
            return;
        }
        
        // Subtract the downpayment from player's cash
        bool cashSubtracted = playerFinance.SubtractCash(currentProperty.downpayment);
        if (!cashSubtracted)
        {
            Debug.LogWarning("Failed to subtract cash! Purchase cancelled.");
            return;
        }
        
        // Spawn PlayerItem prefab at the path transform
        if (playerItemPrefab != null && targetPathTransform != null)
        {
            GameObject playerItem = Instantiate(playerItemPrefab, targetPathTransform.position, targetPathTransform.rotation, targetPathTransform);
            playerItem.name = $"PlayerItem_{propertyName}";
            Debug.Log($"Spawned PlayerItem at {targetPathTransform.name}");
        }
        else
        {
            Debug.LogWarning("Cannot spawn PlayerItem: Prefab or transform is null!");
        }
        
        // Add income item to PlayerFinance
        if (playerFinance != null && !string.IsNullOrEmpty(propertyName))
        {
            // Add income item with property name as details and income amount
            playerFinance.AddIncomeItem(propertyName, currentProperty.income);
            Debug.Log($"Added income item: {propertyName} - {currentProperty.income}");
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
}
