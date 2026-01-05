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
    
    public void AnimateCard(Transform startTransform, Transform endTransform)
    {
        if (isAnimating)
        {
            Debug.LogWarning("Card is already animating!");
            return;
        }
        
        StartCoroutine(CardAnimationSequence(startTransform, endTransform));
    }
    
    private IEnumerator CardAnimationSequence(Transform startTransform, Transform endTransform)
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
        
        while (elapsedTime < moveDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / moveDuration;
            
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
        
        // Step 4: Wait 3 seconds
        yield return new WaitForSeconds(waitDuration);
        
        // Step 5: Destroy the card
        Destroy(gameObject);
        
        isAnimating = false;
    }
}

