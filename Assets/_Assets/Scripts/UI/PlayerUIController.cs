using UnityEngine;
using TMPro;

public class PlayerUIController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI cashText;
    
    [Header("Player Finance Reference")]
    [SerializeField] private PlayerFinance playerFinance;
    
    [Header("Display Settings")]
    [Tooltip("Format string for displaying cash. {0} will be replaced with the cash value.")]
    [SerializeField] private string cashFormat = "RM{0:F0}";
    
    private void Start()
    {
        // Find CashText if not assigned
        if (cashText == null)
        {
            // Try to find it in children
            cashText = GetComponentInChildren<TextMeshProUGUI>();
            
            // If still null, try to find by name (CashText is in LowerBanner)
            if (cashText == null)
            {
                Transform cashTextTransform = transform.Find("LowerBanner/CashText");
                if (cashTextTransform != null)
                {
                    cashText = cashTextTransform.GetComponent<TextMeshProUGUI>();
                }
            }
            
            // Last resort: search by name in scene
            if (cashText == null)
            {
                GameObject cashTextObj = GameObject.Find("CashText");
                if (cashTextObj != null)
                {
                    cashText = cashTextObj.GetComponent<TextMeshProUGUI>();
                }
            }
        }
        
        // Find PlayerFinance if not assigned
        if (playerFinance == null)
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
        }
        
        // Subscribe to cash changes (not payday changes)
        if (playerFinance != null)
        {
            playerFinance.OnCashChanged += UpdateCashText;
            // Update immediately with current cash value
            UpdateCashText(playerFinance.CurrentCash);
        }
        else
        {
            Debug.LogWarning("PlayerUIController: PlayerFinance not found! CashText will not be updated.");
        }
    }
    
    private void UpdateCashText(float cash)
    {
        if (cashText != null)
        {
            cashText.text = string.Format(cashFormat, cash);
        }
        else
        {
            Debug.LogWarning("PlayerUIController: CashText is null! Cannot update display.");
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
            Debug.LogWarning("PlayerUIController: PlayerFinance is null! Cannot refresh display.");
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
