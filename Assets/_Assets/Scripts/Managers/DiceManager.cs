using UnityEngine;

public class DiceManager : MonoBehaviour
{
    [Header("Spawner References")]
    [SerializeField] private Transform firstSpawner;
    [SerializeField] private Transform secondSpawner;
    [SerializeField] private Transform oneDiceSpawner; // Spawner for one dice mode (OneDice/FirstSpawner)
    
    [Header("Dice Settings")]
    [SerializeField] private float diceCheckInterval = 0.1f;
    [SerializeField] private GameObject dicePrefab;
    
    // Public properties to access spawners and settings
    public Transform FirstSpawner => firstSpawner;
    public Transform SecondSpawner => secondSpawner;
    public Transform OneDiceSpawner => oneDiceSpawner;
    public float DiceCheckInterval => diceCheckInterval;
    public GameObject DicePrefab => dicePrefab;
    
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
