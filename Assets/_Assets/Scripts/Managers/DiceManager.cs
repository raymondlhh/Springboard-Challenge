using UnityEngine;

public class DiceManager : MonoBehaviour
{
    [Header("Spawner References")]
    [SerializeField] private Transform firstSpawner;
    [SerializeField] private Transform secondSpawner;
    [SerializeField] private Transform oneDiceSpawner; // Spawner for one dice mode (OneDice/FirstSpawner)
    
    [Header("Dice Method Selection")]
    [SerializeField] private bool IsSecondMethod = false; // If true, use video-based dice method instead of spawning physical dice
    
    [Header("Dice Settings")]
    [SerializeField] private float diceCheckInterval = 0.1f;
    [SerializeField] private GameObject dicePrefab;
    
    [Header("Debug Settings")]
    [SerializeField] private bool IsDebugging = false; // Enable debug mode to use fixed movement steps
    [SerializeField] private int debugFixedSteps = 1; // Fixed number of steps to move when IsDebugging is true
    
    [Header("Video Player References (Second Method)")]
    [SerializeField] private GameObject diceMeter; // The DiceMeter video player object
    [SerializeField] private GameObject[] doubleVideoPlayers; // Array for double rolls: double_no2_1+1, double_no4_2+2, etc.
    [SerializeField] private GameObject[] singleVideoPlayers; // Array for single dice: single_no1 to single_no6
    [SerializeField] private GameObject[] nonDoubleVideoPlayers; // Array for non-double two dice: no3_1+2, no4_1+3, etc.
    
    // Public properties to access spawners and settings
    public Transform FirstSpawner => firstSpawner;
    public Transform SecondSpawner => secondSpawner;
    public Transform OneDiceSpawner => oneDiceSpawner;
    public float DiceCheckInterval => diceCheckInterval;
    public GameObject DicePrefab => dicePrefab;
    public bool IsDebuggingEnabled => IsDebugging;
    public int DebugFixedSteps => debugFixedSteps;
    public bool UseSecondMethod => IsSecondMethod;
    public GameObject DiceMeter => diceMeter;
    public GameObject[] DoubleVideoPlayers => doubleVideoPlayers;
    public GameObject[] SingleVideoPlayers => singleVideoPlayers;
    public GameObject[] NonDoubleVideoPlayers => nonDoubleVideoPlayers;
    
    /// <summary>
    /// Checks if both dice have the same value (only valid in two dice mode)
    /// </summary>
    /// <param name="firstDiceValue">Value of the first dice</param>
    /// <param name="secondDiceValue">Value of the second dice</param>
    /// <returns>True if both dice have the same value</returns>
    public bool AreDiceMatching(int firstDiceValue, int secondDiceValue)
    {
        return firstDiceValue == secondDiceValue;
    }
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Find spawners if not assigned
        if (firstSpawner == null || secondSpawner == null)
        {
            FindSpawners();
        }
        
        // Load dice prefab if not assigned
        if (dicePrefab == null)
        {
            LoadDicePrefab();
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    private void FindSpawners()
    {
        // Try to find spawners by name
        GameObject firstSpawnerObj = GameObject.Find("FirstSpawner");
        GameObject secondSpawnerObj = GameObject.Find("SecondSpawner");
        
        // Try to find OneDice/FirstSpawner for one dice mode
        GameObject oneDiceParent = GameObject.Find("OneDice");
        if (oneDiceParent != null)
        {
            Transform oneDiceFirstSpawner = oneDiceParent.transform.Find("FirstSpawner");
            if (oneDiceFirstSpawner != null)
            {
                oneDiceSpawner = oneDiceFirstSpawner;
            }
        }
        
        // Fallback: try to find OneDice/FirstSpawner directly
        if (oneDiceSpawner == null)
        {
            Transform oneDiceFirstSpawner = GameObject.Find("OneDice/FirstSpawner")?.transform;
            if (oneDiceFirstSpawner != null)
            {
                oneDiceSpawner = oneDiceFirstSpawner;
            }
        }
        
        if (firstSpawnerObj != null)
        {
            firstSpawner = firstSpawnerObj.transform;
        }
        
        if (secondSpawnerObj != null)
        {
            secondSpawner = secondSpawnerObj.transform;
        }
    }
    
    private void LoadDicePrefab()
    {
        // Try to load dice prefab from Resources folder
        // Note: The prefab must be in a "Resources" folder for this to work at runtime
        dicePrefab = Resources.Load<GameObject>("Dice");
        
        // If still null, the prefab should be assigned in the Inspector
        // or placed in a Resources folder
        if (dicePrefab == null)
        {
            Debug.LogWarning("Dice Prefab not found! Please assign it in the Inspector or place it in a Resources folder.");
        }
    }
}
