using UnityEngine;

public class DiceController : MonoBehaviour
{
    [Header("Dice Settings")]
    [SerializeField] private float rollForce = 10f;
    [SerializeField] private float rollTorque = 10f;
    [SerializeField] private float minRollForce = 5f;
    [SerializeField] private float maxRollForce = 15f;
    [SerializeField] private float minRollTorque = 5f;
    [SerializeField] private float maxRollTorque = 15f;
    
    [Header("Value Detection")]
    [SerializeField] private float checkInterval = 0.1f;
    [SerializeField] private float velocityThreshold = 0.1f;
    [SerializeField] private float angularVelocityThreshold = 0.1f;
    [SerializeField] private float stabilityWaitTime = 0.2f; // Wait for dice to stabilize
    
    [Header("Face Mapping")]
    [Tooltip("Face values for: Up, Down, Forward, Back, Right, Left. Adjust if detection is incorrect.")]
    [SerializeField] private int[] faceValues = new int[] { 1, 6, 2, 5, 3, 4 };
    
    private Rigidbody rb;
    private bool isRolling = false;
    private int currentValue = 0;
    private float lastCheckTime = 0f;
    private float stableTime = 0f;
    
    public int CurrentValue => currentValue;
    public bool IsRolling => isRolling;
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        
        // Configure rigidbody for dice physics
        rb.mass = 1f;
        rb.linearDamping = 0.5f;
        rb.angularDamping = 0.5f;
        
        // Start as kinematic (no physics) - will be enabled when rolling
        rb.isKinematic = true;
        rb.useGravity = false;
    }
    
    public void RollDice()
    {
        if (isRolling) return;
        
        isRolling = true;
        currentValue = 0;
        stableTime = 0f;
        
        // Reset position and rotation to initial state
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        
        // Enable physics (make non-kinematic and enable gravity)
        rb.isKinematic = false;
        rb.useGravity = true;
        
        // Wake up the rigidbody
        rb.WakeUp();
        
        // Add random force and torque for realistic rolling
        Vector3 randomForce = new Vector3(
            Random.Range(-1f, 1f),
            Random.Range(0.5f, 1.5f),
            Random.Range(-1f, 1f)
        ).normalized * Random.Range(minRollForce, maxRollForce);
        
        Vector3 randomTorque = new Vector3(
            Random.Range(-1f, 1f),
            Random.Range(-1f, 1f),
            Random.Range(-1f, 1f)
        ).normalized * Random.Range(minRollTorque, maxRollTorque);
        
        rb.AddForce(randomForce, ForceMode.Impulse);
        rb.AddTorque(randomTorque, ForceMode.Impulse);
    }
    
    private void Update()
    {
        if (isRolling)
        {
            // Check if dice has stopped rolling
            if (Time.time - lastCheckTime >= checkInterval)
            {
                lastCheckTime = Time.time;
                
                // Check if dice is at rest
                if (rb.linearVelocity.magnitude < velocityThreshold && 
                    rb.angularVelocity.magnitude < angularVelocityThreshold)
                {
                    // Count how long the dice has been stable
                    stableTime += checkInterval;
                    
                    // Only detect after dice has been stable for a duration
                    if (stableTime >= stabilityWaitTime)
                    {
                        // Dice has stopped, detect the value
                        DetectDiceValue();
                        isRolling = false;
                        stableTime = 0f;
                    }
                }
                else
                {
                    // Dice is still moving, reset stability timer
                    stableTime = 0f;
                }
            }
        }
    }
    
    private void DetectDiceValue()
    {
        // Get all 6 face directions in world space
        Vector3[] faceDirections = new Vector3[]
        {
            transform.up,           // Index 0: Up
            -transform.up,          // Index 1: Down
            transform.forward,      // Index 2: Forward
            -transform.forward,     // Index 3: Back
            transform.right,       // Index 4: Right
            -transform.right       // Index 5: Left
        };
        
        // Detect the BOTTOM face (pointing downward) instead of top face
        // This is more reliable because the bottom face is always in contact with the ground
        float maxDot = -1f;
        int bestIndex = 1; // Default to Down
        
        for (int i = 0; i < faceDirections.Length; i++)
        {
            // Check alignment with world DOWN (negative Y)
            float dot = Vector3.Dot(faceDirections[i].normalized, Vector3.down);
            
            if (dot > maxDot)
            {
                maxDot = dot;
                bestIndex = i;
            }
        }
        
        // Map the detected face index to the actual dice value
        // Since we detected the bottom face, we need to get the opposite face value
        // On standard dice, opposite faces sum to 7
        if (bestIndex >= 0 && bestIndex < faceValues.Length)
        {
            int bottomFaceValue = faceValues[bestIndex];
            // Calculate the top face value (opposite face)
            // If bottom is 1, top is 6; if bottom is 6, top is 1, etc.
            currentValue = 7 - bottomFaceValue;
        }
        else
        {
            currentValue = 1; // Fallback
        }
        
        // Debug output
        string[] directionNames = { "Up", "Down", "Forward", "Back", "Right", "Left" };
        int bottomValue = (bestIndex >= 0 && bestIndex < faceValues.Length) ? faceValues[bestIndex] : 0;
        Debug.Log($"[Dice {gameObject.name}] Detected Top: {currentValue} | " +
                  $"Bottom Face: {directionNames[bestIndex]} (Value: {bottomValue}) | " +
                  $"Dot: {maxDot:F2}");
    }
    
    public void ResetDice()
    {
        isRolling = false;
        currentValue = 0;
        
        // Stop physics and make kinematic again
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        
        // Reset position and rotation
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
    }
}

