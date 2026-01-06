using UnityEngine;
using System.Collections;

public class CardController : MonoBehaviour
{
    [Header("Animation Settings")]
    [SerializeField] private float moveDuration = 2f;
    [SerializeField] private float rotateDuration = 0.5f;
    [SerializeField] private float waitDuration = 3f;
    
    private bool isAnimating = false;
    
    public bool IsAnimating => isAnimating;
    
    public void AnimateCard(Transform startTransform, Transform endTransform, float customMoveDuration = -1f, float customWaitDuration = -1f)
    {
        if (isAnimating)
        {
            Debug.LogWarning("Card is already animating!");
            return;
        }
        
        // Use custom duration if provided, otherwise use the serialized value
        float durationToUse = customMoveDuration > 0 ? customMoveDuration : moveDuration;
        float waitDurationToUse = customWaitDuration > 0 ? customWaitDuration : waitDuration;
        StartCoroutine(CardAnimationSequence(startTransform, endTransform, durationToUse, waitDurationToUse));
    }
    
    private IEnumerator CardAnimationSequence(Transform startTransform, Transform endTransform, float moveDurationToUse, float waitDurationToUse)
    {
        isAnimating = true;
        
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
        
        // Step 4: Wait before destroying
        yield return new WaitForSeconds(waitDurationToUse);
        
        // Step 5: Destroy the card
        Destroy(gameObject);
        
        isAnimating = false;
    }
}

