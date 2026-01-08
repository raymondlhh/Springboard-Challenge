using UnityEngine;
using System.Collections;

public class CardController : MonoBehaviour
{
    [Header("Animation Settings")]
    [SerializeField] private float moveDuration = 2f;
    [SerializeField] private float rotateDuration = 0.5f;
    [SerializeField] private float waitDuration = 3f;
    
    private bool isAnimating = false;
    private bool shouldWaitForInput = false; // Flag to delay destruction for RealEstate cards
    private bool hasReachedEnd = false; // Flag to indicate card has reached end path
    
    public bool IsAnimating => isAnimating;
    public bool HasReachedEnd => hasReachedEnd;
    
    // Event fired when card reaches end path (for RealEstate cards)
    public System.Action<CardController> OnCardReachedEnd;
    
    public void AnimateCard(Transform startTransform, Transform endTransform, float customMoveDuration = -1f, float customWaitDuration = -1f, bool waitForInput = false)
    {
        if (isAnimating)
        {
            Debug.LogWarning("Card is already animating!");
            return;
        }
        
        shouldWaitForInput = waitForInput;
        
        // Use custom duration if provided, otherwise use the serialized value
        float durationToUse = customMoveDuration > 0 ? customMoveDuration : moveDuration;
        float waitDurationToUse = customWaitDuration > 0 ? customWaitDuration : waitDuration;
        StartCoroutine(CardAnimationSequence(startTransform, endTransform, durationToUse, waitDurationToUse));
    }
    
    private IEnumerator CardAnimationSequence(Transform startTransform, Transform endTransform, float moveDurationToUse, float waitDurationToUse)
    {
        isAnimating = true;
        hasReachedEnd = false;
        
        // Step 1: Spawn at CardsStartPath with 180 degree rotation
        transform.position = startTransform.position;
        transform.rotation = startTransform.rotation * Quaternion.Euler(0, 0, 180);
        
        Vector3 startPos = startTransform.position;
        Vector3 endPos = endTransform.position;
        Quaternion startRot = transform.rotation; // 180 degrees
        Quaternion endRot = endTransform.rotation; // 0 degrees (target rotation)
        
        // Step 2: Move to CardsEndPath (keeping 180 degree rotation during movement)
        float elapsedTime = 0f;
        
        while (elapsedTime < moveDurationToUse)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / moveDurationToUse;
            
            // Smooth curve (ease in-out)
            float curve = t * t * (3f - 2f * t);
            
            // Interpolate position only (keep rotation at 180)
            transform.position = Vector3.Lerp(startPos, endPos, curve);
            
            // Keep rotation at 180 during movement
            transform.rotation = startRot;
            
            yield return null;
        }
        
        // Ensure exact final position
        transform.position = endPos;
        
        // Step 3: Rotate back to 0 degrees after reaching end path
        elapsedTime = 0f;
        while (elapsedTime < rotateDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / rotateDuration;
            
            // Smooth curve for rotation
            float curve = t * t * (3f - 2f * t);
            
            // Rotate from 180 to 0
            transform.rotation = Quaternion.Lerp(startRot, endRot, curve);
            
            yield return null;
        }
        
        // Ensure exact final rotation
        transform.rotation = endRot;
        
        // Mark that card has reached end
        hasReachedEnd = true;
        
        // If this is a RealEstate card, notify and wait for input
        if (shouldWaitForInput)
        {
            // Notify CardsManager that card has reached end
            OnCardReachedEnd?.Invoke(this);
            
            // Wait indefinitely until manually destroyed
            while (shouldWaitForInput && gameObject != null)
            {
                yield return null;
            }
        }
        else
        {
            // Step 4: Wait before destroying (normal behavior)
            yield return new WaitForSeconds(waitDurationToUse);
            
            // Step 5: Destroy the card
            Destroy(gameObject);
        }
        
        isAnimating = false;
    }
    
    /// <summary>
    /// Manually destroys the card (called when user makes a decision)
    /// </summary>
    public void DestroyCard()
    {
        shouldWaitForInput = false;
        if (gameObject != null)
        {
            Destroy(gameObject);
        }
    }
}

