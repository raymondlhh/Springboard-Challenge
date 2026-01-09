using UnityEngine;
using System.Collections;

/// <summary>
/// Controls AI player behavior and decision-making
/// </summary>
public class AIController : MonoBehaviour
{
    [Header("AI Settings")]
    [SerializeField] private float decisionDelayMin = 0.5f;
    [SerializeField] private float decisionDelayMax = 2.0f;
    [SerializeField] private float purchaseProbability = 0.6f; // 60% chance to purchase if affordable
    
    private Player player;
    private bool isMakingDecision = false;
    
    /// <summary>
    /// Initialize the AI controller with a reference to the player
    /// </summary>
    public void Initialize(Player playerRef)
    {
        player = playerRef;
    }
    
    /// <summary>
    /// Called when AI needs to make a purchase decision (e.g., for real estate or business)
    /// </summary>
    public IEnumerator MakePurchaseDecision(float cost, float income, System.Action<bool> onDecisionMade)
    {
        if (isMakingDecision)
        {
            Debug.LogWarning("AIController: Already making a decision, skipping...");
            yield break;
        }
        
        isMakingDecision = true;
        
        // Try to find player if it's null (safety check)
        if (player == null)
        {
            player = GetComponent<Player>();
            if (player == null)
            {
                Debug.LogError("AIController: Player reference is null and cannot be found on GameObject!");
                isMakingDecision = false;
                onDecisionMade?.Invoke(false);
                yield break;
            }
            Debug.LogWarning("AIController: Player was null, found it on GameObject. Consider calling Initialize() properly.");
        }
        
        Debug.Log($"AIController: Starting purchase decision. Cost: {cost}, Income: {income}, Current PurchaseProb: {purchaseProbability}");
        
        // Simulate thinking time
        float delay = Random.Range(decisionDelayMin, decisionDelayMax);
        yield return new WaitForSeconds(delay);
        
        bool shouldPurchase = false;
        
        if (player != null && player.PlayerFinance != null)
        {
            float currentCash = player.PlayerFinance.CurrentCash;
            
            // Check if AI can afford it
            if (currentCash >= cost)
            {
                float adjustedProbability = purchaseProbability;
                
                // If purchaseProbability is 1.0, always buy (if affordable)
                if (purchaseProbability >= 1.0f)
                {
                    shouldPurchase = true;
                    adjustedProbability = 1.0f; // For debug log
                    Debug.Log($"AIController: Purchase probability is 1.0, will always BUY (if affordable)");
                }
                else
                {
                    // For probabilities < 1.0, consider cash reserves
                    // More cash = more likely to purchase
                    float cashRatio = currentCash / (cost * 2f); // Consider if cash is at least 2x the cost
                    adjustedProbability = purchaseProbability * Mathf.Clamp01(cashRatio);
                    
                    // Also consider income potential
                    if (income > 0)
                    {
                        adjustedProbability += 0.2f; // Boost probability if it generates income
                        adjustedProbability = Mathf.Clamp01(adjustedProbability); // Ensure it doesn't exceed 1.0
                    }
                    
                    // Make decision
                    float randomValue = Random.Range(0f, 1f);
                    shouldPurchase = randomValue < adjustedProbability;
                    Debug.Log($"AIController: Random value: {randomValue}, Adjusted prob: {adjustedProbability}, Decision: {(shouldPurchase ? "BUY" : "PASS")}");
                }
                
                Debug.Log($"AI {player.PlayerName} decision: Cost=${cost}, Cash=${currentCash}, Income=${income}, PurchaseProb={purchaseProbability}, AdjustedProb={adjustedProbability}, Decision={(shouldPurchase ? "BUY" : "PASS")}");
            }
            else
            {
                Debug.Log($"AI {player.PlayerName} cannot afford: Cost=${cost}, Cash=${currentCash}");
            }
        }
        else
        {
            Debug.LogWarning($"AIController: Player or PlayerFinance is null! Player: {(player != null ? "exists" : "null")}, Finance: {(player != null && player.PlayerFinance != null ? "exists" : "null")}");
        }
        
        isMakingDecision = false;
        
        Debug.Log($"AIController: Invoking callback with decision: {(shouldPurchase ? "BUY" : "PASS")}");
        onDecisionMade?.Invoke(shouldPurchase);
        
        // Ensure callback is invoked even if it's null
        if (onDecisionMade == null)
        {
            Debug.LogWarning("AIController: onDecisionMade callback is null!");
        }
    }
    
    /// <summary>
    /// Called when it's the AI's turn to roll dice
    /// </summary>
    public IEnumerator RollDice(System.Action onRollComplete)
    {
        // Simulate thinking time before rolling
        float delay = Random.Range(0.3f, 1.0f);
        yield return new WaitForSeconds(delay);
        
        onRollComplete?.Invoke();
    }
    
    /// <summary>
    /// Set the purchase probability (0-1)
    /// </summary>
    public void SetPurchaseProbability(float probability)
    {
        purchaseProbability = Mathf.Clamp01(probability);
        Debug.Log($"AIController: Purchase probability set to {purchaseProbability} for player {(player != null ? player.PlayerName : "Unknown")}");
    }
    
    /// <summary>
    /// Get the current purchase probability
    /// </summary>
    public float GetPurchaseProbability()
    {
        return purchaseProbability;
    }
    
    /// <summary>
    /// Set decision delay range
    /// </summary>
    public void SetDecisionDelay(float min, float max)
    {
        decisionDelayMin = Mathf.Max(0f, min);
        decisionDelayMax = Mathf.Max(decisionDelayMin, max);
    }
}
