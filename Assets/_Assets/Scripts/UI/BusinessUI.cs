using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BusinessUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject businessUIPanel;
    [SerializeField] private Button yesButton;
    [SerializeField] private Button noButton;
    
    [Header("Business Display")]
    [SerializeField] private TextMeshProUGUI businessNameText;
    [SerializeField] private TextMeshProUGUI capitalText;
    [SerializeField] private TextMeshProUGUI cashFlowText;
    [SerializeField] private TextMeshProUGUI incomePerVisitText;
    
    [Header("References")]
    [SerializeField] private PlayerFinance playerFinance;
    [SerializeField] private BusinessData businessData;
    
    private BusinessData.BusinessProperty currentBusiness;
    private CardController currentCard;
    private Transform targetPathTransform;
    private string businessName; // e.g., "Business02"
    private GameObject playerItemPrefab;
    
    // Events
    public System.Action OnPurchaseComplete;
    public System.Action OnPurchaseCancelled;
    
    private void Start()
    {
        // Find BusinessUI panel if not assigned
        if (businessUIPanel == null)
        {
            GameObject businessUIObj = GameObject.Find("BusinessUI");
            if (businessUIObj != null)
            {
                businessUIPanel = businessUIObj;
            }
        }
        
        // Find buttons if not assigned
        if (yesButton == null)
        {
            Transform yesButtonTransform = transform.Find("YesButton");
            if (yesButtonTransform != null)
            {
                yesButton = yesButtonTransform.GetComponent<Button>();
            }
        }
        
        if (noButton == null)
        {
            Transform noButtonTransform = transform.Find("NoButton");
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
        if (businessUIPanel != null)
        {
            businessUIPanel.SetActive(false);
        }
    }
    
    /// <summary>
    /// Shows the BusinessUI with business information
    /// </summary>
    public void ShowBusinessUI(BusinessData.BusinessProperty business, CardController card, Transform pathTransform, string busName)
    {
        if (business == null)
        {
            Debug.LogError("Cannot show BusinessUI: Business is null!");
            return;
        }
        
        currentBusiness = business;
        currentCard = card;
        targetPathTransform = pathTransform;
        businessName = busName;
        
        // Update UI text
        if (businessNameText != null)
        {
            businessNameText.text = business.displayName;
        }
        
        if (capitalText != null)
        {
            capitalText.text = $"$ {business.capital:N0}";
        }
        
        if (cashFlowText != null)
        {
            cashFlowText.text = $"$ {business.cashFlow:N0}";
        }
        
        if (incomePerVisitText != null)
        {
            incomePerVisitText.text = $"+$ {business.incomePerVisit:N0} income for every visit";
        }
        
        // Show UI
        if (businessUIPanel != null)
        {
            businessUIPanel.SetActive(true);
        }
        else
        {
            Debug.LogWarning("BusinessUI panel is null! Cannot show UI.");
        }
    }
    
    private void OnYesClicked()
    {
        if (currentBusiness == null)
        {
            Debug.LogError("Cannot purchase: Current business is null!");
            return;
        }
        
        // Check if player has enough cash (using capital)
        if (playerFinance == null)
        {
            Debug.LogError("PlayerFinance is null! Cannot purchase business.");
            return;
        }
        
        // Check if player has enough cash for the capital
        if (playerFinance.CurrentCash < currentBusiness.capital)
        {
            Debug.LogWarning($"Not enough cash! Need {currentBusiness.capital}, have {playerFinance.CurrentCash}");
            return;
        }
        
        // Subtract the capital from player's cash
        bool cashSubtracted = playerFinance.SubtractCash(currentBusiness.capital);
        if (!cashSubtracted)
        {
            Debug.LogWarning("Failed to subtract cash! Purchase cancelled.");
            return;
        }
        
        // Add cash flow to income items
        if (playerFinance != null && !string.IsNullOrEmpty(businessName))
        {
            // Add income item with business name as details and cash flow as amount
            playerFinance.AddIncomeItem(businessName, currentBusiness.cashFlow);
            Debug.Log($"Added income item: {businessName} - {currentBusiness.cashFlow}");
        }
        
        // Spawn PlayerItem prefab at the path transform
        if (playerItemPrefab != null && targetPathTransform != null)
        {
            GameObject playerItem = Instantiate(playerItemPrefab, targetPathTransform.position, targetPathTransform.rotation, targetPathTransform);
            playerItem.name = $"PlayerItem_{businessName}";
            Debug.Log($"Spawned PlayerItem at {targetPathTransform.name}");
        }
        else
        {
            Debug.LogWarning("Cannot spawn PlayerItem: Prefab or transform is null!");
        }
        
        // Hide UI
        if (businessUIPanel != null)
        {
            businessUIPanel.SetActive(false);
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
        if (businessUIPanel != null)
        {
            businessUIPanel.SetActive(false);
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
    /// Hides the BusinessUI
    /// </summary>
    public void HideBusinessUI()
    {
        if (businessUIPanel != null)
        {
            businessUIPanel.SetActive(false);
        }
    }
}
