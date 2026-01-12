using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;

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
    [Tooltip("Single VideoPlayer component shared by both dice and DiceMeter videos. Used by BOTH human and AI players.")]
    [SerializeField] private VideoPlayer videoPlayer; // Single VideoPlayer component shared by both dice and DiceMeter videos
    [SerializeField] private RawImage rawImage; // RawImage component shared by both dice and DiceMeter (materials are switched dynamically)
    [SerializeField] private Material diceMaterial; // Material to use when displaying dice videos
    [SerializeField] private Material diceMeterMaterial; // Material to use when displaying DiceMeter video
    [SerializeField] private string diceMeterVideoUrl; // URL path to DiceMeter video (e.g., "StreamingAssets/Videos/Dice/DiceMeter.m4v")
    [Tooltip("Video URLs for double dice rolls. Used by BOTH human and AI players - same videos for all players.")]
    [SerializeField] private string[] doubleVideoUrls; // Array of URL paths for double rolls: double_no2_1+1, double_no4_2+2, etc.
    [Tooltip("Video URLs for single dice rolls. Used by BOTH human and AI players - same videos for all players.")]
    [SerializeField] private string[] singleVideoUrls; // Array of URL paths for single dice: single_no1 to single_no6
    [Tooltip("Video URLs for non-double two dice rolls. Used by BOTH human and AI players - same videos for all players.")]
    [SerializeField] private string[] nonDoubleVideoUrls; // Array of URL paths for non-double two dice: no3_1+2, no4_1+3, etc.
    [Header("Video URL Settings")]
    [Tooltip("Base path for video URLs. If empty, will use Application.streamingAssetsPath")]
    [SerializeField] private string videoBasePath = ""; // Base path for video URLs
    
    // Public properties to access spawners and settings
    public Transform FirstSpawner => firstSpawner;
    public Transform SecondSpawner => secondSpawner;
    public Transform OneDiceSpawner => oneDiceSpawner;
    public float DiceCheckInterval => diceCheckInterval;
    public GameObject DicePrefab => dicePrefab;
    public bool IsDebuggingEnabled => IsDebugging;
    public int DebugFixedSteps => debugFixedSteps;
    public bool UseSecondMethod => IsSecondMethod;
    public VideoPlayer VideoPlayer => videoPlayer;
    public RawImage RawImage => rawImage;
    public Material DiceMaterial => diceMaterial;
    public Material DiceMeterMaterial => diceMeterMaterial;
    public string[] DoubleVideoUrls => doubleVideoUrls;
    public string[] SingleVideoUrls => singleVideoUrls;
    public string[] NonDoubleVideoUrls => nonDoubleVideoUrls;
    public string DiceMeterVideoUrl => diceMeterVideoUrl;
    
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
        
        // Setup video player and RawImage connection if using second method
        if (IsSecondMethod)
        {
            SetupVideoPlayerRawImage();
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
    
    /// <summary>
    /// Sets up the connection between VideoPlayer and RawImage for displaying video
    /// </summary>
    private void SetupVideoPlayerRawImage()
    {
        if (videoPlayer == null)
        {
            Debug.LogWarning("[DiceManager] VideoPlayer is not assigned! Cannot setup RawImage connection.");
            return;
        }
        
        // Try to find RawImage if not assigned
        if (rawImage == null)
        {
            // Try to find RawImage in the video player's GameObject or its children
            rawImage = videoPlayer.GetComponentInChildren<RawImage>();
            
            // If still not found, try to find by name
            if (rawImage == null)
            {
                GameObject rawImageObj = GameObject.Find("RawImage");
                if (rawImageObj != null)
                {
                    rawImage = rawImageObj.GetComponent<RawImage>();
                }
            }
        }
        
        if (rawImage == null)
        {
            Debug.LogWarning("[DiceManager] RawImage not found! Video will not display in UI. Please assign RawImage in Inspector.");
            return;
        }
        
        // Ensure VideoPlayer is set to render to RenderTexture and use URL source
        if (videoPlayer.renderMode != VideoRenderMode.RenderTexture)
        {
            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            Debug.Log("[DiceManager] VideoPlayer render mode set to RenderTexture.");
        }
        
        // Set VideoPlayer source to URL
        videoPlayer.source = VideoSource.Url;
        Debug.Log("[DiceManager] VideoPlayer source set to URL.");
        
        // Create or get RenderTexture if not already set
        RenderTexture renderTexture = videoPlayer.targetTexture;
        if (renderTexture == null)
        {
            // Create a new RenderTexture with common video dimensions
            renderTexture = new RenderTexture(1920, 1080, 0, RenderTextureFormat.ARGB32);
            renderTexture.name = "VideoRenderTexture";
            videoPlayer.targetTexture = renderTexture;
            Debug.Log("[DiceManager] Created new RenderTexture for VideoPlayer.");
        }
        
        // Assign RenderTexture to RawImage texture
        rawImage.texture = renderTexture;
        
        // Set default material to dice material if available
        if (diceMaterial != null)
        {
            rawImage.material = diceMaterial;
            rawImage.material.mainTexture = renderTexture;
        }
        else if (rawImage.material != null && rawImage.material != rawImage.defaultMaterial)
        {
            // Use existing material if dice material not assigned
            rawImage.material.mainTexture = renderTexture;
        }
        
        Debug.Log("[DiceManager] VideoPlayer-RawImage connection established successfully.");
    }
    
    /// <summary>
    /// Manually setup VideoPlayer to RawImage connection (can be called from Inspector or other scripts)
    /// </summary>
    public void SetupVideoPlayerToRawImage(VideoPlayer player, RawImage rawImage)
    {
        if (player == null || rawImage == null)
        {
            Debug.LogWarning("[DiceManager] VideoPlayer or RawImage is null! Cannot setup connection.");
            return;
        }
        
        // Set render mode
        player.renderMode = VideoRenderMode.RenderTexture;
        
        // Create or get RenderTexture
        RenderTexture renderTexture = player.targetTexture;
        if (renderTexture == null)
        {
            renderTexture = new RenderTexture(1920, 1080, 0, RenderTextureFormat.ARGB32);
            renderTexture.name = $"{player.gameObject.name}_RenderTexture";
            player.targetTexture = renderTexture;
        }
        
        // Assign to RawImage
        rawImage.texture = renderTexture;
        
        // Set material texture if using custom material
        if (rawImage.material != null && rawImage.material != rawImage.defaultMaterial)
        {
            rawImage.material.mainTexture = renderTexture;
        }
        
        Debug.Log($"[DiceManager] Connected VideoPlayer '{player.gameObject.name}' to RawImage '{rawImage.gameObject.name}'.");
    }
    
    /// <summary>
    /// Gets the full URL path for a video file
    /// </summary>
    public string GetVideoUrl(string relativePath)
    {
        if (string.IsNullOrEmpty(relativePath))
        {
            return null;
        }
        
        // If path already starts with http:// or https://, use it as-is
        if (relativePath.StartsWith("http://") || relativePath.StartsWith("https://"))
        {
            return relativePath;
        }
        
        // Normalize the path by removing leading/trailing slashes and backslashes
        string normalizedPath = relativePath.TrimStart('/', '\\').TrimEnd('/', '\\');
        
        // Remove any leading "Assets/StreamingAssets/" or "StreamingAssets/" from the path
        // to prevent duplicate path segments when combining with Application.streamingAssetsPath
        if (normalizedPath.StartsWith("Assets/StreamingAssets/", System.StringComparison.OrdinalIgnoreCase))
        {
            normalizedPath = normalizedPath.Substring("Assets/StreamingAssets/".Length);
        }
        else if (normalizedPath.StartsWith("StreamingAssets/", System.StringComparison.OrdinalIgnoreCase))
        {
            normalizedPath = normalizedPath.Substring("StreamingAssets/".Length);
        }
        
        // If base path is specified, use it
        if (!string.IsNullOrEmpty(videoBasePath))
        {
            // Ensure proper path separator
            string basePath = videoBasePath.TrimEnd('/', '\\');
            return $"{basePath}/{normalizedPath}";
        }
        
        // Otherwise, use StreamingAssets path
        return System.IO.Path.Combine(Application.streamingAssetsPath, normalizedPath).Replace('\\', '/');
    }
    
    /// <summary>
    /// Plays the DiceMeter video using the shared VideoPlayer and switches to DiceMeter material
    /// </summary>
    public void PlayDiceMeterVideo()
    {
        if (videoPlayer == null)
        {
            Debug.LogWarning("[DiceManager] VideoPlayer is not assigned! Cannot play DiceMeter video.");
            return;
        }
        
        if (string.IsNullOrEmpty(diceMeterVideoUrl))
        {
            Debug.LogWarning("[DiceManager] DiceMeter Video URL is not assigned! Cannot play DiceMeter video.");
            return;
        }
        
        if (rawImage == null)
        {
            Debug.LogWarning("[DiceManager] RawImage is not assigned! Cannot switch to DiceMeter material.");
            return;
        }
        
        // Ensure VideoPlayer GameObject is active (may have been deactivated for cards/movement)
        if (videoPlayer.gameObject != null && !videoPlayer.gameObject.activeSelf)
        {
            videoPlayer.gameObject.SetActive(true);
            Debug.Log("[DiceManager] Activated VideoPlayer GameObject for DiceMeter video.");
        }
        
        // Ensure VideoPlayer source is set to URL
        videoPlayer.source = VideoSource.Url;
        
        // Switch to DiceMeter material
        if (diceMeterMaterial != null)
        {
            rawImage.material = diceMeterMaterial;
            RenderTexture renderTexture = videoPlayer.targetTexture;
            if (renderTexture != null)
            {
                rawImage.material.mainTexture = renderTexture;
            }
            Debug.Log("[DiceManager] Switched RawImage to DiceMeter material.");
        }
        
        // Get full URL path and set it
        string fullUrl = GetVideoUrl(diceMeterVideoUrl);
        videoPlayer.url = fullUrl;
        videoPlayer.isLooping = true; // Typically meter videos loop
        
        // Prepare the video before playing to ensure it's ready
        videoPlayer.Prepare();
        
        // Play the video (it will start when prepared)
        videoPlayer.Play();
        
        Debug.Log($"[DiceManager] Playing DiceMeter video from URL: {fullUrl}");
    }
    
    /// <summary>
    /// Switches RawImage material to Dice material (called when playing dice videos)
    /// </summary>
    public void SwitchToDiceMaterial()
    {
        if (rawImage == null)
        {
            return;
        }
        
        if (diceMaterial != null)
        {
            rawImage.material = diceMaterial;
            RenderTexture renderTexture = videoPlayer != null ? videoPlayer.targetTexture : null;
            if (renderTexture != null)
            {
                rawImage.material.mainTexture = renderTexture;
            }
            Debug.Log("[DiceManager] Switched RawImage to Dice material.");
        }
    }
    
    /// <summary>
    /// Stops the DiceMeter video and fully resets the VideoPlayer to ensure clean transition
    /// </summary>
    public void StopDiceMeterVideo()
    {
        if (videoPlayer != null)
        {
            // Stop the video
            videoPlayer.Stop();
            
            // Clear the URL to ensure the VideoPlayer is fully reset
            // This prevents the VideoPlayer from trying to play the DiceMeter when we switch to dice video
            videoPlayer.url = "";
            
            Debug.Log("[DiceManager] Stopped DiceMeter video and cleared URL.");
        }
    }
}
