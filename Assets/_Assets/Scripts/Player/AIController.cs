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
            yield break;
        }
        
        isMakingDecision = true;
        
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
                // Calculate purchase probability based on cash reserves
                // More cash = more likely to purchase
                float cashRatio = currentCash / (cost * 2f); // Consider if cash is at least 2x the cost
                float adjustedProbability = purchaseProbability * Mathf.Clamp01(cashRatio);
                
                // Also consider income potential
                if (income > 0)
                {
                    adjustedProbability += 0.2f; // Boost probability if it generates income
                }
                
                // Make decision
                shouldPurchase = Random.Range(0f, 1f) < adjustedProbability;
                
                Debug.Log($"AI {player.PlayerName} decision: Cost=${cost}, Cash=${currentCash}, Income=${income}, Decision={(shouldPurchase ? "BUY" : "PASS")}");
            }
            else
            {
                Debug.Log($"AI {player.PlayerName} cannot afford: Cost=${cost}, Cash=${currentCash}");
            }
        }
        
        isMakingDecision = false;
        onDecisionMade?.Invoke(shouldPurchase);
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
