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
    
    private Rigidbody rb;
    private bool isRolling = false;
    private int currentValue = 0;
    private float lastCheckTime = 0f;
    
    // Dice face directions (standard dice orientation)
    // These vectors represent the "up" direction for each face value
    private Vector3[] faceDirections = new Vector3[]
    {
        Vector3.zero,           // 0 - unused
        -Vector3.up,            // 1 - bottom face
        Vector3.forward,        // 2 - front face
        -Vector3.right,        // 3 - left face
        Vector3.right,         // 4 - right face
        -Vector3.forward,      // 5 - back face
        Vector3.up             // 6 - top face
    };
    
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
                    // Dice has stopped, detect the value
                    DetectDiceValue();
                    isRolling = false;
                }
            }
        }
    }
    
    private void DetectDiceValue()
    {
        // Method 1: Raycast from center to each face direction
        // The face pointing up (or closest to up) determines the value
        float maxDot = -1f;
        int detectedValue = 1;
        
        for (int i = 1; i < faceDirections.Length; i++)
        {
            // Transform the face direction to world space
            Vector3 worldDirection = transform.TransformDirection(faceDirections[i]);
            
            // Check how aligned this direction is with world up
            float dot = Vector3.Dot(worldDirection, Vector3.up);
            
            if (dot > maxDot)
            {
                maxDot = dot;
                detectedValue = i;
            }
        }
        
        currentValue = detectedValue;
        
        // Alternative method: Use raycasting to check which face is pointing up
        // This is more reliable for complex dice models
        DetectValueByRaycast();
    }
    
    private void DetectValueByRaycast()
    {
        // Cast rays from the dice center in multiple directions
        // The face with the highest dot product to Vector3.up wins
        float maxDot = -1f;
        int bestValue = 1;
        
        // Check all 6 faces
        Vector3[] directions = new Vector3[]
        {
            transform.up,           // Top (6)
            -transform.up,          // Bottom (1)
            transform.forward,      // Front (2)
            -transform.forward,     // Back (5)
            transform.right,       // Right (4)
            -transform.right       // Left (3)
        };
        
        int[] values = new int[] { 6, 1, 2, 5, 4, 3 };
        
        for (int i = 0; i < directions.Length; i++)
        {
            float dot = Vector3.Dot(directions[i], Vector3.up);
            if (dot > maxDot)
            {
                maxDot = dot;
                bestValue = values[i];
            }
        }
        
        currentValue = bestValue;
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

